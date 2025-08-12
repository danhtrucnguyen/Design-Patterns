using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Design_Patterns.Creational_Pattern
{
    public sealed class LoggerSingleton
    {
        private static readonly Lazy<LoggerSingleton> _instance =
            new(() => new LoggerSingleton(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static LoggerSingleton Instance => _instance.Value;

        private readonly ConcurrentQueue<string> _buffer = new();

        private LoggerSingleton() { }

        public void Write(string message)
        {
            var line = $"{DateTime.UtcNow:O} [LOG] {message}";
            _buffer.Enqueue(line);
        }

        public string[] Snapshot() => _buffer.ToArray();

        public void Log(string message) => Write(message);
        public string[] GetLogs() => Snapshot();
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== Singleton Pattern Demo ===");

            var logger1 = LoggerSingleton.Instance;
            logger1.Log("Thong bao tu Logger 1");

            var logger2 = LoggerSingleton.Instance;
            logger2.Log("Thong bao tu Logger 2");

            Console.WriteLine(ReferenceEquals(logger1, logger2)
                ? "logger1 va logger2 la cung mot instance."
                : "logger1 va logger2 KHONG phai la cung mot instance.");

            Console.WriteLine("\n=== Toan bo log ===");
            foreach (var entry in logger1.GetLogs())
                Console.WriteLine(entry);
        }
    }
}
