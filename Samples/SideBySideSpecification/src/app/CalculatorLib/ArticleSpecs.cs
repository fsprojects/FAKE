using CalculatorLib.SubFolder;
using NUnit.Framework;

namespace CalculatorLib
{
    [TestFixture]
    public class ArticleSpecs
    {
        [Test]
        public static void TestStupidArticle()
        {
            var article = new Article {Id = 1};

            Assert.AreEqual(1, article.Id);
        }
    }
}