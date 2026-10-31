using System;
using System.Collections.Generic;

namespace PortfolioSamples.Mes
{
    public class CircuitBreaker
    {
        // 시간을 주입해 대기 없이 재시도·복구 경계를 시험합니다.
        private readonly Func<DateTime> _utcNow;
        private readonly object _sync = new object();
        private readonly CircuitBreakerOptions _options;
        private readonly ILoggerService _log;
        private int _failureCount;
        private bool _isHalfOpenProbeRunning;
        private DateTime? _nextProbeAtUtc;
        private readonly Dictionary<MesFailureCategory, int> _classifiedFailureCounts =
            new Dictionary<MesFailureCategory, int>();
        public CircuitBreaker(CircuitBreakerOptions options, ILoggerService log, Func<DateTime> utcNow)
        {
            if (options == null || log == null || utcNow == null)
                throw new ArgumentNullException("dependency");
            if (options.FailureThreshold < 1 || options.OpenSeconds < 1)
                throw new ArgumentOutOfRangeException("options");
            _options = options;
            _utcNow = utcNow;
            _log = log;
            State = CircuitBreakerState.Closed;
        }

        public CircuitBreakerState State { get; private set; }
        public int FailureCount
        {
            get
            {
                lock (_sync)
                {
                    return _failureCount;
                }
            }
        }

        public DateTime? NextProbeAtUtc
        {
            get
            {
                lock (_sync)
                {
                    return _nextProbeAtUtc;
                }
            }
        }

        public int GetClassifiedFailureCount(MesFailureCategory category)
        {
            lock (_sync)
            {
                int count;
                return _classifiedFailureCounts.TryGetValue(category, out count)
                    ? count
                    : 0;
            }
        }

        /// <summary>
        /// 요청 실행 권한과 Half-Open Probe 1건 예약을 하나의 잠금 안에서 처리한다.
        /// 이 메서드가 true를 반환한 요청만 외부 호출을 수행해야 합니다. 공개 샘플은 실제 외부 통신을 하지 않습니다.
        /// </summary>
        public bool TryEnter(out DateTime? retryAfterUtc)
        {
            retryAfterUtc = null;
            if (!_options.Enabled)
            {
                return true;
            }

            lock (_sync)
            {
                if (State == CircuitBreakerState.Closed)
                {
                    return true;
                }

                DateTime nowUtc = _utcNow();
                if (State == CircuitBreakerState.Open)
                {
                    DateTime nextProbeUtc = _nextProbeAtUtc.GetValueOrDefault(nowUtc);
                    if (nowUtc < nextProbeUtc)
                    {
                        retryAfterUtc = nextProbeUtc;
                        return false;
                    }

                    ChangeState(CircuitBreakerState.HalfOpen, "OpenToHalfOpen", string.Empty);
                }

                if (_isHalfOpenProbeRunning)
                {
                    retryAfterUtc = _nextProbeAtUtc;
                    return false;
                }

                _isHalfOpenProbeRunning = true;
                return true;
            }
        }
        public MesOperationResult BuildOpenResult(string stage)
        {
            string retryText = NextProbeAtUtc.HasValue
                ? " NextProbeUtc=" + NextProbeAtUtc.Value.ToString("o")
                : string.Empty;
            return MesOperationResult.Fail(-503, stage + " blocked: MES Circuit Open" + retryText,
                MesFailureCategory.Server, true);
        }
        public void ReportSuccess()
        {
            if (!_options.Enabled)
            {
                return;
            }

            lock (_sync)
            {
                _failureCount = 0;
                _isHalfOpenProbeRunning = false;
                if (State == CircuitBreakerState.HalfOpen)
                {
                    ChangeState(CircuitBreakerState.Closed, "HalfOpenToClosed", string.Empty);
                }
            }
        }

        /// <summary>
        /// 분류된 결과를 Circuit Breaker에 반영한다.
        /// 업무 거절과 입력 오류는 MES 통신이 정상적으로 왕복한 결과이므로 장애 횟수에 넣지 않는다.
        /// </summary>
        public void ReportResult(string stage, MesOperationResult result)
        {
            if (!_options.Enabled || result == null)
            {
                return;
            }

            lock (_sync)
            {
                if (!result.Success && result.FailureCategory != MesFailureCategory.None)
                {
                    int categoryCount;
                    _classifiedFailureCounts.TryGetValue(result.FailureCategory,
                        out categoryCount);
                    _classifiedFailureCounts[result.FailureCategory] =
                        categoryCount + 1;
                }
                if (result.Success || !result.CountsAsCircuitFailure())
                {
                    _failureCount = 0;
                    _isHalfOpenProbeRunning = false;
                    if (State == CircuitBreakerState.HalfOpen)
                    {
                        ChangeState(CircuitBreakerState.Closed, "HalfOpenToClosed",
                            "Stage=" + stage + " Category=" + result.FailureCategory);
                    }
                    return;
                }

                _isHalfOpenProbeRunning = false;
                if (State == CircuitBreakerState.HalfOpen)
                {
                    OpenCircuit("HalfOpenToOpen", stage, result);
                    return;
                }

                if (State == CircuitBreakerState.Open)
                {
                    return;
                }

                _failureCount++;
                if (_failureCount >= Math.Max(1, _options.FailureThreshold))
                {
                    OpenCircuit("ClosedToOpen", stage, result);
                }
            }
        }

        /// <summary>
        /// Probe가 실제 통신 결과 없이 취소된 경우 점유만 해제한다.
        /// 취소를 성공으로 간주해 Circuit을 닫거나 통신 실패로 집계하지 않는다.
        /// </summary>
        public void ReleaseProbeWithoutResult(string stage)
        {
            if (!_options.Enabled)
            {
                return;
            }

            lock (_sync)
            {
                if (State != CircuitBreakerState.HalfOpen ||
                    !_isHalfOpenProbeRunning)
                {
                    return;
                }

                _isHalfOpenProbeRunning = false;
                if (_log != null)
                {
                    _log.Warn("Circuit Half-Open probe released without result Stage=" +
                        (stage ?? string.Empty));
                }
            }
        }

        private void OpenCircuit(string transition, string stage, MesOperationResult result)
        {
            _nextProbeAtUtc = _utcNow().AddSeconds(Math.Max(1, _options.OpenSeconds));
            ChangeState(CircuitBreakerState.Open, transition,
                "Stage=" + stage +
                " Category=" + result.FailureCategory +
                " FailureCount=" + _failureCount +
                " NextProbeUtc=" + _nextProbeAtUtc.Value.ToString("o"));
        }

        private void ChangeState(CircuitBreakerState nextState, string transition, string detail)
        {
            State = nextState;
            if (nextState == CircuitBreakerState.Closed)
            {
                _nextProbeAtUtc = null;
            }

            string message = "AUDIT CircuitTransition=" + transition +
                " State=" + nextState +
                (string.IsNullOrEmpty(detail) ? string.Empty : " " + detail);
            if (transition == "ClosedToOpen" || transition == "HalfOpenToOpen")
            {
                _log.Warn(message);
            }
            else
            {
                _log.Info(message);
            }
        }
    }
}

