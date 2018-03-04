using System;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore
{
    public class When_modifying_the_Registry
    {
        static readonly RegistryHelper.RegistryBaseKey HkeyCurrentUser = RegistryHelper.RegistryBaseKey.HKEYCurrentUser;
        const string SubKey = "Software\\FAKE";
        const string KeyName = "RegistryUnitTest";

        It should_read_whats_written = () =>
        {
            var expected = DateTime.Now.Ticks.ToString();
            RegistryHelper.createRegistrySubKey(HkeyCurrentUser, SubKey);
            RegistryHelper.setRegistryValue(HkeyCurrentUser, SubKey, KeyName, expected);
            var value = RegistryHelper.getRegistryValue(HkeyCurrentUser, SubKey, KeyName);
            value.ShouldEqual(expected);
        };

        It should_delete_key = () =>
        {
            var expected = DateTime.Now.Ticks.ToString();
            RegistryHelper.createRegistrySubKey(HkeyCurrentUser, SubKey);
            RegistryHelper.setRegistryValue(HkeyCurrentUser, SubKey, KeyName, expected);
            var value = RegistryHelper.getRegistryValue(HkeyCurrentUser, SubKey, KeyName);
            value.ShouldEqual(expected);

            RegistryHelper.deleteRegistryValue(HkeyCurrentUser, SubKey, KeyName);
            var ex = Catch.Exception( () => RegistryHelper.getRegistryValue(HkeyCurrentUser, SubKey, KeyName));
            ex.ShouldBeOfType<NullReferenceException>(); // cries...
        };
    }
}