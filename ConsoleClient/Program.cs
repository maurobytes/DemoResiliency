using ConsoleClient.OutputHelpers;
using ConsoleClient.Scenarios;
using System;
using System.Linq;
using System.Threading;

namespace ConsoleClient
{
    class Program
    {
        private static readonly object lockObject = new object();

        static void Main(string[] args)
        {
            Statistic[] statistics = new Statistic[0];

            var progress = new Progress<Progress>();
            progress.ProgressChanged += (sender, progressArgs) =>
            {
                foreach (var message in progressArgs.Messages)
                {
                    WriteLineInColor(message.Message, message.Color.ToConsoleColor());
                }
                statistics = progressArgs.Statistics;
            };

            CancellationTokenSource cancellationSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationSource.Token;

            //Escenario 1
            new NoPolicyAsync().ExecuteAsync(cancellationToken, progress).Wait();
            //Escenario 2
            //new RetryNTimesAsync().ExecuteAsync(cancellationToken, progress).Wait();
            //Escenario 3
            //new Wrap_Fallback_WaitAndRetry_CircuitBreaker_Async().ExecuteAsync(cancellationToken, progress).Wait();

            // Keep the console open.
            Console.ReadKey();
            cancellationSource.Cancel();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            // Output statistics.
            int longestDescription = statistics.Max(s => s.Description.Length);
            foreach (Statistic stat in statistics)
            {
                WriteLineInColor(stat.Description.PadRight(longestDescription) + ": " + stat.Value, stat.Color.ToConsoleColor());
            }

            // Keep the console open.
            Console.ReadKey();
        }

        public static void WriteLineInColor(string msg, ConsoleColor color)
        {
            lock (lockObject)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(msg);
                Console.ResetColor();
            }
        }

    }
}
