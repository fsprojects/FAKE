/// Contains tasks to host webprojects in IIS Express.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Fake.IISExpress

open System.Diagnostics
open System
open System.IO
open System.Xml.Linq

/// Options for using IISExpress
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type IISExpressOptions = 
    { ToolPath : string }

/// IISExpress default parameters - tries to locate the iisexpress.exe
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let IISExpressDefaults = 
    { ToolPath = 
          let root = 
              if Environment.Is64BitOperatingSystem then 
                  Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
              else Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
          
          Path.Combine(root, "IIS Express", "iisexpress.exe") }

/// Create a IISExpress config file from a given template
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let createConfigFile (name, siteId : int, templateFileName, path, hostName, port : int) = 
    let xname s = XName.Get(s)
    let uniqueConfigFile = Path.Combine(Path.GetTempPath(), "iisexpress-" + Guid.NewGuid().ToString() + ".config")
    use template = File.OpenRead(templateFileName)
    let xml = XDocument.Load(template)
    let sitesElement = xml.Root.Element(xname "system.applicationHost").Element(xname "sites")
    let appElement = 
        XElement
            (xname "site", XAttribute(xname "name", name), XAttribute(xname "id", siteId.ToString()), 
             XAttribute(xname "serverAutoStart", "true"), 
             
             XElement
                 (xname "application", XAttribute(xname "path", "/"), 
                  
                  XElement
                      (xname "virtualDirectory", XAttribute(xname "path", "/"), XAttribute(xname "physicalPath", DirectoryInfo(path).FullName))), 
             
             XElement
                 (xname "bindings", 
                  
                  XElement
                      (xname "binding", XAttribute(xname "protocol", "http"), 
                       XAttribute(xname "bindingInformation", "*:" + port.ToString() + ":" + hostName)),
                       
                  XElement
                      (xname "binding", XAttribute(xname "protocol", "http"), 
                       XAttribute(xname "bindingInformation", "*:" + port.ToString() + ":*"))))
    sitesElement.Add(appElement)
    xml.Save(uniqueConfigFile)
    uniqueConfigFile

/// This task starts the given site in IISExpress with the given ConfigFile.
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the default parameters.
///  - `configFileName` - The file name of the IISExpress configfile.
///  - `siteId` - The id (in the config file) of the website to run.
///
/// ## Sample
///
///      HostWebsite (fun p -> { p with ToolPath = "iisexpress.exe" }) "configfile.config" 1
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let HostWebsite setParams configFileName siteId = 
    let parameters = setParams IISExpressDefaults

    use __ = traceStartTaskUsing "StartWebSite" configFileName
    let args = sprintf "/config:\"%s\" /siteid:%d" configFileName siteId
    tracefn "Starting WebSite with %s %s" parameters.ToolPath args

    let proc = 
        ProcessStartInfo(FileName = parameters.ToolPath, Arguments = args, UseShellExecute = false) 
        |> Process.Start

    proc

/// Opens the given url in the browser
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let OpenUrlInBrowser url = Process.Start(url:string) |> ignore

/// Opens the given website in the browser
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let OpenWebsiteInBrowser hostName port = sprintf "http://%s:%d/" hostName port |> OpenUrlInBrowser
