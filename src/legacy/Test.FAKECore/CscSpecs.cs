using System;
using System.IO;
using System.Linq;
using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;

namespace Test.FAKECore
{
    public class compile_csfiles_to_dll
    {
        static string tempDir, outFile, csFile1, csFile2;
        static FSharpFunc<CscHelper.CscParams, CscHelper.CscParams> cscParams;

        Establish context = () =>
        {
            tempDir = Path.GetTempPath();
            csFile1 = Path.Combine(tempDir, "test1.cs");
            csFile2 = Path.Combine(tempDir, "test2.cs");

            File.WriteAllText(csFile1, @"
using System;

namespace Test {
    public class Class1 {
        public string Hello(string what) {
            return String.Format(""Hello {0}"", what);
        }
    }
}");

            File.WriteAllText(csFile2, @"
using System;

namespace Test {
    public class Class2 : Class1 {
        public void HelloWorld() {
            Console.WriteLine(this.Hello(""World""));
        }
    }
}");

            outFile = Path.Combine(tempDir, "test.dll");
            try { File.Delete(outFile); } catch (FileNotFoundException) {}

            cscParams = FSharpFuncUtil.ToFSharpFunc<CscHelper.CscParams, CscHelper.CscParams>(
                p => new CscHelper.CscParams(
                    output: outFile,
                    toolPath: p.ToolPath,
                    target: CscHelper.CscTarget.Library,
                    platform: p.Platform,
                    references: p.References,
                    debug: p.Debug,
                    otherParams: p.OtherParams
                )
            );
        };

        Because of = () => CscHelper.Csc(cscParams, ListModule.OfSeq(new [] { csFile1, csFile2 }));

        It should_compile_to_dll = () => File.Exists(outFile).ShouldBeTrue();
    }
}

