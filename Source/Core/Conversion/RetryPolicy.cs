using System;
using System.IO;
using System.Xml;

namespace PortfolioSamples.Conversion
{
    /// <summary>변환 결과와 감시가 공유하는 오류 재시도 의존성 규칙이다.</summary>
    public static class RetryPolicy
    {
        /// <summary>
        /// Windows 공유 위반·잠금 위반을 일반 디스크 오류와 구분한다.
        /// </summary>
        /// <remarks>
        /// IOException의 Win32 하위 오류 32/33만 공유·잠금 위반으로 분류한다. 모든 I/O 장애를 파일 잠금으로 해석하지 않는다.
        /// </remarks>
        /// <param name = "exception">처리 정책을 분류하거나 마스킹할 예외.</param>
        /// <returns>설명한 조건을 만족하면 true, 만족하지 않으면 false.</returns>
        public static bool IsFileLocked(Exception exception)
        {
            if (exception == null) return false;
            int code = exception.HResult & 0xffff;
            return exception is IOException && (code == 32 || code == 33);
        }

        /// <summary>
        /// 허용된 파일·입력 오류의 재시도 의존성을 반환한다.
        /// </summary>
        /// <remarks>
        /// 업무/형식 오류는 입력 또는 출력/설정 변경, I/O·권한·시간 초과는 시간 재시도로 분류한다. 취소는 원래 예외로 다시 던지고 분류되지 않은 런타임 오류는 false로 반환한다.
        /// </remarks>
        /// <param name = "exception">처리 정책을 분류하거나 마스킹할 예외.</param>
        /// <param name = "dependency">다음 재시도에 필요한 시간/입력/출력/설정 변경 종류.</param>
        /// <returns>설명한 조건을 만족하면 true, 만족하지 않으면 false.</returns>
        public static bool TryClassify(Exception exception, out RetryDependency dependency)
        {
            if (exception is OperationCanceledException)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
            dependency = default (RetryDependency);
            var conversion = exception as ConversionException;
            if (conversion != null)
            {
                dependency = conversion.ErrorCode == "INTEGRITY001" ? RetryDependency.Output : conversion.ErrorCode.StartsWith("CFG", StringComparison.Ordinal) ? RetryDependency.Settings : conversion.IsWaiting ? RetryDependency.Time : RetryDependency.Input;
                return true;
            }

            if (exception is XmlException || exception is InvalidDataException || exception is FormatException)
            {
                dependency = RetryDependency.Input;
                return true;
            }

            if (exception is IOException || exception is UnauthorizedAccessException || exception is TimeoutException)
            {
                dependency = RetryDependency.Time;
                return true;
            }

            return false;
        }
    }
}

