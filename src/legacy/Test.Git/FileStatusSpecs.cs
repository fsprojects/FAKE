using Machine.Specifications;
using static Fake.Git.FileStatus;

namespace Test.Git
{
    public class when_getting_file_status
    {
        It should_be_able_to_get_modified_files =
            () => FileStatus
                .Parse.Invoke("M")
                .ShouldEqual(FileStatus.Modified);

        It should_be_able_to_get_rewritten_files =
            () => FileStatus
                .Parse.Invoke("M42")
                .ShouldEqual(FileStatus.Modified);

        It should_be_able_to_get_renamed_files =
            () => FileStatus
                .Parse.Invoke("R100")
                .ShouldEqual(FileStatus.Renamed);

        It should_be_able_to_get_probably_renamed_files =
            () => FileStatus
                .Parse.Invoke("R75")
                .ShouldEqual(FileStatus.Renamed);
    }
}