﻿using CommandLine.Text;
using CommandLine;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;

namespace Shelly.Command
{
    [Verb("extract", isDefault: true, aliases: new string[] { "ex", "unpack"}, HelpText = "Extract Assets")]
    internal class OptionExtract
    {
        [Option('O', "output", Required = false, HelpText = "Export location")]
        public string Output { get; set; } = String.Empty;

        [Option('K', "key", Required = false, HelpText = "AES Key of Tower of Fantasy.")]
        public string Key { get; set; } = String.Empty;

        [Value(0, MetaName = "gameDir", HelpText = "Game directory", Required = true)]
        public string GameDir { get; set; } = String.Empty;
    }

    internal class ExtractHandler {
        public static void handle(OptionExtract opts)
        {
            Log.Information("Gamedir: {0}", opts.GameDir);
            Log.Information("Initalizing provider...");

            if (string.IsNullOrEmpty(opts.Output))
            {
                opts.Output = Path.Combine(opts.GameDir, "outputs");

            }
            Log.Information("OutputDir: {0}", opts.Output);

            var provider = new DefaultFileProvider(
                directory: opts.GameDir,
                searchOption: SearchOption.AllDirectories,
                versions: new VersionContainer(EGame.GAME_TowerOfFantasy)
            );
            provider.Initialize();

            string[] pathList = {
            "Hotta/Content/Resources/CoreBlueprints",
            "Hotta/Content/Resources/Dialogues",
            "Hotta/Content/Resources/Icon",
            "Hotta/Content/Resources/Text",
            "Hotta/Content/Resources/UI",
            "Hotta/Content/SevenForest/Data"
        };

            string[] skippedRes =
            {
            "DA_TamingMonster.uasset"
        };

            if (!string.IsNullOrEmpty(opts.Key))
            {
                provider.SubmitKey(new FGuid(), new CUE4Parse.Encryption.Aes.FAesKey(opts.Key));
                Log.Information("Key: {0}", opts.Key);
            }

            try
            {
                var allResources = provider.Files.Where(o =>
                {
                    return pathList.Any(n =>
                    {
                        return o.ToString().Contains(n) && !skippedRes.Contains(o.ToString());
                    });
                });

                Log.Information("Filtered: {0}", allResources.Count());

                foreach (var asset in allResources.Where(a => a.Key.ToLower().Contains("taming")))
                {
                    Log.Information("Parsing {0} at {1}", asset.Value.Name, asset.Value.PathWithoutExtension);
                    try
                    {
                        var curPackage = provider.LoadPackage(asset.Value);
                        Log.Information("{0}", curPackage.NameMap);
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
    }
}