/// Contains functions used to transform config (or any XML) files using Microsoft's XML Document Transformations.
[<RequireQualifiedAccess>]
module Fake.DotNet.Xdt

open System
open System.IO
open System.Xml
open DotNet.Xdt
open Fake.Core

let private xdtTag = KnownTags.Task "XDT"

/// Integrates XDT logging into FAKE logging.
type internal FakeXmlTransformationLogger() =
    interface IXmlTransformationLogger with
        member x.EndSection(message, messageArgs) = 
            Trace.log(String.Format(message, messageArgs))
            Trace.closeTagUnsafe xdtTag
        member x.EndSection(``type``, message, messageArgs) = 
            match ``type`` with
            | MessageType.Verbose -> 
                Trace.trace(String.Format(message, messageArgs))
                Trace.closeTagUnsafe xdtTag
            | _ -> 
                Trace.log(String.Format(message, messageArgs))
                Trace.closeTagUnsafe xdtTag

        member x.LogError(message, messageArgs) =
            Trace.traceError(String.Format(message, messageArgs))

        member x.LogError(file, message, messageArgs) =
            Trace.traceErrorfn "File: %s" file
            Trace.traceError(String.Format(message, messageArgs))

        member x.LogError(file, lineNumber, linePosition, message, messageArgs) =
            Trace.traceErrorfn "File: %s:%d:%d" file lineNumber linePosition
            Trace.traceError(String.Format(message, messageArgs))

        member x.LogErrorFromException(ex) =
            Trace.traceException ex

        member x.LogErrorFromException(ex, file) =
            Trace.traceErrorfn "File: %s" file
            Trace.traceException ex

        member x.LogErrorFromException(ex, file, lineNumber, linePosition) =
            Trace.traceErrorfn "File: %s:%d:%d" file lineNumber linePosition
            Trace.traceException ex

        member x.LogMessage(message, messageArgs) =
            Trace.log(String.Format(message, messageArgs))

        member x.LogMessage(``type``, message, messageArgs) =
            match ``type`` with
            | MessageType.Verbose -> Trace.trace(String.Format(message, messageArgs))
            | _ -> Trace.log(String.Format(message, messageArgs))

        member x.LogWarning(message, messageArgs) =
            Trace.traceImportant(String.Format(message, messageArgs))

        member x.LogWarning(file, message, messageArgs) =
            Trace.traceImportantfn "File: %s" file
            Trace.traceImportant(String.Format(message, messageArgs))

        member x.LogWarning(file, lineNumber, linePosition, message, messageArgs) =
            Trace.traceImportantfn "File: %s:%d:%d" file lineNumber linePosition
            Trace.traceImportant(String.Format(message, messageArgs))

        member x.StartSection(message, messageArgs) =
            Trace.openTagUnsafe xdtTag "StartSection"
            Trace.log(String.Format(message, messageArgs))

        member x.StartSection(``type``, message, messageArgs) =
            match ``type`` with
            | MessageType.Verbose -> 
                Trace.openTagUnsafe xdtTag "StartSectionVerbose"
                Trace.trace(String.Format(message, messageArgs))
            | _ -> 
                Trace.openTagUnsafe xdtTag "StartSectionNormal"
                Trace.log(String.Format(message, messageArgs))

/// Reads XML file (typically a config file), makes changes according to XDT transform syntax, saves result.
let transformFile (inXmlFile:string) (transformFile:string) (outXmlFile:string) =
    if not <| File.Exists inXmlFile then Trace.traceErrorfn "XML file %s does not exist." inXmlFile
    if not <| File.Exists transformFile then Trace.traceErrorfn "XML Document Transform file %s does not exist." transformFile
    use xdtStream = new FileStream(Path.GetFullPath transformFile, FileMode.Open, FileAccess.Read)
    let fakeLogger = new FakeXmlTransformationLogger()
    use xdt = new XmlTransformation(xdtStream, fakeLogger)
    let xml = new XmlDocument()
    xml.Load(Path.GetFullPath inXmlFile)
    if not <| xdt.Apply(xml) then new InvalidOperationException(sprintf "Unable to transform %s with %s." inXmlFile transformFile) |> raise
    xml.Save(outXmlFile)

/// Modifies an XML file in place using an XDT file named by inserting a .configName in between the filename and .extension.
let transformFileWithConfigName (configName:string) (xmlFile:string) =
    let xdt = Path.ChangeExtension(xmlFile, configName + (Path.GetExtension(xmlFile)))
    if File.Exists xdt then
        transformFile xmlFile xdt xmlFile
    else
        Trace.traceImportantfn "No %s config file found for '%s'. Skipping." configName xmlFile

/// Modifies XML files in place using an XDT file named by inserting a .configName in between each filename and .extension.
let transformFilesWithConfigName (configName:string) (files:Fake.IO.IGlobbingPattern) = 
    Seq.iter (transformFileWithConfigName configName) files
