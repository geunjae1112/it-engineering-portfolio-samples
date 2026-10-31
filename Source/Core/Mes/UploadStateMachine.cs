using System;

namespace PortfolioSamples.Mes
{
    public class UploadStateMachine
    {
        public UploadState Current { get; private set; }
        public UploadStateMachine()
        {
            Current = UploadState.Waiting;
        }
        public void MoveTo(UploadState next)
        {
            if (!CanMove(Current, next))
            {
                throw new InvalidOperationException("Invalid upload state transition. From=" + Current + " To=" + next);
            }
            Current = next;
        }
        public static bool CanMove(UploadState current, UploadState next)
        {
            if (current == next)
            {
                return true;
            }
            if (next == UploadState.MesQuerying)
            {
                return true;
            }
            if (next == UploadState.Failed || next == UploadState.RetryWait ||
                next == UploadState.Indeterminate || next == UploadState.ReconciliationWait ||
                next == UploadState.RecoveryRequired || next == UploadState.DefinitiveFailure)
            {
                return true;
            }
            switch (current)
            {
                case UploadState.Success:
                case UploadState.Failed:
                case UploadState.Indeterminate:
                case UploadState.ReconciliationWait:
                case UploadState.RecoveryRequired:
                case UploadState.DefinitiveFailure:
                    return next == UploadState.Waiting;
                case UploadState.Waiting:
                    return next == UploadState.MesQuerying;
                case UploadState.MesQuerying:
                    return next == UploadState.VisionMatching;
                case UploadState.VisionMatching:
                    return next == UploadState.UploadPending;
                case UploadState.UploadPending:
                    return next == UploadState.Uploading;
                case UploadState.Uploading:
                    return next == UploadState.Success;
                case UploadState.RetryWait:
                    return next == UploadState.Uploading || next == UploadState.Success;
                default:
                    return false;
            }
        }
    }
}

