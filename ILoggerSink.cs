using System.Runtime.CompilerServices;

namespace MTC.Log
{
    public interface ILoggerSink
    {
        void Debug(string mensaje, [CallerMemberName] string memberName = "");
        void Error(string mensaje, bool excepcion, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0);
        void Info(string mensaje, [CallerMemberName] string memberName = "");
        void Warn(string mensaje, [CallerMemberName] string memberName = "");

        /// <summary>Si el nivel Debug esta prendido para ESTE logger. Se consulta antes de armar el
        /// mensaje: el $"..." se evalua igual aunque NLog despues descarte la linea.</summary>
        bool IsDebugEnabled { get; }
    }
}