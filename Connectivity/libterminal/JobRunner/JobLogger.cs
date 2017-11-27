using System;
using System.Collections.Generic;
using System.Text;

namespace libterminal.JobRunner
{
    public class JobLogger
    {
        public enum Level
        {
            Debug,
            Informational,
            Warning,
            Error,
            Critical
        };

        public void WriteDebug(string message) { WriteLine(Level.Debug, message);  }
        public void WriteInformational(string message) { WriteLine(Level.Debug, message); }
        public void WriteWarning(string message) { WriteLine(Level.Debug, message); }
        public void WriteError(string message) { WriteLine(Level.Debug, message); }
        public void WriteCritical(string message) { WriteLine(Level.Debug, message); }

        public void WriteLine(Level level, string message)
        {
        }
    }
}
