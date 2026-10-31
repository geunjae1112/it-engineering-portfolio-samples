namespace PortfolioSamples.Mes
{
    public enum UploadState
    {
        Waiting,
        MesQuerying,
        VisionMatching,
        UploadPending,
        Uploading,
        Success,
        Failed,
        RetryWait,
        DefinitiveFailure,
        Indeterminate,
        ReconciliationWait,
        RecoveryRequired
    }
}

