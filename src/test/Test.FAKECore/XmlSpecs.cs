using System.IO;
using System.Xml;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.XMLHandling
{
    public class when_loading_xml
    {
        const string TargetText =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<painting><img src=\"madonna.jpg\" alt=\"Foligno Madonna, by Raphael\" />" +
            "<caption>This is Raphael's \"Foligno\" Madonna, painted in <date year=\"1515\" /> - <date year=\"1512\" />.</caption>" +
            "</painting>";

        static readonly FileInfo File1 = new FileInfo(@"TestData\AllObjects.txt");
        static readonly FileInfo File2 = new FileInfo(@"TestData\AllObjects_2.txt");

        static XmlDocument _doc;

        Because of = () => _doc = XMLHelper.XMLDoc(TargetText);

        It should_equal_the_target_text = () => _doc.OuterXml.ShouldEqual(TargetText);
    }

    public class when_modifying_xml
    {
        protected const string OriginalText =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<painting>  <img src=\"madonna.jpg\" alt=\"Foligno Madonna, by Raphael\" />" +
            "  <caption>This is Raphael's \"Foligno\" Madonna, painted in <date year=\"1511\" /> - <date year=\"1512\" />.</caption>" +
            "</painting>";

        protected const string XPath = "painting/caption/date/@year";

        protected static readonly string FileName = Path.Combine(TestData.TestDir, "test.xml");

        protected static XmlDocument Doc;
        protected static string TargetText = OriginalText.Replace("1511", "1515");

        Cleanup after = () => FileHelper.DeleteFile(FileName);

        Establish context = () =>
        {
            StringHelper.WriteStringToFile(false, FileName, OriginalText);
            Doc = new XmlDocument();
            Doc.LoadXml(OriginalText);
        };

        protected static string ResultXml
        {
            get { return StringHelper.ReadFileAsString(FileName).Replace("\r\n", ""); }
        }
    }

    public class when_poking_xml : when_modifying_xml
    {
        Because of = () => XMLHelper.XmlPoke(FileName, XPath, "1515");

        It should_equal_the_target_text = () => ResultXml.ShouldEqual(TargetText.Replace("\r\n", ""));
    }

    public class when_modifying_xml_with_xpath : when_modifying_xml
    {
        Because of = () => XMLHelper.XPathReplace(XPath, "1515", Doc).Save(FileName);

        It should_equal_the_target_text = () => ResultXml.ShouldEqual(TargetText.Replace("\r\n", ""));
    }
}