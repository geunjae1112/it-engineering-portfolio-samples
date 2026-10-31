namespace PortfolioSamples.Mes
{
    /// <summary>
    /// MES 요청이 실패한 원인을 통신 장애와 업무 거절로 구분한다.
    /// Circuit Breaker는 이 값 중 일시적인 통신 장애만 실패 횟수로 계산한다.
    /// </summary>
    public enum MesFailureCategory
    {
        None = 0,
        Transport = 1,
        Timeout = 2,
        Server = 3,
        Authentication = 4,
        Business = 5,
        Validation = 6,
        Cancelled = 7,
        Unknown = 8
    }
}

