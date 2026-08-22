using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MTC.Log
{
    public static class Extensiones
    {
        #region excepciones

        public static void logearExcepcionFallback(this Exception ex, ILoggerSink log, string carpeta, string metodo, string carpetaLogs = "Logs")
        {
            if (log != null)
                log.Error($"__inicializar: {ex.Message}", true);
            else
            {
                string donde, archivo = "error.txt";
                try
                {
                    donde = Path.Combine(carpeta, carpetaLogs);
                    if (!Directory.Exists(donde))
                        Directory.CreateDirectory(donde);

                    archivo = Path.Combine(donde, archivo);
                }
                catch (Exception ex2)
                {
                    archivo = Path.Combine(carpeta, archivo);
                }

                try
                {
                    File.WriteAllText(archivo, $"{metodo}: {ex}");
                }
                catch
                {
                    // No se pudo escribir el log de error, no hay mucho más que hacer.
                }
            }
        }

        public static string armarMensajeErrorExcepcionMetodo(this Exception ex, string prefijo = "", [CallerMemberName] string memberName = "")
        {
            var result = $"{memberName}: {ex.Message}";

            if (!string.IsNullOrEmpty(prefijo))
                result += $". {prefijo}";

            if (ex.InnerException != null)
                result += Environment.NewLine + $"Info. adic.: {ex.InnerException.Message}";
            return result;
        }

        public static string armarMensajeErrorExcepcion(this Exception ex, string prefijo = "", bool trace = false
                                                                     /* [CallerMemberName] string memberName = "",
                                                                      [CallerFilePath] string file = "",
                                                                      [CallerLineNumber] int line = 0*/)
        {
            if (!trace)
            {
                var result = ex.InnerException == null ? ex.Message : $"{ex.Message}. Info. adic.: {ex.InnerException.Message}";
                return string.IsNullOrEmpty(prefijo) ? result : $"{prefijo}: {result}";
            }

            //  return $"Error ejecutando {memberName} en {System.IO.Path.GetFileName(file)}:{line}";
            return string.Join(Environment.NewLine, ex.traceException());
        }

        #region privados
        private static string ExtractBracketed(string str)
        {
            string s;
            if (str.IndexOf('<') > -1) //using the Regex when the string does not contain <brackets> returns an empty string.
                s = Regex.Match(str, @"\<([^>]*)\>").Groups[1].Value;
            else
                s = str;
            if (s == "")
                return "'Emtpy'"; //for log visibility we want to know if something it's empty.
            else
                return s;
        }

        static string ThreadAndDateInfo
        {
            //returns thread number and precise date and time.
            get { return "[" + Thread.CurrentThread.ManagedThreadId + " - " + DateTime.Now.ToString(Logger.K_FORMATO_LOG) + "] "; }
        }
        #endregion
        public static void traceException(this Exception ex, ILogger log)
        {
            foreach (var cadena in ex.traceException())
                log.Error(cadena, excepcion: true);
        }

        public static void traceException(this Exception ex, Action<string> Error)
        {
            foreach (var cadena in ex.traceException())
                Error(cadena);
        }

        public static List<string> traceException(this Exception ex)
        {
            List<string> result = new();
            try
            {
                var site = ex.TargetSite;//Get the methodname from the exception.
                var methodName = site == null ? "" : site.Name;//avoid null ref if it's null.
                methodName = ExtractBracketed(methodName);

                var stkTrace = new System.Diagnostics.StackTrace(ex, true);
                for (int i = 0; i < 3; i++)
                {
                    //In most cases GetFrame(0) will contain valid information, but not always. That's why a small loop is needed. 
                    var frame = stkTrace.GetFrame(i);
                    int lineNum = frame.GetFileLineNumber();//get the line and column numbers
                    int colNum = frame.GetFileColumnNumber();
                    string className = ExtractBracketed(frame.GetMethod().ReflectedType.FullName);
                    result.Add($"{ThreadAndDateInfo}. Exception en: {className}.{methodName}. Ln: {lineNum} Col: {colNum}. Msg: {ex.Message}");

                    if (lineNum + colNum > 0)
                        break; //exit the for loop if you have valid info. If not, try going up one frame...                    
                }
            }
            catch (Exception ee)
            {
                //Avoid any situation that the Trace is what crashes you application. While trace can log to a file. Console normally not output to the same place.
                result.Add($"Tracing exception in traceException(Exception ee):{ee.Message}");
            }
            return result;
        }

        #endregion
    }
}
