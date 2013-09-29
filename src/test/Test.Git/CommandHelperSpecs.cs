using System.IO;
using Fake;
using Fake.Git;
using Machine.Specifications;

namespace Test.Git
{
    public class when_running_git_version
    {
        It should_find_a_git_version =
            () => CommandHelper.runSimpleGitCommand(".", "version").ShouldStartWith("git version ");
    }


    public class when_creating_a_git_repository
    {
        public static readonly string RepositoryPath = Path.Combine(Path.GetTempPath(), "TestRepository");
        Establish ctx = () => FileHelper.CleanDir(RepositoryPath);

        It should_find_a_git_version =
            () => CommandHelper.runSimpleGitCommand(RepositoryPath, "init").ShouldStartWith("Initialized empty Git repository in ");
    }

    public class when_staging_a_file : when_creating_a_git_repository
    {
        static readonly string FileName = Path.Combine(RepositoryPath, "Testfile.txt");

        Establish ctx = () => File.WriteAllText(FileName, "test");

        It should_find_a_git_version =
            () => CommandHelper.runSimpleGitCommand(".", "add . --all").ShouldBeEmpty();

        Cleanup after = () => File.Delete(FileName);
    }
}