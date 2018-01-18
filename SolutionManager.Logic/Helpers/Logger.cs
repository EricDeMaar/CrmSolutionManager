using System;
using System.Globalization;

namespace SolutionManager.Logic.Helpers
{
    public static class Logger
    {
        public static void Log(string message) => Console.WriteLine($"[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}] - {message}");
    }
}
