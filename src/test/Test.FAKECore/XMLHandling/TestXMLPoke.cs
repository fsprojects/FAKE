using System.IO;
using Fake;
using NUnit.Framework;
using System.Xml;

namespace Test.FAKECore.XMLHandling
{
    [TestFixture]
    public class TestXmlPoke
    {
        const string OriginalText =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                "<painting> <img src=\"madonna.jpg\" alt=\"Foligno Madonna, by Raphael\" />" +
                "<caption>This is Raphael's \"Foligno\" Madonna, painted in <date year=\"1511\" /> - <date year=\"1512\" />.</caption>" +
                "</painting>";

        const string TargetText =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                "<painting>  <img src=\"madonna.jpg\" alt=\"Foligno Madonna, by Raphael\" />  " +
                "<caption>This is Raphael's \"Foligno\" Madonna, painted in <date year=\"1515\" /> - <date year=\"1512\" />.</caption>" +
                "</painting>";

        const string XPath = "painting/caption/date/@year";

        readonly string _fileName = Path.Combine(TestData.TestDir, "test.xml");

        [SetUp]
        public void SaveFiles()
        {
            StringHelper.WriteStringToFile(false, _fileName, OriginalText);
        }

        [Test]
        public void CanXmlPoke()
        {
            // Act
            XMLHelper.XmlPoke(_fileName, XPath, "1515");

            // Assert
            var result = StringHelper.ReadFileAsString(_fileName).Replace("\r\n", "");
            Assert.AreEqual(TargetText.Replace("\r\n", ""), result);
        }

        [Test]
        public void CanModifyXPath()
        {
            // Arrange
            var doc = new XmlDocument();
            doc.LoadXml(OriginalText);

            // Act
            XMLHelper.XPathReplace(XPath, "1515", doc).Save(_fileName);

            // Assert
            var result = StringHelper.ReadFileAsString(_fileName).Replace("\r\n", "");
            Assert.AreEqual(TargetText.Replace("\r\n", ""), result);
        }

        [TearDown]
        public void DeleteFiles()
        {
            FileHelper.DeleteFile(_fileName);
        }

    }
}