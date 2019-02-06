using Autofac;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.IO;

namespace Tailer
{
    public class Program : IConfiguration
    {
        private static IContainer Container { get; set; }

        /// <summary>
        /// Delegates argument handling to CommandLineApplication
        /// </summary>
        /// <param name="args">The command-line arguments</param>
        public static void Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        /// <summary>
        /// Handles execution of the application
        /// </summary>
        public void OnExecute()
        {
            // Initialize DI Container after parameter parsing
            Initialize();
            System.Console.WriteLine($"Processing {Path}");
        }

        /// <summary>
        /// Initialize the DI Container
        /// </summary>
        private void Initialize()
        {
            if (null != Container) return;

            var builder = new ContainerBuilder();
            // NLog is a handy way to do output without worrying about where it's going
            builder.Register(l => new LoggerFactory().AddNLog()).As<ILoggerFactory>();
            builder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>)).SingleInstance();

            Container = builder.Build();
        }
    }
}
