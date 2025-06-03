namespace Integration.Api.Models.Logger
{
    using Exception = System.Exception;

    public class StackTraceException : Exception
    {
        public StackTraceException(string message, string stackTrace)
            : base(message)
        {
            this.StackTrace = stackTrace;
        }

        public override string StackTrace { get; }
    }
}
