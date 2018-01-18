using System;

namespace SolutionManager.Logic.Results
{
    public class ImportSolutionResult : Result
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
        Failure,
        UnableToRetrieve
    }
}
