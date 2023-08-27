using CommandLine;
using Serilog;
using Serilog.Events;
using Shelly.Command;

class Program
{
    class OptionGlobal
    {
        [Option('D', "debug", Required = false, HelpText = "Debug mode")]
        public bool Debug { get; set; }
    }

    static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();


        Parser.Default.ParseArguments<OptionGlobal, OptionExtract>(args)
            .WithParsed<OptionGlobal>(x =>
            {   
                if (x.Debug)
                {
                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.Console()
                        .CreateLogger();
                }
            })
            .WithParsed<OptionExtract>(o  => ExtractHandler.FromOpts(o).Handle())
            .WithNotParsed(HandleParseError);

    }

    static void HandleParseError(IEnumerable<Error> errs)
    {
        Log.Error("Invalid options {0}", errs);
        Environment.Exit(1);
    }
}
