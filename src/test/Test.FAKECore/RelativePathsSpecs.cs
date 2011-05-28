using System;
using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class when_working_with_relative_paths
    {
        It should_combine_two_paths_with_additional_backslash =
            () => EnvironmentHelper.combinePaths(@"\\Hades\Dir1", @"\Test")
                  .ShouldEqual(@"\\Hades\Dir1\Test");

        It should_combine_two_paths =
            () => EnvironmentHelper.combinePaths(@"\\Hades\Dir1", @"Test\blub")
                  .ShouldEqual(@"\\Hades\Dir1\Test\blub");

        It should_combine_a_directory_with_a_file =
            () => EnvironmentHelper.combinePaths(@"C:\Dir1\", @"\Test\myfile.txt")
                  .ShouldEqual(@"C:\Dir1\Test\myfile.txt");

        It should_get_the_path_of_a_different_machine =
            () => GetRelativePath(@"\\Hades\Dir1")
                      .ShouldEqual(@"\\Hades\Dir1");

        It should_get_the_path_of_a_file_in_a_subdirectory =
            () => GetRelativePath(Environment.CurrentDirectory + @"\Test1\Test2\text.txt")
                      .ShouldEqual(@".\Test1\Test2\text.txt");

        It should_get_the_path_of_a_subdirectory =
            () => GetRelativePath(Environment.CurrentDirectory + @"\Test1\Test2")
                      .ShouldEqual(@".\Test1\Test2");

        It should_get_the_path_of_a_test_subdirectory =
            () => GetRelativePath(GetParent(GetParent(GetRelativePath(Environment.CurrentDirectory))) + "\\Test1")
                      .ShouldEqual(@"..\..\Test1");

        It should_get_the_path_of_the_current_directory =
            () => GetRelativePath(Environment.CurrentDirectory)
                      .ShouldEqual(".");

        It should_get_the_path_of_the_parent_directory =
            () => GetParent(GetRelativePath(Environment.CurrentDirectory))
                      .ShouldEqual(@"..");

        It should_get_the_path_of_the_parent_of_the_parent_directory =
            () => GetParent(GetParent(GetRelativePath(Environment.CurrentDirectory)))
                      .ShouldEqual(@"..\..");

        static string GetRelativePath(string path)
        {
            return StringHelper.toRelativePath(new DirectoryInfo(path).FullName);
        }

        static string GetParent(string path)
        {
            return StringHelper.toRelativePath(new DirectoryInfo(path).Parent.FullName);
        }
    }
}