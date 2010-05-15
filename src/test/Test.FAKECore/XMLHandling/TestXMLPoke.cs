using System.IO;
using Fake;
using NUnit.Framework;
using System.Xml;

namespace Test.FAKECore.FileHandling
{
    [TestFixture]
    public class TestXMLPoke : BaseTest
    {
        string originalText =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                "<painting> <img src=\"madonna.jpg\" alt=\"Foligno Madonna, by Raphael\" />" +
                "<caption>This is Raphael's \"Foligno\" Madonna, painted in <date year=\"1511\" /> - <date year=\"1512\" />.</caption>" +
                "</painting>";

        string targetText =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                "<painting>  <img src=\"madonna.jpg\" alt=\"Foligno Madonna, by Raphael\" />  " +
                "<caption>This is Raphael's \"Foligno\" Madonna, painted in <date year=\"1515\" /> - <date year=\"1512\" />.</caption>" +
                "</painting>";

        string xPath = "painting/caption/date/@year";

        string fileName = Path.Combine(TestData.TestDir, "test.xml");

        [SetUp]
        public void SaveFiles()
        {
            StringHelper.WriteStringToFile(false, fileName, originalText);
        }

        [Test]
        public void CanXMLPoke()
        {
            // Act
            XMLHelper.XmlPoke(fileName, xPath, "1515");

            // Assert
            var result = StringHelper.ReadFileAsString(fileName).Replace("\r\n", "");
            Assert.AreEqual(targetText.Replace("\r\n", ""), result);
        }

        [Test]
        public void CanModifyXPath()
        {
            // Arrange
            var doc = new XmlDocument();
            doc.LoadXml(originalText);

            // Act
            XMLHelper.XPathReplace(xPath, "1515", doc).Save(fileName);

            // Assert
            var result = StringHelper.ReadFileAsString(fileName).Replace("\r\n", "");
            Assert.AreEqual(targetText.Replace("\r\n", ""), result);
        }

        [TearDown]
        public void DeleteFiles()
        {
            FileHelper.DeleteFile(fileName);
        }

    }
}