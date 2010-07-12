using NUnit.Framework;

namespace Dependency
{
    [TestFixture]
    public class TestClassSpecs
    {
        [Test]
        public void Test()
        {            
            Assert.IsTrue((new TestClass()).IsStupid);
        }
    }
}