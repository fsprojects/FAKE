using System.IO;
using Fake.MSBuild;
using Machine.Specifications;

namespace Test.FAKECore.SideBySideSpecification
{
    public class when_comparing_files
    {
        protected static string ProjectXml;

        protected static void CompareFiles(string result, string resultFileName)
        {
            var expected = File.ReadAllText(resultFileName).Replace("\r\n", "\n");
            var actual = File.ReadAllText(result);
            actual.Replace("\r\n", "\n").ShouldEqual(expected);
        }
    }

    public class when_splicing_NUnit_reference : when_comparing_files
    {
        Because of = () => ProjectXml = SpecsRemovement.RemoveAllNUnitReferences("SideBySideSpecification/Project1.txt");

        It should_be_spliced = () => CompareFiles(ProjectXml, "SideBySideSpecification/Project1_WithoutNUnit.txt");
    }


    public class when_splicing_test_file : when_comparing_files
    {
        Because of = () => ProjectXml = SpecsRemovement.RemoveAllSpecAndTestDataFiles("SideBySideSpecification/Project1.txt");

        It should_be_spliced = () => CompareFiles(ProjectXml, "SideBySideSpecification/Project1_WithoutTests.txt");
    }

    public class when_splicing_test_file_and_test_data_files : when_comparing_files
    {
        Because of = () => ProjectXml = SpecsRemovement.RemoveAllSpecAndTestDataFiles("SideBySideSpecification/Project2.txt");

        It should_be_spliced = () => CompareFiles(ProjectXml, "SideBySideSpecification/Project2_WithoutTests.txt");
    }
}