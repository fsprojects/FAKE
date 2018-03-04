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
	public static class TestData
	{
		public static string FileName(string file)
		{
			return Path.Combine ("TestData", file);
		}

		public static bool Exists(string file)
		{
			return File.Exists (FileName (file));
		}

		public static void Require(string file)
		{
			if (!Exists (file))
			{
				throw new ArgumentException (String.Format("Unable to read test data {0}", 
				                                           Path.GetFullPath(FileName(file))));
			}
		}

		public static string Read(string file)
		{
			Require (file);
			return File.ReadAllText (FileName (file), Encoding.UTF8);
		}

		public static void Copy(string source, string dest)
		{
			Require (source);
			File.Copy (FileName (source), FileName (dest), true);
		}

		public static void Delete(string file)
		{
			File.Delete (FileName(file));
		}
	}

    [Ignore("Doesn't work on mono")]
    public class when_transforming_file_explicitly
    {
        static string TransformedFile;

        Cleanup after = () => TestData.Delete("web.new.config");

        Establish context = () => TransformedFile = TestData.Read("web.transformed.config");

        Because of = () => XDTHelper.TransformFile(TestData.FileName("web.config"), 
		                                           TestData.FileName("web.test.config"), 
		                                           TestData.FileName("web.new.config"));

        It should_equal_the_transformed_file = () => TestData.Read("web.new.config").ShouldEqual(TransformedFile);
    }

    [Ignore("Doesn't work on mono")]
    public class when_transforming_file_with_config_name
    {
        static string TransformedFile;

        Cleanup after = () => TestData.Copy("web.original.config", "web.config");

        Establish context = () => TransformedFile = TestData.Read("web.transformed.config");

        Because of = () => XDTHelper.TransformFileWithConfigName("test", TestData.FileName("web.config"));

        It should_equal_the_transformed_file = () => TestData.Read("web.config").ShouldEqual(TransformedFile);
    }

    [Ignore("Doesn't work on mono")]
    public class when_transforming_files_with_config_name
    {
        static string TransformedFile;

		Cleanup after = () => TestData.Copy("web.original.config", "web.config");

        Establish context = () => TransformedFile = TestData.Read("web.transformed.config");

        Because of = () => XDTHelper.TransformFilesWithConfigName("test", new FileSystem.FileIncludes("TestData", 
            new FSharpList<string>("web.config", FSharpList<string>.Empty), FSharpList<string>.Empty));

        It should_equal_the_transformed_file = () => TestData.Read("web.config").ShouldEqual(TransformedFile);
    }
}
