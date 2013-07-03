using System;
using System.Collections.Generic;
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


    public class when_poking_xml
    {
        const string OriginalText =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<painting>  <img src=\"madonna.jpg\" alt=\"Foligno Madonna, by Raphael\" />" +
            "  <caption>This is Raphael's \"Foligno\" Madonna, painted in <date year=\"1511\" /> - <date year=\"1512\" />.</caption>" +
            "</painting>";

        const string XPath = "painting/caption/date/@year";

        static readonly string FileName = Path.Combine(TestData.TestDir, "test.xml");

        static XmlDocument _doc;
        static readonly string TargetText = OriginalText.Replace("1511", "1515");


        Cleanup after = () => FileHelper.DeleteFile(FileName);

        Establish context = () =>
        {
            StringHelper.WriteStringToFile(false, FileName, OriginalText);
            _doc = new XmlDocument();
            _doc.LoadXml(OriginalText);
        };

        Because of = () => XMLHelper.XmlPoke(FileName, XPath, "1515");

        It should_equal_the_target_text =
            () => StringHelper.ReadFileAsString(FileName).Replace("\r", "").Replace("\n", "")
                      .ShouldEqual(TargetText.Replace("\r", "").Replace("\n", ""));
    }

    public class when_modifying_xml_with_xpath
    {
        const string OriginalText =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<painting>  <img src=\"madonna.jpg\" alt=\"Foligno Madonna, by Raphael\" />" +
            "  <caption>This is Raphael's \"Foligno\" Madonna, painted in <date year=\"1511\" /> - <date year=\"1512\" />.</caption>" +
            "</painting>";

        const string XPath = "painting/caption/date/@year";

        static XmlDocument _doc;
        static XmlDocument _resultDoc;
        static string _targetText;

        Establish context = () =>
        {
            _doc = new XmlDocument();
            _doc.LoadXml(OriginalText);
            _targetText = _doc.OuterXml.Replace("1511", "1515");
        };

        Because of = () => _resultDoc = XMLHelper.XPathReplace(XPath, "1515", _doc);

        It should_equal_the_target_text =
            () => _resultDoc.OuterXml.ShouldEqual(_targetText);
    }
}