using System;
using Fake;
using Fake.IO;
using Machine.Specifications;
using Test.FAKECore.FileHandling;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;

namespace Test.FAKECore.FileHandling
{
	public class CopyRecursiveSpecs
	{
		private static readonly string DestinationDir = IOPath.Combine(TestData.TestDir, "destination");
		private static readonly string Dir = IOPath.Combine(TestData.TestDir, "Dir6");

		public class when_skipping_existing_files : BaseFunctions
		{
			private const string AltText = "hello mum";
			private static readonly string TargetDir = IOPath.Combine(DestinationDir, "Sub1");
			private static readonly string TargetFile = IOPath.Combine(TargetDir, "file1.nav");

			static void CreateTestFileStructureWithDest()
			{
				CreateTestFileStructure();
				CleanDir(DestinationDir);
				CleanDir(TargetDir);
				CreateTestFile(IOPath.Combine(TargetFile), AltText);
			}

			Establish context = CreateTestFileStructureWithDest;

			Because of = () => Shell.CopyRecursive2(Shell.CopyRecursiveMethod.Skip, Dir, DestinationDir);

			It does_not_overwrite_existing_file = () => IOFile.ReadAllText(TargetFile).ShouldEqual(AltText);
		}

		public class when_excluding_files_based_on_pattern : BaseFunctions
		{
			Establish context = CreateTestFileStructure;

			Because of = () => Shell.CopyRecursive2(Shell.CopyRecursiveMethod.NewExcludePattern("**/*.nav"), Dir, DestinationDir);

			It does_not_include_files_in_pattern = () => IOFile.Exists(IOPath.Combine(DestinationDir, "Sub1/file1.nav")).ShouldBeFalse();
			It includes_files_not_in_pattern = () => IOFile.Exists(IOPath.Combine(DestinationDir, "Sub1/file2.nat")).ShouldBeTrue();
		}

		public class when_including_files_based_on_pattern : BaseFunctions
		{
			Establish context = CreateTestFileStructure;

			Because of = () => Shell.CopyRecursive2(Shell.CopyRecursiveMethod.NewIncludePattern("**/*.nav"), Dir, DestinationDir);

			It excludes_files_in_pattern = () => IOFile.Exists(IOPath.Combine(DestinationDir, "Sub1/file2.nat")).ShouldBeFalse();
			It does_not_exclude_files_not_in_pattern = () => IOFile.Exists(IOPath.Combine(DestinationDir, "Sub1/file1.nav")).ShouldBeTrue();
		}
	}
}
