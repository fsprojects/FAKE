[<AutoOpen>]
module Fake.WiXHelper

open System.IO

let mutable fileCount = 0

let wixFile (fi:FileInfo) =
    fileCount <- fileCount + 1
    sprintf "<File Id=\"fi_%d\" Name=\"%s\" Source=\"%s\" />" 
      fileCount fi.Name fi.FullName

let rec wixDir (dir:System.IO.DirectoryInfo) =
    let dirs =
      dir.GetDirectories()
        |> Seq.map wixDir
        |> separated ""

    let files =
      dir.GetFiles()
        |> Seq.map wixFile
        |> separated ""

    let compo =
      if files = "" then "" else
      sprintf "<Component Id=\"%s\" Guid=\"%s\">%s</Component>" dir.Name (System.Guid.NewGuid().ToString()) files

    sprintf "<Directory Id=\"%s\" Name=\"%s\">%s%s</Directory>" dir.Name dir.Name dirs compo

let rec wixComponentRefs (dir:DirectoryInfo) =
    let compos =
      dir.GetDirectories()
        |> Seq.map wixComponentRefs
        |> separated ""

    if dir.GetFiles().Length > 0 then sprintf "%s<ComponentRef Id=\"%s\"/>" compos dir.Name else compos

let getFilesAsWiXString files =
    files
      |> Seq.map (fun file -> new FileInfo(file))
      |> Seq.map wixFile
      |> separated " "

open System

type WiXParams =
 { ToolDirectory: string;}

/// WiX default params  
let WiXDefaults : WiXParams =
 { ToolDirectory = @".\tools\Wix\"; }
   
let Candle (parameters:WiXParams) wixScript = 
    traceStartTask "Candle" wixScript  

    let fi = new System.IO.FileInfo(wixScript)
    let wixObj = sprintf @"%s\%s.wixobj" fi.Directory.FullName fi.Name

    let tool = parameters.ToolDirectory + "candle.exe"
    let args = 
        sprintf "-out \"%s\" \"%s\" -ext WiXNetFxExtension" 
            wixObj
            (wixScript |> FullName)

    trace (parameters.ToolDirectory + " "  + args)
    if not (execProcess3 (fun info ->  
        info.FileName <- tool
        info.WorkingDirectory <- null
        info.Arguments <- args))
    then
        failwith "Candle failed."
                    
    traceEndTask "Candle" wixScript
    wixObj


let Light (parameters:WiXParams) outputFile wixObj = 
    traceStartTask "Light" wixObj   

    let tool = parameters.ToolDirectory + "light.exe"
    let args = 
            sprintf "\"%s\" -spdb -dcl:high -out \"%s\" -ext WiXNetFxExtension -ext WixUIExtension.dll -ext WixUtilExtension.dll" 
                (wixObj |> FullName)
                (outputFile |> FullName)

    trace (parameters.ToolDirectory + " "  + args)
    if not (execProcess3 (fun info ->  
        info.FileName <- tool
        info.WorkingDirectory <- null
        info.Arguments <- args))
    then
        failwith "Light failed."
                    
    traceEndTask "Light" wixObj

/// Uses Candle and Light to create a msi.
let WiX setParams outputFile wixScript =
    let parameters = WiXDefaults |> setParams    
    wixScript
      |> Candle parameters 
      |> Light parameters outputFile 