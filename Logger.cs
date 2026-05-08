using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace MTC.Log
{
    public class Logger : ILogger, ICreateLogger
    {
        public const string K_FORMATO_LOG = "dd/MM/yy HH:mm:ss.ffffff";
        public const ConsoleColor colorDebug = ConsoleColor.Yellow;
        public const ConsoleColor colorWarn = ConsoleColor.DarkMagenta;
        public const ConsoleColor colorError = ConsoleColor.Red;
        public const ConsoleColor colorInfo = ConsoleColor.Green;

        #region variables
        static LogFactory _factory = null;
        static readonly Lock _lockFactory = new();
        static Encoding _enc1252 = null;
        ILoggerSink logearMensaje;
        bool logeaConsola = false;
        #endregion

        #region propiedades
        public NLog.Logger log { get; private set; }
        Encoding enc1252
        {
            get
            {
                if (_enc1252 == null)
                    _enc1252 = CodePagesEncodingProvider.Instance.GetEncoding(1252);
                return _enc1252;
            }
        }

        public string ConfigFile { get; private set; }
        #endregion

        #region ctor
        public Logger(bool logeaConsola) { this.logeaConsola = logeaConsola; }
        public Logger(string nombreLog, string archivoConfigLog, bool logeaConsola = false, ILoggerSink logearMensaje = null)
        {
            if (!string.IsNullOrEmpty(this.ConfigFile = archivoConfigLog))
                CreateLog(nombreLog, archivoConfigLog);
            this.logeaConsola = logeaConsola;
            this.logearMensaje = logearMensaje;
        }
        #endregion

        #region mensajes

        /// <summary>
        /// Dejamos los nombres de los metodos en Case capital por compatibilidad con NLog
        /// </summary>
        /// <param name="mensaje"></param>
        /// <param name="memberName"></param>
        public void Warn(string mensaje, [CallerMemberName] string memberName = "")
        {
            if (logeaConsola)
                escribir(memberName, mensaje, colorDebug);

            if (log != null)
                log.Warn($"{memberName}. {mensaje}");

            if (logearMensaje != null)
                logearMensaje.Warn($"{memberName}. {mensaje}");
        }

        /// <summary>
        /// Dejamos los nombres de los metodos en Case capital por compatibilidad con NLog
        /// </summary>
        /// <param name="mensaje"></param>
        /// <param name="memberName"></param>
        public void Debug(string mensaje, [CallerMemberName] string memberName = "")
        {
            if (logeaConsola)
                escribir(memberName, mensaje, colorDebug);

            if (log != null)
                log.Debug($"{memberName}. {mensaje}");

            if (logearMensaje != null)
                logearMensaje.Debug($"{memberName}. {mensaje}");
        }

        /// <summary>
        /// Dejamos los nombres de los metodos en Case capital por compatibilidad con NLog
        /// </summary>
        /// <param name="mensaje"></param>
        /// <param name="memberName"></param>
        public void Error(string mensaje, bool excepcion, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (logeaConsola)
            {
                if (excepcion)
                    escribir(memberName, mensaje, colorError, filePath, lineNumber);
                else
                    escribir(memberName, mensaje, colorError, filePath: "", lineNumber: 0);
            }

            if (log != null)
            {
                if (excepcion)
                    log.Error($"{memberName}. {Properties.Resources.K_ARCHIVO}:{filePath}. {Properties.Resources.K_NRO_LINEA}: {lineNumber}. {mensaje}");
                else
                    log.Error($"{mensaje}");
            }

            if (logearMensaje != null)
                logearMensaje.Error($"{memberName}. {Properties.Resources.K_ARCHIVO}:{filePath}. {Properties.Resources.K_NRO_LINEA}: {lineNumber}. {mensaje}", excepcion: excepcion);
        }

        public void Info(string mensaje, [CallerMemberName] string memberName = "")
        {
            if (logeaConsola)
                escribir(memberName, mensaje, colorInfo);

            if (log != null)
                log.Info($"{memberName}. {mensaje}");

            if (logearMensaje != null)
                logearMensaje.Info($"{memberName}. {mensaje}");
        }

        void escribir(string memberName, string mensaje, ConsoleColor color, string filePath = "", int lineNumber = 0)
        {
            Console.ForegroundColor = color;

            if (string.IsNullOrEmpty(filePath))
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss:fff dd/MM/yy")}-{memberName}. {mensaje}");
            else
            {
                if (lineNumber == 0)
                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss:fff dd/MM/yy")}-{memberName}. {Properties.Resources.K_ARCHIVO}:{filePath}. {mensaje}");
                else
                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss:fff dd/MM/yy")}-{memberName}. {Properties.Resources.K_ARCHIVO}:{filePath}. {Properties.Resources.K_NRO_LINEA}: {lineNumber}. {mensaje}");
            }
        }
        #endregion

        public void CreateLog(string nombreLog, string archivoConfigLog)
        {
            lock (_lockFactory)
            {
                if (_factory == null)
                    _factory = new LogFactory().Setup()
                        .LoadConfiguration(new XmlLoggingConfiguration(archivoConfigLog))
                        .LogFactory;
            }
            log = _factory.GetLogger(nombreLog);
        }

        string getLogFileName(string targetName)
        {
            string fileName = null;
            if (log.Factory.Configuration != null && log.Factory.Configuration.ConfiguredNamedTargets.Count != 0)
            {
                Target target = log.Factory.Configuration.FindTargetByName(targetName);
                if (target == null)
                    throw new Exception($"No se pudo encontrar el target Could not find target: {targetName}");

                FileTarget fileTarget = null;
                var wrapperTarget = target as WrapperTargetBase;

                // Unwrap the target if necessary.
                if (wrapperTarget == null)
                    fileTarget = target as FileTarget;
                else
                    fileTarget = wrapperTarget.WrappedTarget as FileTarget;

                if (fileTarget == null)
                    throw new Exception($"No se pudo obtener un FileTarget de {target.GetType()}");

                var logEventInfo = new LogEventInfo { TimeStamp = DateTime.Now };
                fileName = fileTarget.FileName.Render(logEventInfo);
            }
            else
                throw new Exception("LogManager no contiene una configuracion o bien no existen targets con nombre");

            if (!File.Exists(fileName))
                throw new Exception($"El archivo {fileName} no existe");

            return fileName;
        }

        public string GetLogContent(LogContentRequest request) => __contenidoArchivoLog(request);

        string __contenidoArchivoLog(LogContentRequest request)
        {
            var result = string.Empty;
            try
            {
                var subsistema = (string.IsNullOrEmpty(request.SubSystem) ? "" : request.SubSystem).Trim();
                var targetName = request.Type switch
                {
                    LogType.Error => string.IsNullOrEmpty(subsistema) ? "errorLog" : $"{subsistema}ErrorLog",
                    LogType.Debug => string.IsNullOrEmpty(subsistema) ? "debugLog" : $"{subsistema}DebugLog",
                    LogType.Info => string.IsNullOrEmpty(subsistema) ? "infoLog" : $"{subsistema}InfoLog",
                    LogType.Warning => string.IsNullOrEmpty(subsistema) ? "warnLog" : $"{subsistema}WarnLog",
                    _ => throw new ArgumentOutOfRangeException()
                };

                var archivo = getLogFileName(targetName);
                using (var fs = new FileStream(archivo, FileMode.Open, FileAccess.Read))
                using (var sr = new StreamReader(fs, enc1252))
                    result = sr.ReadToEnd();
            }
            catch (Exception ex) { result = ex.Message; }
            return result;
        }

    }
}