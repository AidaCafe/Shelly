using CommandLine.Text;
using CommandLine;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;
using System.Runtime.CompilerServices;
using System.Linq;

namespace Shelly.Command
{
    [Verb("extract", isDefault: true, aliases: new string[] { "ex", "unpack"}, HelpText = "Extract Assets")]
    internal class OptionExtract
    {
        [Value(0, MetaName = "GAME_DIR", HelpText = "Game directory", Required = true)]
        public string GameDir { get; set; } = String.Empty;

        [Option('D', "debug", Required = false, HelpText = "Debug mode")]
        public bool Debug { get; set; }

        [Option('K', "key", Required = false, HelpText = "AES Key of Tower of Fantasy.")]
        public string? Key { get; set; }

        [Option('O', "output", Required = false, HelpText = "Export location")]
        public string? Output { get; set; }

        [Option('F', "filter", Default = new string[] {
            "Hotta/Content/Resources/CoreBlueprints",
            "Hotta/Content/Resources/Dialogues",
            "Hotta/Content/Resources/Icon",
            "Hotta/Content/Resources/Text",
            "Hotta/Content/Resources/UI",
            "Hotta/Content/SevenForest/Data"
        }, Required = false)]
        public IEnumerable<string>? Filter { get; set; }

        [Option('S', "skip", Default = new string[] { "DA_TamingMonster" },Required = false)]
        public IEnumerable<string>? Skipped { get; set; }
    }

    internal class ExtractHandler : AbstractCommand {
        public readonly string gameDir;
        public readonly string outputDir;

        private string? key;
        public readonly string[]? filter;
        public readonly string[]? skipped;

        private readonly DefaultFileProvider provider;
        public ExtractHandler(string gameDir, string output) { 
            this.gameDir = gameDir;
            this.outputDir = output;
            this.provider = new DefaultFileProvider(
                directory: this.gameDir,
                searchOption: SearchOption.AllDirectories,
                versions: new VersionContainer(EGame.GAME_TowerOfFantasy)
            );
        }
        public ExtractHandler(string gameDir, string output, string[] filter, string[] skipped) {
            this.gameDir = gameDir;
            this.outputDir = output;
            this.filter = filter;
            this.skipped = skipped;
            this.provider = new DefaultFileProvider(
                directory: this.gameDir,
                searchOption: SearchOption.AllDirectories,
                versions: new VersionContainer(EGame.GAME_TowerOfFantasy)
            );
        }

        public void Initialize() {
            provider.Initialize();
        }

        public void SetAesKey(string key) {
            if (!string.IsNullOrEmpty(key))
            {
                this.key = key;
                provider.SubmitKey(new FGuid(), new CUE4Parse.Encryption.Aes.FAesKey(key));
            }
        }

        public static ExtractHandler FromOpts(OptionExtract opts) {
            ExtractHandler handler = new(
                gameDir: opts.GameDir,
                output: string.IsNullOrEmpty(opts.Output) ? Path.Combine(opts.GameDir, "output") : opts.Output,
                filter: opts.Filter?.ToArray() ?? Array.Empty<string>(),
                skipped: opts.Skipped?.ToArray() ?? Array.Empty<string>()
            );
            Log.Information("Gamedir: {0}\nOutputDir: {1}", handler.gameDir, handler.outputDir);
            Log.Information("Initializing provider...");
            handler.provider.Initialize();

            if (opts.Key != null) {
                handler.SetAesKey(opts.Key);
                Log.Information("Key: {0}", handler.key);
            }
            return handler;
        }

        public override void Handle()
        {
            Log.Information("Skipped: {0}", skipped);
            try
            {
                var allResources = provider.Files.Where(o =>
                {
                    return this.filter?.Any(n =>
                    {
                        return o.ToString().Contains(n);
                    }) ?? true;
                });

                Log.Information("Filtered: {0}", allResources.Count());

                foreach (var asset in allResources.Where(a => a.Key.ToLower().Contains("taming")))
                {
                    if (skipped?.Any(asset.Value.Path.Contains) ?? false) {
                        continue;
                    }
                    Log.Information("Parsing {0} at {1}", asset.Value.Name, asset.Value.PathWithoutExtension);
                    try
                    {
                        var curPackage = provider.LoadPackage(asset.Value);
                        Log.Information("Cur Name: {0}", curPackage.Name);
                        var allObjects = curPackage.GetExports();
                        foreach (var obj in allObjects)
                        {
                            Log.Information("{0}", JsonConvert.SerializeObject(obj, Formatting.Indented));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Error occurred when parsing {0} : {1}", asset.Value.Path, ex.Message);
                        Log.Debug("Stack Trace \n {0}", ex.StackTrace);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Fail to load res: {0}", e);
                Environment.Exit(1);
            }
        }

        public override AbstractCommand FromOpts()
        {
            throw new NotImplementedException();
        }
    }
}
