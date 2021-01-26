using System;
using System.Collections.Generic;

namespace Tag.Neuron.Xamarin.Services
{
    public interface ILogService
    {
        void AddListener(ILogListener listener);
        void RemoveListener(ILogListener listener);
        void LogWarning(string format, params object[] args);
        void LogException(Exception e);
        void LogException(Exception e, params KeyValuePair<string, string>[] parameters);
        void LogEvent(string name, params KeyValuePair<string, string>[] extraParameters);

        void SaveExceptionDump(string title, string stackTrace);
        string LoadExceptionDump();
        void DeleteExceptionDump();
    }
}