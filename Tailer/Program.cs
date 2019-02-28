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

        /// <inheritdoc />
        [Option(ShortName = "p", Description = "The path to watch")]
        public string Path { get; } = "/tmp/access.log";

        /// <inheritdoc />
        [Option(CommandOptionType.SingleOrNoValue, ShortName = "s",
            Description = "If specified, skip over existing content in the path (otherwise, processes current content and then tails new content)")]
        public bool SkipContent { get; } = false;

        /// <inheritdoc />
        [Option(ShortName = "i", Description = "The number of seconds between statistics reports (defaults to 10s)")]
        public int Interval { get; } = 10;

        /// <inheritdoc />
        [Option(ShortName = "w", Description = "The number of seconds of traffic to consider when triggering threshold limits")]
        public int Window { get; } = 120;

        /// <inheritdoc />
        [Option(ShortName = "t", Description = "The limit of requests per second that triggers high traffic alerts")]
        public int Threshold { get; } = 10;

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
            using (var scope = Container.BeginLifetimeScope())
            {
                System.Console.WriteLine($"Processing {Path}");
            }
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
