namespace MTC.Log
{
    public interface ILogger : ICreateLogger, ILoggerSink
    {
        string ConfigFile { get; }
        string GetLogContent(LogContentRequest request);        
    }
}