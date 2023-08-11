using System;
using CommandLine;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;

class Program
{
    class Options
    {
        [Option('d', "debug", Required = false, HelpText = "Show additional debug information")]
        public bool Debug { get; set; }

        [Option('O', "output", Required = false, HelpText = "Export location")]
        public string Output { get; set; } = String.Empty;

        [Option('K', "key", Required = false, HelpText = "AES Key of Tower of Fantasy.")]
        public string Key { get; set; } = String.Empty;

        [Value(0, MetaName = "gameDir", HelpText = "Game directory", Required = true)]
        public string GameDir { get; set; } = String.Empty;
    }

    static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();


        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(RunOptions)
            .WithNotParsed(HandleParseError);
        
    }

    static void RunOptions(Options opts)
    {
        if (string.IsNullOrEmpty(opts.Output))
        {
            opts.Output = Path.Combine(opts.GameDir, "outputs");
        }

        Log.Information("Gamedir: {0} OutputDir: {1}", opts.GameDir, opts.Output);
        Log.Information("Initalizing provider...");

        var provider = new DefaultFileProvider(
            directory: opts.GameDir,
            searchOption: SearchOption.AllDirectories,
            versions: new VersionContainer(EGame.GAME_TowerOfFantasy)
        );
        
        if (!string.IsNullOrEmpty(opts.Key)) {
            provider.SubmitKey(new FGuid(), new CUE4Parse.Encryption.Aes.FAesKey(opts.Key));
        }
      
    }

    static void HandleParseError(IEnumerable<Error> errs)
    {
        Log.Error("Invalid options {0}", errs);
        Environment.Exit(1);
    }
}
