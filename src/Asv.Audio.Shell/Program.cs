using System.Reflection;
using System.Text;
using Asv.AirTalk.Payload;
using NLog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Asv.Audio.Shell;

class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static int Main(string[] args)
    {
        HandleExceptions();
        try
        {
            Assembly.GetExecutingAssembly().PrintWelcomeToConsole();
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            var app = new CommandApp();

            app.Configure(config =>
            {
                config.AddCommand<WindowsLoopCommand>(WindowsLoopCommand.Name);

#if DEBUG
                config.PropagateExceptions();
                config.ValidateExamples();
#endif
            });
            return app.Run(args);
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex);
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return -99;
        }
    }

    private static void HandleExceptions()
    {
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Logger.Fatal(
                args.Exception,
                $"Task scheduler unobserver task exception from '{sender}': {args.Exception.Message}"
            );
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            Logger.Fatal(
                $"Unhandled AppDomain exception. Sender '{sender}'. Args: {eventArgs.ExceptionObject}"
            );
        };
    }
}
