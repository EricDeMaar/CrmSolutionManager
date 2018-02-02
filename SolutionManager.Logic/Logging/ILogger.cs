using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionManager.Logic.Logging
{
    public interface ILogger
    {
        void Debug(object message);
        void Debug(object message, Exception ex);
        void Info(object message);
        void Info(object message, Exception ex);
        void Warning(object message);
        void Warning(object message, Exception ex);
        void Error(object message);
        void Error(object message, Exception ex);
        void Fatal(object message);
        void Fatal(object message, Exception ex);
    }
}
