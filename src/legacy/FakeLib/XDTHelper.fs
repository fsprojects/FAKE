/// Contains functions used to transform config (or any XML) files using Microsoft's XML Document Transformations.
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module Fake.XDTHelper

open System
open System.IO
open System.Xml
open Microsoft.Web.XmlTransform

/// Integrates XDT logging into FAKE logging.
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type FakeXmlTransformationLogger() =
    interface IXmlTransformationLogger with
        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.EndSection(message, messageArgs) = 
            postMessage <| LogMessage(String.Format(message, messageArgs), true)
            postMessage <| CloseTag("XDT")
        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.EndSection(``type``, message, messageArgs) = 
            match ``type`` with
            | MessageType.Verbose -> 
                postMessage <| TraceMessage(String.Format(message, messageArgs), true)
                postMessage <| CloseTag("XDT")
            | _ -> 
                postMessage <| LogMessage(String.Format(message, messageArgs), true)
                postMessage <| CloseTag("XDT")

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]            
        member x.LogError(message, messageArgs) =
            postMessage <| ErrorMessage(String.Format(message, messageArgs))
        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]        

        member x.LogError(file, message, messageArgs) =
            postMessage <| ErrorMessage(sprintf "File: %s" file)
            postMessage <| ErrorMessage(String.Format(message, messageArgs))

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]        
        member x.LogError(file, lineNumber, linePosition, message, messageArgs) =
            postMessage <| ErrorMessage(sprintf "File: %s:%d:%d" file lineNumber linePosition)
            postMessage <| ErrorMessage(String.Format(message, messageArgs))

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]        
        member x.LogErrorFromException(ex) =
            postMessage <| ErrorMessage(string ex)

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
        member x.LogErrorFromException(ex, file) =
            postMessage <| ErrorMessage(sprintf "File: %s" file)
            postMessage <| ErrorMessage(string ex)

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]        
        member x.LogErrorFromException(ex, file, lineNumber, linePosition) =
            postMessage <| ErrorMessage(sprintf "File: %s:%d:%d" file lineNumber linePosition)
            postMessage <| ErrorMessage(string ex)

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]        
        member x.LogMessage(message, messageArgs) =
            postMessage <| LogMessage(String.Format(message, messageArgs), true)

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]        
        member x.LogMessage(``type``, message, messageArgs) =
            match ``type`` with
            | MessageType.Verbose -> postMessage <| TraceMessage(String.Format(message, messageArgs), true)
            | _ -> postMessage <| LogMessage(String.Format(message, messageArgs), true)

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]        
        member x.LogWarning(message, messageArgs) =
            postMessage <| ImportantMessage(String.Format(message, messageArgs))

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]        
        member x.LogWarning(file, message, messageArgs) =
            postMessage <| ImportantMessage(sprintf "File: %s" file)
            postMessage <| ImportantMessage(String.Format(message, messageArgs))

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]        
        member x.LogWarning(file, lineNumber, linePosition, message, messageArgs) =
            postMessage <| ImportantMessage(sprintf "File: %s:%d:%d" file lineNumber linePosition)
            postMessage <| ImportantMessage(String.Format(message, messageArgs))

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]        
        member x.StartSection(message, messageArgs) =
            postMessage <| OpenTag("XDT","StartSection")
            postMessage <| LogMessage(String.Format(message, messageArgs), true)

        [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]        
        member x.StartSection(``type``, message, messageArgs) =
            match ``type`` with
            | MessageType.Verbose -> 
                postMessage <| OpenTag("XDT","StartSectionVerbose")
                postMessage <| TraceMessage(String.Format(message, messageArgs), true)
            | _ -> 
                postMessage <| OpenTag("XDT","StartSectionNormal")
                postMessage <| LogMessage(String.Format(message, messageArgs), true)

/// Reads XML file (typically a config file), makes changes according to XDT transform syntax, saves result.
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]        
let TransformFile (inXmlFile:string) (transformFile:string) (outXmlFile:string) =
    if not <| File.Exists inXmlFile then postMessage <| ErrorMessage(sprintf "XML file %s does not exist." inXmlFile)
    if not <| File.Exists transformFile then postMessage <| ImportantMessage(sprintf "XML Document Transform file %s does not exist." transformFile)
    use xdtStream = new FileStream(Path.GetFullPath transformFile, FileMode.Open, FileAccess.Read)
    let fakeLogger = new FakeXmlTransformationLogger()
    use xdt = new XmlTransformation(xdtStream, fakeLogger)
    let xml = new XmlDocument()
    xml.Load(Path.GetFullPath inXmlFile)
    if not <| xdt.Apply(xml) then new InvalidOperationException(sprintf "Unable to transform %s with %s." inXmlFile transformFile) |> raise
    xml.Save(outXmlFile)

/// Modifies an XML file in place using an XDT file named by inserting a .configName in between the filename and .extension.
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]        
let TransformFileWithConfigName (configName:string) (xmlFile:string) =
    let xdt = Path.ChangeExtension(xmlFile, configName + (Path.GetExtension(xmlFile)))
    if File.Exists xdt then
        TransformFile xmlFile xdt xmlFile
    else
        postMessage <| ImportantMessage(sprintf "No %s config file found for '%s'. Skipping." configName xmlFile)

/// Modifies XML files in place using an XDT file named by inserting a .configName in between each filename and .extension.
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]        
let TransformFilesWithConfigName (configName:string) (files:FileIncludes) = 
    Seq.iter (TransformFileWithConfigName configName) files
