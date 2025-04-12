using System.Xml;
using System.Xml.Xsl;

namespace Cryptography.Utilities;

public class TransformXml
{

    //var xslFilePath = AppContext.BaseDirectory + "wwwroot\\Ecddi\\ecddi.xsl";
    //var cssFilePath = AppContext.BaseDirectory + "wwwroot\\Ecddi\\xslt2.txt";

    public string TransformXmlWithXsl(string xmlData, string xslFilePath, string cssContentFilePath)
    {
        if (string.IsNullOrEmpty(xmlData))
        {
            return null;
        }

        var xslTransform = new XslCompiledTransform();

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Parse,
            XmlResolver = new XmlUrlResolver(),
        };

        using var xslReader = XmlReader.Create(xslFilePath, settings);
        var xsltSettings = new XsltSettings(enableDocumentFunction: true, enableScript: false);
        var xmlResolver = new XmlUrlResolver();
        xmlResolver.Credentials = System.Net.CredentialCache.DefaultCredentials;

        xslTransform.Load(xslFilePath, xsltSettings, xmlResolver);
        using var xmlReader = XmlReader.Create(new StringReader(xmlData));
        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, xslTransform.OutputSettings);
        var cssContent = File.ReadAllText(cssContentFilePath);
        var xsltArguments = new XsltArgumentList();
        xsltArguments.AddParam("cssContent", "", cssContent);

        xslTransform.Transform(xmlReader, xsltArguments, xmlWriter);

        return stringWriter.ToString();
    }
}
