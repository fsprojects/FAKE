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
            return 
                sbOut.ToString()
                .Replace("Running Buildscript: " + scriptFilePath, "")
                .Replace("\n", "").Replace("\r", "");
        }

        static string Run(string script, string arguments, bool useCache)
        {
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

        static FSIHelper.Script script(string path, string contents)
        {
            return new FSIHelper.Script(contents, path.Replace("\\", "/"), null, null);
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
                    var scriptHash =
                            FSIHelper.getScriptHash(new FSIHelper.Script[] { script(scriptFilePath, "printf \"foobar\"") }, new List<string>());

                    var cacheFilePath = Path.Combine(".", ".fake", scriptFileName + "_" + scriptHash + ".dll");

                    File.Exists(cacheFilePath).ShouldEqual(false);

                    RunExplicit(scriptFilePath, arguments, false)
                       .ShouldEqual("foobar");

                    File.Exists(cacheFilePath).ShouldEqual(false);

                    RunExplicit(scriptFilePath, arguments, true)
                        .ShouldStartWith("Cache doesn't exist");

                    File.Exists(cacheFilePath).ShouldEqual(true);

                    RunExplicit(scriptFilePath, arguments, true)
                        .ShouldEqual(
                            ("Using cache" + nl + "foobar")
                            .Replace("\n", "").Replace("\r", ""));

                    File.WriteAllText(scriptFilePath, "printf \"foobarbaz\"");

                    var changedScriptHash = FSIHelper.getScriptHash(new FSIHelper.Script[] { script(scriptFilePath, "printf \"foobarbaz\"") }, new List<string>());
                    RunExplicit(scriptFilePath, arguments, true)
                        .ShouldStartWith("Cache is invalid, recompiling");

                    File.Exists("./.fake/" + scriptFileName + "_" + changedScriptHash + ".dll").ShouldEqual(true);
                }
                finally
                {
                    if (File.Exists(scriptFilePath))
                        File.Delete(scriptFilePath);
                }
            };

        It should_load_file =
            () =>
            {
                var mainPath = Path.GetTempFileName() + ".fsx";
                var loadedPath = Path.GetTempFileName() + ".fsx";
                try
                {
                    var mainScript =
                        "printf \"main\"\n#load \"" +
                            loadedPath.ToString().Replace("\\", "/") + "\"";
                    var loadedScript = "printf \"loaded;\"";
                    File.WriteAllText(mainPath, mainScript.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\r", nl));
                    File.WriteAllText(loadedPath, loadedScript.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\r", nl));

                    RunExplicit(mainPath, "", false)
                        .ShouldEqual("loaded;main");
                }
                finally
                {
                    if (File.Exists(mainPath))
                        File.Delete(mainPath);
                    if (File.Exists(loadedPath))
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
                var hash = FSIHelper.getScriptHash(scriptContents, new List<string>());

                File.WriteAllText(lastPath, "printfn \"foobarbaz\"");

                scriptContents = FSIHelper.getAllScripts(mainPath);
                var newHash = FSIHelper.getScriptHash(scriptContents,new List<string>());
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
