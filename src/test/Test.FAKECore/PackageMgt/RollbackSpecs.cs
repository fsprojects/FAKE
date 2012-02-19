using System.Collections.Generic;

namespace Test.FAKECore.PackageMgt
{
    using Fake;
    using Machine.Specifications;

    public class when_trying_to_rollback_one
    {
        static string _selectedbackup;
        Because of = () => _selectedbackup = DeploymentHelper.getPreviousPackageFromBackup(TestData.TestDataDir, "JQuery");

        It should_select_for_rollback =
                () => _selectedbackup.ShouldEqual(@"TestData\deployments/JQuery/backups/jQuery.1.6.nupkg");
    }
}