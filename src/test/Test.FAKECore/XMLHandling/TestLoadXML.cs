using Fake;
using NUnit.Framework;
using System.Xml;

namespace Test.FAKECore.XMLHandling
{
    [TestFixture]
    public class TestLoadXml 
    {
        const string TargetText =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<painting><img src=\"madonna.jpg\" alt=\"Foligno Madonna, by Raphael\" />" +
                "<caption>This is Raphael's \"Foligno\" Madonna, painted in <date year=\"1515\" /> - <date year=\"1512\" />.</caption>" +
                "</painting>";

        [Test]
        public void CanLoadXml()
        {
            // Act
            XmlDocument doc = XMLHelper.XMLDoc(TargetText);           

            // Assert
            Assert.AreEqual(TargetText, doc.OuterXml);
        }
    }
}