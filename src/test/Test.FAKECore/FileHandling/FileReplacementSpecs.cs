using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.FileHandling
{
    public class when_replacing_a_nav_file_with_itself
    {
        static readonly FileInfo InputFile = new FileInfo("TestData/Form_5227779.nav");
        static readonly FileInfo OutputFile = new FileInfo("TestData/Form_5227779.rpl");

        Establish context = () => _text = StringHelper.ReadFileAsString(InputFile.FullName);
        Because of = () => StringHelper.ReplaceFile(OutputFile.FullName, _text);

        It should_be_identical = () => FileHelper.FilesAreEqual(InputFile, OutputFile).ShouldBeTrue();
        static string _text;
    }
}