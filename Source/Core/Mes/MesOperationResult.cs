namespace PortfolioSamples.Mes
{
    public class MesOperationResult
    {
        public bool Success { get; set; }
        public int ReturnCode { get; set; }
        public string ErrorString { get; set; }
        public MesResultKind ResultKind { get; set; }
        public MesFailureCategory FailureCategory { get; set; }
        public bool IsTransientFailure { get; set; }
        public static MesOperationResult Ok(int returnCode)
        {
            return new MesOperationResult
            {
                Success = true,
                ReturnCode = returnCode,
                ErrorString = string.Empty,
                ResultKind = MesResultKind.Success,
                FailureCategory = MesFailureCategory.None,
                IsTransientFailure = false
            };
        }
        public static MesOperationResult Fail(int returnCode, string errorString)
        {
            return Fail(returnCode, errorString, MesFailureCategory.Business, false);
        }

        /// <summary>
        /// 확정 실패에 원인 분류를 함께 기록한다.
        /// 업무 규칙 거절은 기본 Fail 메서드를 사용하고, 통신 장애만 명시적으로 분류한다.
        /// </summary>
        public static MesOperationResult Fail(int returnCode, string errorString,
            MesFailureCategory failureCategory, bool isTransientFailure)
        {
            return new MesOperationResult
            {
                Success = false,
                ReturnCode = returnCode,
                ErrorString = errorString ?? string.Empty,
                ResultKind = MesResultKind.DefinitiveFailure,
                FailureCategory = failureCategory,
                IsTransientFailure = isTransientFailure
            };
        }

        public static MesOperationResult Indeterminate(int returnCode, string errorString)
        {
            return Indeterminate(returnCode, errorString, MesFailureCategory.Unknown, false);
        }

        /// <summary>
        /// 실제 MES 반영 여부를 확정할 수 없는 실패를 원인 분류와 함께 만든다.
        /// 이 결과는 기존 ResultKind 멱등성 판단을 그대로 유지한다.
        /// </summary>
        public static MesOperationResult Indeterminate(int returnCode, string errorString,
            MesFailureCategory failureCategory, bool isTransientFailure)
        {
            return new MesOperationResult
            {
                Success = false,
                ReturnCode = returnCode,
                ErrorString = errorString ?? string.Empty,
                ResultKind = MesResultKind.Indeterminate,
                FailureCategory = failureCategory,
                IsTransientFailure = isTransientFailure
            };
        }

        /// <summary>
        /// 사용자가 취소한 요청을 통신 장애와 구분해 반환한다.
        /// </summary>
        public static MesOperationResult Cancelled(string errorString)
        {
            return Fail(-499, errorString, MesFailureCategory.Cancelled, false);
        }

        /// <summary>
        /// Circuit Breaker 실패 횟수에 포함할 일시적 장애인지 판정한다.
        /// Server 오류는 일시적이라고 명시된 경우에만 포함한다.
        /// </summary>
        public bool CountsAsCircuitFailure()
        {
            if (FailureCategory == MesFailureCategory.Transport ||
                FailureCategory == MesFailureCategory.Timeout)
            {
                return true;
            }

            return FailureCategory == MesFailureCategory.Server && IsTransientFailure;
        }

        public static MesResultKind Classify(MesOperationResult result)
        {
            if (result == null)
            {
                return MesResultKind.Indeterminate;
            }
            if (result.Success)
            {
                return MesResultKind.Success;
            }
            if (result.ResultKind == MesResultKind.Indeterminate || result.ReturnCode == -408)
            {
                return MesResultKind.Indeterminate;
            }
            return MesResultKind.DefinitiveFailure;
        }
    }
}

