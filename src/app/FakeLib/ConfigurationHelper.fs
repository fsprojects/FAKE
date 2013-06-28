[<AutoOpen>]
module Fake.ConfigurationHelper
    
    open System.IO
    open System.Xml
    open System.Linq
    open System.Xml.Linq
    open System.Xml.XPath

    let readConfig file =
        use fileStream = File.OpenRead(file) 
        let configElement = XElement.Load(fileStream)
        fileStream.Close()
        configElement

    let private getElement config xpath =
       (Extensions.XPathSelectElement(config, xpath))

    let updateConfigSetting file xpath attribute value = 
        let config = readConfig file
        let node = getElement config xpath
        if node = null 
        then failwithf "Could not find node addressed by %s in file %s" xpath file
        else
            let attr = node.Attribute(XName.Get(attribute)) 
            attr.Value <- value           
            use fs = File.Open(file, FileMode.Truncate, FileAccess.Write)
            config.Save(fs)

    let updateAppSetting nodeName value file =
        updateConfigSetting file ("appSettings/add[@key='" + nodeName + "']") "value" value
        
    let updateConnectionString nodeName value file =
        updateConfigSetting file ("connectionStrings/add[@name='" + nodeName + "']") "connectionString" value

