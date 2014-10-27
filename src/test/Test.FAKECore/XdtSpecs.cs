using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Collections;

namespace Test.FAKECore.XDTHandling
{
    public class when_transforming_file_explicitly
    {
        static string TransformedFile;

        Cleanup after = () => File.Delete(@"TestData\web.new.config");

        Establish context = () => TransformedFile = StringHelper.ReadFileAsString(@"TestData\web.transformed.config");

        Because of = () => XDTHelper.TransformFile(@"TestData\web.config", @"TestData\web.test.config", @"TestData\web.new.config");

        It should_equal_the_transformed_file = () => StringHelper.ReadFileAsString(@"TestData\web.new.config").ShouldEqual(TransformedFile);
    }

    public class when_transforming_file_with_config_name
    {
        static string TransformedFile;

        Cleanup after = () => File.Copy(@"TestData\web.original.config", @"TestData\web.config", true);

        Establish context = () => TransformedFile = StringHelper.ReadFileAsString(@"TestData\web.transformed.config");

        Because of = () => XDTHelper.TransformFileWithConfigName("test", @"TestData\web.config");

        It should_equal_the_transformed_file = () => StringHelper.ReadFileAsString(@"TestData\web.config").ShouldEqual(TransformedFile);
    }

    public class when_transforming_files_with_config_name
    {
        static string TransformedFile;

        Cleanup after = () => File.Copy(@"TestData\web.original.config", @"TestData\web.config", true);

        Establish context = () => TransformedFile = StringHelper.ReadFileAsString(@"TestData\web.transformed.config");

        Because of = () => XDTHelper.TransformFilesWithConfigName("test", new FileSystem.FileIncludes("TestData", 
            new FSharpList<string>("web.config", FSharpList<string>.Empty), FSharpList<string>.Empty));

        It should_equal_the_transformed_file = () => StringHelper.ReadFileAsString(@"TestData\web.config").ShouldEqual(TransformedFile);
    }
}
