using Machine.Specifications;
using static Fake.Git.FileStatus;

namespace Test.Git
{
    public class when_getting_file_status
    {
        It should_be_able_to_get_renamed_files =
            () => FileStatus
                .Parse.Invoke("R100")
                .ShouldEqual(FileStatus.NewRenamed(100));

        It should_be_able_to_get_probably_renamed_files =
            () => FileStatus
                .Parse.Invoke("R75")
                .ShouldEqual(FileStatus.NewRenamed(75));
    }
}