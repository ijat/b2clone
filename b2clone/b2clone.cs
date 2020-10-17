using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using b2clone.Models;
using lib_b2clone;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.File.GZip;

namespace b2clone
{
    static class Program
    {
        private static string userConfPath = "user.conf.txt";
        static async Task Main()
        {
            InitLogging();

            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

                Log.Information("[b2clone v{0}]", fileVersionInfo.ProductVersion);
                Console.Title = $"[b2clone v{fileVersionInfo.ProductVersion}]";
                
                if (!File.Exists(userConfPath))
                {
                    Log.Information("User configuration file does not exists.");
                    Log.Information("A new template has been auto generated. Edit {0} as required.", userConfPath);
                    await File.WriteAllTextAsync(userConfPath,
                        JsonConvert.SerializeObject(new UserConf(true), Formatting.Indented));
                    End();
                }

                UserConf userConf = JsonConvert.DeserializeObject<UserConf>(File.ReadAllText(userConfPath));
                if (userConf != null)
                {
                    B2Lib b2Lib = new B2Lib(userConf.KeyId, userConf.ApplicationId, userConf.BucketId);
                    foreach (KeyValuePair<string, string> pathKeyPair in userConf.PathMapper)
                    {
                        await b2Lib.ScanFolderForUploads(pathKeyPair.Value, pathKeyPair.Key);
                    }
                    
                    await b2Lib.UploadToB2();
                }
                else 
                    throw new Exception("Unable to read user configuration. Please check again your configuration file.");

                Log.CloseAndFlush();
                End();
            }
            catch (Exception e)
            {
                Log.Fatal(e.ToString());
            }
        }

        private static void End()
        {
            Console.Write("Press ENTER to exit");
            while (true)
            {
                while (Console.KeyAvailable) 
                    Console.ReadKey(true);
                if (Console.ReadKey(true).Key == ConsoleKey.Enter)
                    Environment.Exit(0);
            }
        }

        private static void InitLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File("b2clone-log.txt.gz", hooks: new GZipHooks())
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information,
                    outputTemplate: "{Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            Log.Verbose("Log initiated");
        }
    }
}