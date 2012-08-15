[<AutoOpen>]
module Fake.WiXHelper

open System
open System.IO

let mutable fileCount = 0

let wixFile (fi:FileInfo) =
    fileCount <- fileCount + 1
    sprintf "<File Id=\"fi_%d\" Name=\"%s\" Source=\"%s\" />" fileCount fi.Name fi.FullName

let rec wixDir fileFilter asSubDir (dir:System.IO.DirectoryInfo) =
    let dirs =
      dir
        |> subDirectories
        |> Seq.map (wixDir fileFilter true)
        |> separated ""

    let files =
      dir
        |> filesInDir
        |> Seq.filter fileFilter
        |> Seq.map wixFile
        |> separated ""

    let compo =
      if files = "" then "" else
      sprintf "<Component Id=\"%s\" Guid=\"%s\">%s</Component>" dir.Name (Guid.NewGuid().ToString()) files

    if asSubDir then
        sprintf "<Directory Id=\"%s\" Name=\"%s\">%s%s</Directory>" dir.Name dir.Name dirs compo
    else
        sprintf "%s%s" dirs compo

let rec wixComponentRefs (dir:DirectoryInfo) =
    let compos =
      dir
        |> subDirectories
        |> Seq.map wixComponentRefs
        |> separated ""

    if (filesInDir dir).Length > 0 then sprintf "%s<ComponentRef Id=\"%s\"/>" compos dir.Name else compos

let getFilesAsWiXString files =
    files
      |> Seq.map (fileInfo >> wixFile)
      |> separated " "

open System

type WiXParams = 
    { ToolDirectory: string;
      TimeOut: TimeSpan }

/// WiX default params  
let WiXDefaults : WiXParams = 
    { ToolDirectory = currentDirectory @@ "tools" @@ "Wix";
      TimeOut = TimeSpan.FromMinutes 5.0 }
   
let Candle (parameters:WiXParams) wixScript = 
    traceStartTask "Candle" wixScript  

    let fi = fileInfo wixScript
    let wixObj = fi.Directory.FullName @@ sprintf @"%s.wixobj" fi.Name

    let tool = parameters.ToolDirectory @@ "candle.exe"
    let args = 
        sprintf "-out \"%s\" \"%s\" -ext WiXNetFxExtension" 
            wixObj
            (wixScript |> FullName)

    tracefn "%s %s" parameters.ToolDirectory args
    if not (execProcess3 (fun info ->  
        info.FileName <- tool
        info.WorkingDirectory <- null
        info.Arguments <- args) parameters.TimeOut)
    then
        failwith "Candle failed."
                    
    traceEndTask "Candle" wixScript
    wixObj


let Light (parameters:WiXParams) outputFile wixObj = 
    traceStartTask "Light" wixObj   

    let tool = parameters.ToolDirectory @@ "light.exe"
    let args = 
            sprintf "\"%s\" -spdb -dcl:high -out \"%s\" -ext WiXNetFxExtension -ext WixUIExtension.dll -ext WixUtilExtension.dll" 
                (wixObj |> FullName)
                (outputFile |> FullName)

    tracefn "%s %s" parameters.ToolDirectory args
    if not (execProcess3 (fun info ->  
        info.FileName <- tool
        info.WorkingDirectory <- null
        info.Arguments <- args) parameters.TimeOut)
    then
        failwith "Light failed."
                    
    traceEndTask "Light" wixObj

/// <summary>Use the WiX tools Candle and Light to create an msi.</summary>
/// <param name="setParams">Function used to create an WiXParams value with your required settings.  Called with an WixParams value configured with the defaults.</param>
/// <param name="outputFile">The msi output file path (given to Light).</param>
/// <param name="wixScript">The path to a WiX script that will be used with Candle.</param>
/// <user/>
let WiX setParams outputFile wixScript =
    let parameters = setParams WiXDefaults     
    wixScript
      |> Candle parameters 
      |> Light parameters outputFile 