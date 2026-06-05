using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace LemonFramework.UProfiler.Core
{
    public static class LogManager
    {
        static FileStream _logFileStream;
        static string _logFilePath = null;

        public static void CreateLogFile(string path, FileMode fileMode = FileMode.Append)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Invalid log file path.");
                return;
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            _logFileStream = new FileStream(path, fileMode);
            _logFilePath = path;
        }

        public static void CloseLogFile()
        {
            if (_logFileStream == null) return;
            _logFilePath = null;
            _logFileStream.Flush();
            _logFileStream.Close();
            _logFileStream.Dispose();
            _logFileStream = null;
        }

        public static void Log(string msg)
        {
            if (ShareDatas.ShowDebugLog)
                Debug.Log(msg);
            if (ShareDatas.WriteLogToFile)
                LogToFile(LogType.Log, $"<font color=\"#0000FF\">[Log]</font>{msg}");
        }

        public static void LogError(string msg)
        {
            if (ShareDatas.ShowDebugLog)
                Debug.LogError(msg);
            if (ShareDatas.WriteLogToFile)
                LogToFile(LogType.Error, $"<font color=\"#FF0000\">[Error]</font>{msg}");
        }

        public static void LogWarning(string msg)
        {
            if (ShareDatas.ShowDebugLog)
                Debug.LogWarning(msg);
            if (ShareDatas.WriteLogToFile)
                LogToFile(LogType.Warning, $"<font color=\"#FFD700\">[Warning]</font>{msg}");
        }

        public static void LogAssert(string msg)
        {
            if (ShareDatas.ShowDebugLog)
                Debug.LogAssertion(msg);
            if (ShareDatas.WriteLogToFile)
                LogToFile(LogType.Assert, $"<font color=\"#FF0000\">[Assert]{msg}</font>");
        }

        public static void LogException(Exception ex)
        {
            if (ShareDatas.ShowDebugLog)
                Debug.LogException(ex);
            if (ShareDatas.WriteLogToFile)
                LogToFile(LogType.Exception, $"<font color=\"#FF0000\">[Exception]</font>{ex.ToString()}");
        }

        public static void LogToFile(string logString, string stackTrace, LogType type)
        {
            LogToFile(type, logString, stackTrace);
        }

        static string ColorTypeLog(LogType type, string msg)
        {
            var log = string.Empty;
            switch (type)
            {
                case LogType.Log:
                    log = $"<font color=\"#0000FF\">[Log]{msg.TrimEnd()}</font>";
                    break;
                case LogType.Error:
                    log = $"<font color=\"#FF0000\">[Error]{msg.TrimEnd()}</font>";
                    break;
                case LogType.Warning:
                    log = $"<font color=\"#FFD700\">[Warning]{msg.TrimEnd()}</font>";
                    break;
                case LogType.Assert:
                    log = $"<font color=\"#FF0000\">[Assert]{msg.TrimEnd()}</font>";
                    break;
                case LogType.Exception:
                    log = $"<font color=\"#FF0000\">[Assert]{msg.TrimEnd()}</font>";
                    break;
            }

            return log;
        }

        /// <summary>Append a log line to the log file.</summary>
        private static void LogToFile(LogType logType, string msg, string stackTrace = null)
        {
            try
            {
                if (!msg.EndsWith("\n"))
                {
                    msg += "\n";
                }

                var nowTime = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                var logStr = $"[{nowTime}]{ColorTypeLog(logType, msg)} \n\rstackTrace:{stackTrace}";
                byte[] data = Encoding.Default.GetBytes(logStr);

                if (_logFileStream == null)
                {
                    if (string.IsNullOrEmpty(_logFilePath))
                    {
                        if (string.IsNullOrEmpty(ShareDatas.StartTimeStr))
                            ShareDatas.StartTimeStr = nowTime.Replace(" ", "_").Replace("/", "_")
                                .Replace(":", "_");
                        _logFilePath = Application.persistentDataPath + "/" + $"log_{ShareDatas.StartTime}.txt";
                    }

                    _logFileStream = new FileStream(_logFilePath, FileMode.Append);
                }

                _logFileStream.Write(data, 0, data.Length);
                _logFileStream.Flush();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
        }
    }
}