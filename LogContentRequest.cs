using System;

namespace MTC.Log
{
    [Serializable]
    public class LogContentRequest
    {
        public LogType Type { get; set; }
        public string SubSystem { get; set; }
    }
}
