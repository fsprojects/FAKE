using Machine.Specifications;

namespace Fake_WebSite.Tests
{
    public class when_using_mspec
    {
        static int _value;
        Because of = () => _value = 1;
        It should_work = () => _value.ShouldEqual(1);
    }
}