using System;
using System.IO;
using Fake;
using Machine.Specifications;
using Test.FAKECore.FileHandling;

namespace Test.FAKECore.FileHandling
{
	public class CopyRecursiveSpecs
	{
		private static readonly string DestinationDir = Path.Combine(TestData.TestDir, "destination");
		private static readonly string Dir = Path.Combine(TestData.TestDir, "Dir6");

		public class when_skipping_existing_files : BaseFunctions
		{
			private const string AltText = "hello mum";
			private static readonly string TargetDir = Path.Combine(DestinationDir, "Sub1");
			private static readonly string TargetFile = Path.Combine(TargetDir, "file1.nav");

			static void CreateTestFileStructureWithDest()
			{
				CreateTestFileStructure();
				CleanDir(DestinationDir);
				CleanDir(TargetDir);
				CreateTestFile(Path.Combine(TargetFile), AltText);
			}

			Establish context = CreateTestFileStructureWithDest;

			Because of = () => FileHelper.CopyRecursive2(FileHelper.CopyRecursiveMethod.Skip, Dir, DestinationDir);

			It does_not_overwrite_existing_file = () => File.ReadAllText(TargetFile).ShouldEqual(AltText);
		}

		public class when_excluding_files_based_on_pattern : BaseFunctions
		{
			Establish context = CreateTestFileStructure;

			Because of = () => FileHelper.CopyRecursive2(FileHelper.CopyRecursiveMethod.NewExcludePattern("**/*.nav"), Dir, DestinationDir);

			It does_not_include_files_in_pattern = () => File.Exists(Path.Combine(DestinationDir, "Sub1/file1.nav")).ShouldBeFalse();
			It includes_files_not_in_pattern = () => File.Exists(Path.Combine(DestinationDir, "Sub1/file2.nat")).ShouldBeTrue();
		}

		public class when_including_files_based_on_pattern : BaseFunctions
		{
			Establish context = CreateTestFileStructure;

			Because of = () => FileHelper.CopyRecursive2(FileHelper.CopyRecursiveMethod.NewIncludePattern("**/*.nav"), Dir, DestinationDir);

			It excludes_files_in_pattern = () => File.Exists(Path.Combine(DestinationDir, "Sub1/file2.nat")).ShouldBeFalse();
			It does_not_exclude_files_not_in_pattern = () => File.Exists(Path.Combine(DestinationDir, "Sub1/file1.nav")).ShouldBeTrue();
		}
	}
}
