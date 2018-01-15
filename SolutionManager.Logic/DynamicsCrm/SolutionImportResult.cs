using System;

namespace SolutionManager.Logic.DynamicsCrm
{
    public struct SolutionImportResult
    {
        public Guid ImportJobId;
        public ImportResultStatus Status;
        public int ErrorCode;
        public string ErrorMessage;
        public string Data;
    }

    public enum ImportResultStatus
    {
        Success,
        Warning,
        Failure
    }
}
