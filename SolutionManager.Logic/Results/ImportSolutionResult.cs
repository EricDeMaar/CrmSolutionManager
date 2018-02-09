using System;

namespace SolutionManager.Logic.Results
{
    public class ImportSolutionResult : Result
    {
        public Guid ImportJobId { get; set; }
        public ImportResultStatus Status { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string Data { get; set; }
    }

    public enum ImportResultStatus
    {
        Success,
        Warning,
        Failure,
        UnableToRetrieve
    }
}
