using Fake;
using Machine.Specifications;

namespace Test.Fake.Deploy.PackageMgt
{
    public class when_trying_to_get_the_previous_package
    {
        static string _selectedbackup;
        Because of = () => _selectedbackup = DeploymentHelper.getPreviousPackageVersionFromBackup(TestData.TestDataDir, "jQuery", 1);

        It should_find_the_predecessor_package = () => _selectedbackup.ShouldEqual("1.6");
    }

    public class when_trying_to_get_the_another_predecessor_package
    {
        static string _selectedbackup;
        Because of = () => _selectedbackup = DeploymentHelper.getPreviousPackageVersionFromBackup(TestData.TestDataDir, "jQuery", 2);

        It should_find_the_right_package = () => _selectedbackup.ShouldEqual("1.5");
    }
}