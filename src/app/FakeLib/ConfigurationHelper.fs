[<AutoOpen>]
/// Contains functions which allow to read and write config files.
module Fake.ConfigurationHelper
    
open System.IO
open System.Xml
open System.Linq
open System.Xml.Linq
open System.Xml.XPath

/// Reads a config file from the given file name
let readConfig fileName =
    use fileStream = File.OpenRead(fileName) 
    let configElement = XElement.Load fileStream
    fileStream.Close()
    configElement

let private getElement config xpath =
    (Extensions.XPathSelectElement(config, xpath))

/// Reads a config file from the given file name, replaces an attribute using the given xPath and writes it back.
let updateConfigSetting fileName xpath attribute value =
    let config = readConfig fileName
    let node = getElement config xpath
    if node = null then 
        failwithf "Could not find node addressed by %s in file %s" xpath fileName
    else
        let attr = node.Attribute(XName.Get(attribute)) 
        attr.Value <- value           
        use fs = File.Open(fileName, FileMode.Truncate, FileAccess.Write)
        config.Save(fs)

/// Reads a config file from the given file name, replaces the app setting value and writes it back.
let updateAppSetting key value file =
    updateConfigSetting file ("appSettings/add[@key='" + key + "']") "value" value

/// Reads a config file from the given file name, replaces the connection string value and writes it back.        
let updateConnectionString connectionStringKey value file =
    updateConfigSetting file ("connectionStrings/add[@name='" + connectionStringKey + "']") "connectionString" value