namespace PortfolioSamples.Mes
{
    /// <summary>운영 환경의 고정값 대신 검증한 설정으로 구성합니다.</summary>
    public sealed class CircuitBreakerOptions
    {
        public bool Enabled { get; set; }
        public int FailureThreshold { get; set; }
        public int OpenSeconds { get; set; }
    }
    public interface ILoggerService
    {
        void Info(string message);
        void Warn(string message);
    }
}
