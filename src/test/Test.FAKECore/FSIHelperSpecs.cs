using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_running_script
    {
        
        static string RunExplicit(string scriptFilePath, string arguments, bool useCache)
        {
            var stdOut = Console.Out;

            var sbOut = new System.Text.StringBuilder();
            var outStream = new StringWriter(sbOut);
            Console.SetOut(outStream);
            Tuple<bool, Microsoft.FSharp.Collections.FSharpList<ProcessHelper.ConsoleMessage>> result;
            try
            {
                result = FSIHelper.executeBuildScriptWithArgsAndReturnMessages(scriptFilePath, new string[] { }, useCache, false);
            }
            finally
            {
                Console.SetOut(stdOut); // Now all output start going back to console window
                Console.Write(sbOut.ToString());
            }


            if (!result.Item1)
            {
                var errors = result.Item2.Where(x => x.IsError).Select(x => x.Message);
                throw new Exception("Executing script failed. Errors: \n" + String.Join("\n", errors));
            }
            foreach (var x in result.Item2)
            {
                Console.WriteLine(x.Message);
            }
            var messages = result.Item2.Where(x => !x.IsError).Select(x => x.Message);
            return sbOut.ToString();
        }

        static string Run(string script, string arguments, bool useCache) {
            var scriptFilePath = Path.GetTempFileName() + ".fsx";
            string result;
            try
            {
                File.WriteAllText(scriptFilePath, script);
                result = RunExplicit(scriptFilePath, arguments, useCache);
            }
            finally
            {
                File.Delete(scriptFilePath);
            }
            
            return result;
        }

        static string nl = System.Environment.NewLine;
        static Tuple<string, string> sc(string path, string contents) {
            return new Tuple<string, string>(path.Replace("\\", "/"), contents);
        }

        It should_use_then_invalidate_cache =
            () =>
            {
                var arguments = "";
                var scriptFilePath = Path.GetTempFileName() + ".fsx";
                var scriptFileName = Path.GetFileName(scriptFilePath);
                try
                {
                    File.WriteAllText(scriptFilePath, "printf \"foobar\"");
                    var scriptHash = FSIHelper.getScriptHash(new Tuple<string,string>[] { sc(scriptFilePath, "printf \"foobar\"") });
                    var cacheFilePath = "./.fake/" + scriptFileName + "_" + scriptHash + ".dll";

                    File.Exists(cacheFilePath).ShouldEqual(false);

                    RunExplicit(scriptFilePath, arguments, false).ShouldEqual("foobar");
                    File.Exists(cacheFilePath).ShouldEqual(false);

                    RunExplicit(scriptFilePath, arguments, true).ShouldEqual(
                        "Cache doesnt exist" + nl + "foobar" + nl + "Saved cache" + nl);
                    File.Exists(cacheFilePath).ShouldEqual(true);

                    RunExplicit(scriptFilePath, arguments, true).ShouldEqual("Using cache" + nl + "foobar");

                    File.WriteAllText(scriptFilePath, "printf \"foobarbaz\"");

                    var changedScriptHash = FSIHelper.getScriptHash(new Tuple<string, string>[] { sc(scriptFilePath, "printf \"foobarbaz\"") });
                    RunExplicit(scriptFilePath, arguments, true).ShouldEqual("Cache is invalid, recompiling" + nl + "foobarbaz" + nl + "Saved cache" + nl);
                    //File.Exists(cacheFilePath).ShouldEqual(false);
                    File.Exists("./.fake/" + scriptFileName + "_" + changedScriptHash + ".dll").ShouldEqual(true);

                }
                finally
                {
                    if (File.Exists(scriptFilePath)) File.Delete(scriptFilePath);
                    //if (Directory.Exists("./.fake")) Directory.Delete("./.fake");
                }
            };

        It should_load_file =
            () =>
            {
                var mainPath = Path.GetTempFileName() + ".fsx";
                var loadedPath = Path.GetTempFileName() + ".fsx";
                try
                {
                    var mainScript = "printf \"main\"\n#load \"" + loadedPath.ToString().Replace("\\", "/") + "\"";
                    var loadedScript = "printf \"loaded;\"";
                    File.WriteAllText(mainPath, mainScript);
                    File.WriteAllText(loadedPath, loadedScript);

                    RunExplicit(mainPath, "", false).ShouldEqual("loaded;main");
                }
                finally
                {
                    File.Delete(mainPath);
                    File.Delete(loadedPath);
                }

            };

        It should_change_hash_when_loaded_file_changes =
            () =>
            {
                var middleDirName = Guid.NewGuid().ToString();
                var middleDirPath = Path.Combine(Path.GetTempPath(), middleDirName);
                Directory.CreateDirectory(middleDirPath);

                var mainPath = (Path.GetTempFileName() + ".fsx");
                var middle1Path = Path.Combine(middleDirPath, "middle1.fsx").Replace("\\", "/");
                var middle2Path = Path.Combine(middleDirPath, "middle2.fsx").Replace("\\", "/");
                var lastPath = (Path.GetTempFileName() + ".fsx").Replace("\\", "/");

                var lastScript = "printfn \"foobar\"";
                var middle2Script = "#load @\"" + lastPath + "\"";
                var middle1Script = "#load \"\"\"middle2.fsx\"\"\"";
                var mainScript = "#load \"" + middleDirName + "/middle1.fsx\"";

                File.WriteAllText(mainPath, mainScript);
                File.WriteAllText(middle1Path, middle1Script);
                File.WriteAllText(middle2Path, middle2Script);
                File.WriteAllText(lastPath, lastScript);

                var scriptContents = FSIHelper.getAllScripts(mainPath);
                var hash = FSIHelper.getScriptHash(scriptContents);

                File.WriteAllText(lastPath, "printfn \"foobarbaz\"");
                
                scriptContents = FSIHelper.getAllScripts(mainPath);
                var newHash = FSIHelper.getScriptHash(scriptContents);
                hash.ShouldNotEqual(newHash);
            };
        It should_get_included_assemblies =
            () =>
            {
                var script = 
                    "#r \"justname\"\n" + 
                    "#r \"./relative/path\"\n" +
                    "#r \"C:/absolute/path\"";

                var included = FSIHelper.getIncludedAssembly(script);

                included.ShouldEqual(new string[] { "justname", "./relative/path", "C:/absolute/path" });
            };
    }

}
