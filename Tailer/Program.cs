using Autofac;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.IO;

namespace Tailer
{
    public class Program : IConfiguration
    {
        public static IContainer Container { get; set; }

        /// <inheritdoc />
        [Option(ShortName = "p", Description = "The path to watch")]
        public string Path { get; set; } = "/tmp/access.log";

        /// <inheritdoc />
        [Option(CommandOptionType.NoValue, ShortName = "e", Description = "Include existing content in the file")]
        public bool ParseExisting { get; } = false;

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
                var processor = scope.Resolve<IStreamProcessor>();

                using (var stream = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                {
                    // Don't process the existing content of the file
                    processor.Process(stream, ParseExisting ? 0 : stream.Length);
                }
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
            builder.RegisterType<TailProcessor>().As<IStreamProcessor>();

            Container = builder.Build();
        }
    }
}
