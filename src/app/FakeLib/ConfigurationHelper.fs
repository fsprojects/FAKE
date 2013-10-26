[<AutoOpen>]
/// Contains functions which allow to read and write config files.
module Fake.ConfigurationHelper
    
open System.Xml
open System.Xml.Linq

/// Reads a config file into an XmlDocument.
/// ## Parameters
///  - `fileName` - The file name of the config file.
let readConfig (fileName:string) = 
    let xmlDocument = new XmlDocument()
    xmlDocument.Load fileName
    xmlDocument

/// Writes an XmlDocument to a config file.
/// ## Parameters
///  - `fileName` - The file name of the config file.
///  - `config` - The XmlDocument representing the config.
let writeConfig (fileName:string) (config:XmlDocument) = config.Save fileName 

/// Reads a config file from the given file name, replaces an attribute using the given xPath and writes it back.
/// ## Parameters
///  - `xpath` - An XPath term which can be used to replace the attribute.
///  - `attribute` - The attribute name for which the value should be replaced.
///  - `value` - The new attribute value.
///  - `config` - The XElement representing the config.
let updateConfig xpath attribute value (config:XmlDocument) =
    let node = config.SelectSingleNode xpath :?> XmlElement
    if node = null then
        failwithf "Could not find node addressed by %s" xpath
    else
        node.SetAttribute(XName.Get(attribute).ToString(), value) 
    config

/// Reads a config file from the given file name, replaces an attribute using the given xPath and writes it back.
/// ## Parameters
///  - `fileName` - The file name of the config file.
///  - `xpath` - An XPath term which can be used to replace the attribute.
///  - `attribute` - The attribute name for which the value should be replaced.
///  - `value` - The new attribute value.
let updateConfigSetting fileName xpath attribute value =
    readConfig fileName
    |> updateConfig xpath attribute value 
    |> writeConfig fileName

/// Reads a config file from the given file name, replaces the app setting value and writes it back.
/// ## Parameters
///  - `key` - The AppSettings attribute key name for which the value should be replaced.
///  - `value` - The new AppSettings attribute value.
///  - `fileName` - The file name of the config file.
///
/// ## Sample
///
///     updateAppSetting "DatabaseName" targetDatabase (navServicePath @@ "CustomSettings.config")
let updateAppSetting key value fileName =
    updateConfigSetting fileName ("//appSettings/add[@key='" + key + "']") "value" value

/// Reads a config file from the given file name, replaces the connection string value and writes it back.   
/// ## Parameters
///  - `connectionStringKey` - The connection string key name for which the value should be replaced.
///  - `value` - The new connection string value.
///  - `fileName` - The file name of the config file.     
let updateConnectionString connectionStringKey value fileName =
    updateConfigSetting fileName ("//connectionStrings/add[@name='" + connectionStringKey + "']") "connectionString" value
