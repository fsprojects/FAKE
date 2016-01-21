using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Fake;
using Machine.Specifications;
using Messages = Microsoft.FSharp.Collections.FSharpList<Fake.ProcessHelper.ConsoleMessage>;

namespace Test.FAKECore
{
    public class when_running_script
    {
        class MyTracer : Fake.TraceListener.ITraceListener
        {
            StringBuilder builder;
            public MyTracer(StringBuilder builder)
            {
                this.builder = builder;
            }

            public void Write(TraceListener.TraceData value)
            {
                if (value.Message != null)
                {
                    builder.Append(value.Message.Value);
                }

                if (value.NewLine != null)
                {
                    builder.AppendLine();
                }
            }

            public override string ToString() { return builder.ToString(); }
        }

        static string[] EmptyArgs = new string[0];

        static Messages IgnoreDateTimeOffset(Messages msgs)
        {
            // We don't care about the datetimeoffset.
            var mapFunc = FSharpFuncUtil.ToFSharpFunc<Fake.ProcessHelper.ConsoleMessage, Fake.ProcessHelper.ConsoleMessage>(cm =>
                new ProcessHelper.ConsoleMessage(cm.IsError, cm.Message, new DateTimeOffset()));
            return Microsoft.FSharp.Collections.ListModule.Map(mapFunc, msgs);
        }

        static Tuple<Messages, string> RunExplicit(string scriptFilePath, string[] scriptArguments, string[] fsiArguments, bool useCache)
        {
            var result = RunExplicitWithResult(scriptFilePath, scriptArguments, fsiArguments, useCache);
            if (!result.Item1)
            {
                var errors = result.Item2.Select(x => x.Message);
                throw new Exception("Executing script failed. Output: \n" + String.Join("\n", errors));
            }
            return Tuple.Create(result.Item2, result.Item3);
        }

        static Tuple<bool, Messages, string> RunExplicitWithResult(string scriptFilePath, string[] scriptArguments, string[] fsiArguments, bool useCache)
        {
            var stdOut = Console.Out;

            var sbOut = new System.Text.StringBuilder();
            var outStream = new StringWriter(sbOut);
            Console.SetOut(outStream);
            Tuple<bool, Messages> result;

            try
            {

                result = FSIHelper.executeBuildScriptWithArgsAndFsiArgsAndReturnMessages(
                    scriptFilePath, scriptArguments, fsiArguments, useCache);
            }
            finally
            {
                Console.SetOut(stdOut); // Now all output start going back to console window
                Console.Write(sbOut.ToString());
            }


            foreach (var x in result.Item2)
            {
                Console.WriteLine(x.Message);
            }

            return Tuple.Create(result.Item1, result.Item2,
                sbOut.ToString()
                .Replace("Running Buildscript: " + scriptFilePath, "")
                .Replace("\n", "").Replace("\r", ""));
        }

        static string RunExplicit(string scriptFilePath, string[] scriptArguments, bool useCache)
        {
            return RunExplicit(scriptFilePath, scriptArguments, EmptyArgs, useCache).Item2;
        }

        static string Run(string script, string[] scriptArguments, bool useCache)
        {
            var scriptFilePath = Path.GetTempFileName() + ".fsx";
            string result;
            try
            {
                File.WriteAllText(scriptFilePath, script);
                result = RunExplicit(scriptFilePath, scriptArguments, useCache);
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

        It fallback_should_not_trigger_on_build_errors =
            () =>
            {
                var scriptFilePath = Path.GetTempFileName() + ".fsx";
                var scriptFileName = Path.GetFileName(scriptFilePath);
                try
                {
                    var scriptText = @"failwith ""some exn""";
                    File.WriteAllText(scriptFilePath, scriptText);
                    var scriptHash =
                            FSIHelper.getScriptHash(new FSIHelper.Script[] { script(scriptFilePath, scriptText) }, new List<string>());

                    var cacheFilePath = Path.Combine(".", ".fake", scriptFileName + "_" + scriptHash + ".dll");

                    File.Exists(cacheFilePath).ShouldEqual(false);

                    var res1 = RunExplicitWithResult(scriptFilePath, EmptyArgs, EmptyArgs, true);
                    res1.Item1.ShouldBeFalse();
                    res1.Item3.ShouldStartWith("Cache doesn't exist");

                    File.Exists(cacheFilePath).ShouldEqual(true);

                    var res2 = RunExplicitWithResult(scriptFilePath, EmptyArgs, EmptyArgs, true);
                    res2.Item1.ShouldBeFalse();
                    res2.Item3.ShouldContain("Using cache");
                    res2.Item3.ShouldNotContain("Cache is invalid, recompiling");
                    res2.Item3.ShouldNotContain("Cache doesn't exist");
                }
                finally
                {
                    if (File.Exists(scriptFilePath))
                        File.Delete(scriptFilePath);
                }
            };

        It fallback_to_compiling_when_cache_is_broken =
            () =>
            {
                var scriptFilePath = Path.GetTempFileName() + ".fsx";
                var scriptFileName = Path.GetFileName(scriptFilePath);
                try
                {
                    File.WriteAllText(scriptFilePath, "printf \"foobar\"");
                    var scriptHash =
                            FSIHelper.getScriptHash(new FSIHelper.Script[] { script(scriptFilePath, "printf \"foobar\"") }, new List<string>());

                    var cacheFilePath = Path.Combine(".", ".fake", scriptFileName + "_" + scriptHash + ".dll");

                    File.Exists(cacheFilePath).ShouldEqual(false);

                    RunExplicit(scriptFilePath, EmptyArgs, true)
                        .ShouldStartWith("Cache doesn't exist");

                    File.Exists(cacheFilePath).ShouldEqual(true);
                    File.WriteAllBytes(cacheFilePath, new byte[] { 8 });

                    var result = RunExplicit(scriptFilePath, EmptyArgs, true);
                    result.ShouldContain("Using cache");
                    result.ShouldContain("Cache is invalid, recompiling");
                }
                finally
                {
                    if (File.Exists(scriptFilePath))
                        File.Delete(scriptFilePath);
                }
            };

        It caching_and_non_caching_version_should_handle_stderr_and_stdout_equally =
            () =>
            {
                var scriptFilePath = Path.GetTempFileName() + ".fsx";
                var scriptFileName = Path.GetFileName(scriptFilePath);
                try
                {
                    File.WriteAllText(scriptFilePath, "printf \"stdout\"; eprintf \"stderr\"");
                    // without cache
                    var res1 = RunExplicit(scriptFilePath, EmptyArgs, EmptyArgs, true);
                    res1.Item2.ShouldStartWith("Cache doesn't exist");

                    // with cache
                    var res2 = RunExplicit(scriptFilePath, EmptyArgs, EmptyArgs, true);
                    res2.Item2.ShouldStartWith("Using cache");

                    Microsoft.FSharp.Core.Operators.op_Equality(IgnoreDateTimeOffset(res1.Item1), IgnoreDateTimeOffset(res2.Item1))
                        .ShouldBeTrue();
                }
                finally
                {
                    if (File.Exists(scriptFilePath))
                        File.Delete(scriptFilePath);
                }
            };

        It tracing_functions_should_work =
            () =>
            {
                var scriptFilePath = Path.GetTempFileName() + ".fsx";
                var scriptFileName = Path.GetFileName(scriptFilePath);
                var t = new MyTracer(new StringBuilder());
                try
                {
                    var fakeLib = typeof(TraceListener).Assembly.Location;
                    TraceListener.listeners.Add(t);
                    {
                        File.WriteAllText(scriptFilePath, @"
#r """ + fakeLib.Replace(@"\", @"\\") + @"""
open Fake
traceFAKE ""TEST_FAKE_OUTPUT""");
                        RunExplicit(scriptFilePath, EmptyArgs, false);
                        var result = t.ToString();
                        var idx = result.IndexOf("TEST_FAKE_OUTPUT");
                        idx.ShouldBeGreaterThan(-1);
                        // We should not have it twice
                        result.Substring(idx + "TEST_FAKE_OUTPUT".Length).ShouldNotContain("TEST_FAKE_OUTPUT");
                    }
                    TraceListener.listeners.Remove(t);
                    t = new MyTracer(new StringBuilder());
                    TraceListener.listeners.Add(t);
                    {
                        File.WriteAllText(scriptFilePath, @"
#r """ + fakeLib.Replace(@"\", @"\\") + @"""
open Fake
trace ""TEST_FAKE_OUTPUT""");
                        RunExplicit(scriptFilePath, EmptyArgs, false);
                        var result = t.ToString();
                        var idx = result.IndexOf("TEST_FAKE_OUTPUT");
                        idx.ShouldBeGreaterThan(-1);
                        // We should not have it twice
                        result.Substring(idx + "TEST_FAKE_OUTPUT".Length).ShouldNotContain("TEST_FAKE_OUTPUT");
                    }
                }
                finally
                {
                    if (File.Exists(scriptFilePath))
                        File.Delete(scriptFilePath);
                    TraceListener.listeners.Remove(t);
                }
            };

        It should_be_able_to_use_system_xml =
            () =>
            {
                var scriptFilePath = Path.GetTempFileName() + ".fsx";
                var scriptFileName = Path.GetFileName(scriptFilePath);
                try
                {
                    File.WriteAllText(scriptFilePath, "open System.Xml");
                    RunExplicit(scriptFilePath, EmptyArgs, false)
                        .ShouldEqual("");
                }
                finally
                {
                    if (File.Exists(scriptFilePath))
                        File.Delete(scriptFilePath);
                }
            };

        It should_be_able_to_use_system_web =
            () =>
            {
                var scriptFilePath = Path.GetTempFileName() + ".fsx";
                var scriptFileName = Path.GetFileName(scriptFilePath);
                try
                {
                    File.WriteAllText(scriptFilePath, "open System.Web");
                    RunExplicit(scriptFilePath, EmptyArgs, false)
                        .ShouldEqual("");
                }
                finally
                {
                    if (File.Exists(scriptFilePath))
                        File.Delete(scriptFilePath);
                }
            };

        /// <summary>
        /// See https://github.com/fsharp/FAKE/pull/1080
        /// </summary>
        It should_work_with_debug_flag =
            () =>
            {
                var scriptFilePath = Path.GetTempFileName() + ".fsx";
                var scriptFileName = Path.GetFileName(scriptFilePath);
                try
                {
                    File.WriteAllText(scriptFilePath, "printfn \"test\"");
                    RunExplicit(
                        scriptFilePath,
                        EmptyArgs,
                        new[] {
                            "--debug+",
                            "--optimize-",
                            "--platform", "AnyCpu",
                            "--configuration", "Release",
                            "--csversion", "2007" },
                        false)
                        .Item1.Head.Message.ShouldEqual("test");
                }
                finally
                {
                    if (File.Exists(scriptFilePath))
                        File.Delete(scriptFilePath);
                }
            };

        It should_use_then_invalidate_cache =
            () =>
            {
                var scriptFilePath = Path.GetTempFileName() + ".fsx";
                var scriptFileName = Path.GetFileName(scriptFilePath);
                try
                {
                    File.WriteAllText(scriptFilePath, "printf \"foobar\"");
                    var scriptHash =
                            FSIHelper.getScriptHash(new FSIHelper.Script[] { script(scriptFilePath, "printf \"foobar\"") }, new List<string>());

                    var cacheFilePath = Path.Combine(".", ".fake", scriptFileName + "_" + scriptHash + ".dll");

                    File.Exists(cacheFilePath).ShouldEqual(false);

                    RunExplicit(scriptFilePath, EmptyArgs, EmptyArgs, false)
                       .Item1.Head.Message.ShouldEqual("foobar");

                    File.Exists(cacheFilePath).ShouldEqual(false);

                    RunExplicit(scriptFilePath, EmptyArgs, true)
                        .ShouldStartWith("Cache doesn't exist");

                    File.Exists(cacheFilePath).ShouldEqual(true);

                    var res1 = RunExplicit(scriptFilePath, EmptyArgs, EmptyArgs, true);
                    res1.Item2.ShouldStartWith("Using cache");
                    res1.Item1.Head.Message.ShouldEqual("foobar");

                    File.WriteAllText(scriptFilePath, "printf \"foobarbaz\"");

                    var changedScriptHash = FSIHelper.getScriptHash(new FSIHelper.Script[] { script(scriptFilePath, "printf \"foobarbaz\"") }, new List<string>());
                    var res2 = RunExplicit(scriptFilePath, EmptyArgs, EmptyArgs, true);
                    res2.Item2.ShouldStartWith("Cache is invalid, recompiling");
                    res2.Item1.Head.Message.ShouldEqual("foobarbaz");

                    // This last test is not strictly needed, but it is a good test to check if we can execute
                    // multiple caching tests in the same session (see comment in FSIHelper.fs where we use Mono.Cecil to rename the assembly)
                    var res3 = RunExplicit(scriptFilePath, EmptyArgs, EmptyArgs, true);
                    res3.Item2.ShouldStartWith("Using cache");
                    res3.Item1.Head.Message.ShouldEqual("foobarbaz");

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

                    var res = RunExplicit(mainPath, EmptyArgs, EmptyArgs, false);
                    res.Item2.ShouldEqual("loaded;main");
                    res.Item1.Head.Message.ShouldEqual("loaded;");
                    res.Item1.Tail.Head.Message.ShouldEqual("main");
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
