using System.IO;
using Fake;
using NUnit.Framework;
using System.Xml;

namespace Test.FAKECore.FileHandling
{
    [TestFixture]
    public class TestLoadXML : BaseTest
    {
        string targetText =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<painting><img src=\"madonna.jpg\" alt=\"Foligno Madonna, by Raphael\" />" +
                "<caption>This is Raphael's \"Foligno\" Madonna, painted in <date year=\"1515\" /> - <date year=\"1512\" />.</caption>" +
                "</painting>";

        string fileName = Path.Combine(TestData.TestDir, "test.xml");

        [Test]
        public void CanLoadXML()
        {
            // Act
            XmlDocument doc = XMLHelper.XMLDoc(targetText);           

            // Assert
            Assert.AreEqual(targetText, doc.OuterXml);
        }
    }
}