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

    public class when_poking_xml_innertext
    {
        const string OriginalText =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<painting>" +
            "  <img src=\"madonna.jpg\" alt=\"Foligno Madonna, by Raphael\" />" +
            "  <caption>This is Raphael's \"Foligno\" Madonna, painted in <date year=\"1511\" /> - <date year=\"1512\" />.</caption>" +
            "  <note name=\"Type\">Oil on wood, transferred to canvas</note>" +
            "  <note name=\"Dimensions\">320 cm * 194 cm (130 in * 76 in)</note>" +
            "  <note name=\"Location\">Pinacoteca Vaticana, Vatican City</note>" +
            "</painting>";

        const string XPath = "painting/note[@name='Location']";

        static readonly string FileName = Path.Combine(TestData.TestDir, "test.xml");

        static XmlDocument _doc;
        static readonly string TargetText = OriginalText.Replace("Pinacoteca Vaticana, Vatican City", "Vatican City");


        Cleanup after = () => FileHelper.DeleteFile(FileName);

        Establish context = () =>
            {
                StringHelper.WriteStringToFile(false, FileName, OriginalText);
                _doc = new XmlDocument();
                _doc.LoadXml(OriginalText);
            };

        Because of = () => XMLHelper.XmlPokeInnerText(FileName, XPath, "Vatican City");

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

    public class when_poking_xml_and_ns
    {
        const string OriginalText =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<asmv1:assembly manifestVersion='1.0' xmlns='urn:schemas-microsoft-com:asm.v1' xmlns:asmv1='urn:schemas-microsoft-com:asm.v1' xmlns:asmv2='urn:schemas-microsoft-com:asm.v2' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>" +
            "  <assemblyIdentity version='0.1.0.0' name='MyApplication' />" +
            "</asmv1:assembly>";

        const string XPath = "//asmv1:assembly/asmv1:assemblyIdentity/@version";

        static readonly string FileName = Path.Combine(TestData.TestDir, "test.xml");

        static XmlDocument _doc;
        static readonly string TargetText = OriginalText.Replace("0.1.0.0", "1.1.0.1");
        static List<Tuple<string, string>> _nsdecl;


        Cleanup after = () => FileHelper.DeleteFile(FileName);

        Establish context = () =>
        {
            StringHelper.WriteStringToFile(false, FileName, OriginalText);
            _doc = new XmlDocument();
            _doc.LoadXml(OriginalText);
            _nsdecl = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("", "urn:schemas-microsoft-com:asm.v1"), 
                new Tuple<string, string>("asmv1", "urn:schemas-microsoft-com:asm.v1"), 
                new Tuple<string, string>("asmv2", "urn:schemas-microsoft-com:asm.v2"), 
                new Tuple<string, string>("xsi", "http://www.w3.org/2001/XMLSchema-instance")
            };
        };

        Because of = () => XMLHelper.XmlPokeNS(FileName, _nsdecl, XPath, "1.1.0.1");

        It should_equal_the_target_text =
            () => StringHelper.ReadFileAsString(FileName).Replace("\r", "").Replace("\n", "").Replace("'", "\"")
                      .ShouldEqual(TargetText.Replace("\r", "").Replace("\n", "").Replace("'", "\""));
    }

    public class when_poking_xml_innertext_and_ns
    {
        const string OriginalText =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<asmv1:assembly manifestVersion='1.0' xmlns='urn:schemas-microsoft-com:asm.v1' xmlns:asmv1='urn:schemas-microsoft-com:asm.v1' xmlns:asmv2='urn:schemas-microsoft-com:asm.v2' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>" +
            "  <assemblyIdentity version='0.1.0.0' name='MyApplication' />" +
            "  <assemblyDescription></assemblyDescription>" + 
            "</asmv1:assembly>";

        const string XPath = "//asmv1:assembly/asmv1:assemblyDescription";

        static readonly string FileName = Path.Combine(TestData.TestDir, "test.xml");

        static XmlDocument _doc;
        static readonly string TargetText = OriginalText.Replace("<assemblyDescription></assemblyDescription>", "<assemblyDescription>A really great assembly. Really.</assemblyDescription>");
        static List<Tuple<string, string>> _nsdecl;


        Cleanup after = () => FileHelper.DeleteFile(FileName);

        Establish context = () =>
            {
                StringHelper.WriteStringToFile(false, FileName, OriginalText);
                _doc = new XmlDocument();
                _doc.LoadXml(OriginalText);
                _nsdecl = new List<Tuple<string, string>>
                {
                    new Tuple<string, string>("", "urn:schemas-microsoft-com:asm.v1"), 
                    new Tuple<string, string>("asmv1", "urn:schemas-microsoft-com:asm.v1"), 
                    new Tuple<string, string>("asmv2", "urn:schemas-microsoft-com:asm.v2"), 
                    new Tuple<string, string>("xsi", "http://www.w3.org/2001/XMLSchema-instance")
                };
            };

        Because of = () => XMLHelper.XmlPokeInnerTextNS(FileName, _nsdecl, XPath, "A really great assembly. Really.");

        It should_equal_the_target_text =
            () => StringHelper.ReadFileAsString(FileName).Replace("\r", "").Replace("\n", "").Replace("'", "\"")
                .ShouldEqual(TargetText.Replace("\r", "").Replace("\n", "").Replace("'", "\""));
    }

    public class when_modifying_xml_with_xpath_and_ns
    {
        const string OriginalText =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<asmv1:assembly manifestVersion='1.0' xmlns='urn:schemas-microsoft-com:asm.v1' xmlns:asmv1='urn:schemas-microsoft-com:asm.v1' xmlns:asmv2='urn:schemas-microsoft-com:asm.v2' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>" +
            "  <assemblyIdentity version='0.1.0.0' name='MyApplication' />" +
            "</asmv1:assembly>";

        const string XPath = "//asmv1:assembly/asmv1:assemblyIdentity/@version";

        static XmlDocument _doc;
        static XmlDocument _resultDoc;
        static List<Tuple<string, string>> _nsdecl;
        static string _targetText;

        Establish context = () =>
        {
            _doc = new XmlDocument();
            _doc.LoadXml(OriginalText);
            _targetText = _doc.OuterXml.Replace("0.1.0.0", "1.1.0.1");
            _nsdecl = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("", "urn:schemas-microsoft-com:asm.v1"), 
                new Tuple<string, string>("asmv1", "urn:schemas-microsoft-com:asm.v1"), 
                new Tuple<string, string>("asmv2", "urn:schemas-microsoft-com:asm.v2"), 
                new Tuple<string, string>("xsi", "http://www.w3.org/2001/XMLSchema-instance")
            };
        };

        Because of = () => _resultDoc = XMLHelper.XPathReplaceNS(XPath, "1.1.0.1", _nsdecl, _doc);

        It should_equal_the_target_text =
            () => _resultDoc.OuterXml.ShouldEqual(_targetText);

    }

    public class when_modifying_xml_with_xsl
    {
        const string OriginalText =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<painting>  <img src=\"madonna.jpg\" alt=\"Foligno Madonna, by Raphael\" />" +
            "  <caption>This is Raphael's \"Foligno\" Madonna, painted in <date year=\"1511\" /> - <date year=\"1512\" />.</caption>" +
            "</painting>";

        const string XslStyleSheet =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\">" +
            "<xsl:output indent=\"yes\" omit-xml-declaration=\"no\" method=\"xml\" encoding=\"utf-8\" />" +
            "  <xsl:template match=\"date[@year='1511']\">" +
            "    <date year=\"1515\" />" +
            "  </xsl:template>" +
            "  <xsl:template match=\"@*|node()\">" +
            "    <xsl:copy>" +
            "      <xsl:apply-templates select=\"@*|node()\"/>" +
            "    </xsl:copy>" +
            "  </xsl:template>" +
            "</xsl:stylesheet>";

        static XmlDocument _doc;
        static XmlDocument _resultDoc;
        static string _targetText;

        Establish context = () =>
        {
            _doc = new XmlDocument();
            _doc.LoadXml(OriginalText);
            _targetText = _doc.OuterXml.Replace("1511", "1515");
        };

        Because of = () => _resultDoc = XMLHelper.XslTransform(XMLHelper.XslTransformer(XslStyleSheet), _doc);

        It should_equal_the_target_text =
            () => _resultDoc.OuterXml.Replace("></img>", " />").Replace("></date>", " />").ShouldEqual(_targetText);
    }
}