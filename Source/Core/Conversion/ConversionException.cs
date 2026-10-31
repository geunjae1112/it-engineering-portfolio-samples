using System;
namespace PortfolioSamples.Conversion
{
    public enum RetryDependency { Time, Input, Output, Settings }

    /// <summary>원문 파일·경로를 메시지에 포함하지 않는 업무 오류입니다.</summary>
    public sealed class ConversionException : Exception
    {
        public string ErrorCode { get; private set; }
        public bool IsWaiting { get; private set; }
        public ConversionException(string code, bool waiting)
            : base("변환 입력 또는 설정을 확인하세요.")
        {
            if (String.IsNullOrWhiteSpace(code)) throw new ArgumentException("오류 코드가 필요합니다.", "code");
            ErrorCode = code;
            IsWaiting = waiting;
        }
    }
}
