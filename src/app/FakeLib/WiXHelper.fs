[<AutoOpen>]
/// Contains tasks to create msi installers using the [WiX toolset](http://wixtoolset.org/)
module Fake.WiXHelper

open System
open System.IO
open System.Collections.Generic

let mutable internal fileCount = 0

let mutable internal dirs = Dictionary()

let dirName dir =
    match dirs.TryGetValue dir with
    | true,n -> dirs.[dir] <- n+1; dir + n.ToString()
    | _ -> dirs.[dir] <- 1; dir

let mutable internal compRefs = Dictionary()

let compRefName compRef =
    match compRefs.TryGetValue compRef with
    | true,n -> compRefs.[compRef] <- n+1; compRef + n.ToString()
    | _ -> compRefs.[compRef] <- 1; compRef

let mutable internal comps = Dictionary()

let compName comp =
    match comps.TryGetValue comp with
    | true,n -> comps.[comp] <- n+1; comp + n.ToString()
    | _ -> comps.[comp] <- 1; comp



/// Creates a WiX File tag from the given FileInfo
let wixFile (fileInfo:FileInfo) =
    fileCount <- fileCount + 1
    sprintf "<File Id=\"fi_%d\" Name=\"%s\" Source=\"%s\" />" fileCount fileInfo.Name fileInfo.FullName

/// Creates WiX File tags from the given files
let getFilesAsWiXString files =
    files
      |> Seq.map (fileInfo >> wixFile)
      |> toLines

/// Creates recursive WiX directory and file tags from the given DirectoryInfo
let rec wixDir fileFilter asSubDir (directoryInfo:DirectoryInfo) =
    let dirs =
      directoryInfo
        |> subDirectories
        |> Seq.map (wixDir fileFilter true)
        |> toLines

    let files =
      directoryInfo
        |> filesInDir
        |> Seq.filter fileFilter
        |> Seq.map wixFile
        |> toLines

    let compo =
      if files = "" then "" else
      sprintf "<Component Id=\"%s\" Guid=\"%s\">\r\n%s\r\n</Component>\r\n" (compName directoryInfo.Name) (Guid.NewGuid().ToString()) files

    if asSubDir then
        sprintf "<Directory Id=\"%s\" Name=\"%s\">\r\n%s%s\r\n</Directory>\r\n" (dirName directoryInfo.Name) directoryInfo.Name dirs compo
    else
        sprintf "%s%s" dirs compo

/// Creates WiX ComponentRef tags from the given DirectoryInfo
let rec wixComponentRefs (directoryInfo:DirectoryInfo) =
    let compos =
      directoryInfo
        |> subDirectories
        |> Seq.map wixComponentRefs
        |> toLines

    if (filesInDir directoryInfo).Length > 0 then sprintf "%s<ComponentRef Id=\"%s\"/>\r\n" compos (compRefName directoryInfo.Name) else compos

open System

/// WiX parameter type
type WiXParams = { 
      ToolDirectory: string
      TimeOut: TimeSpan
      AdditionalCandleArgs: string list
      AdditionalLightArgs: string list }

/// Contains the WiX default parameters  
let WiXDefaults : WiXParams = { 
    ToolDirectory = currentDirectory @@ "tools" @@ "Wix";
    TimeOut = TimeSpan.FromMinutes 5.0;
    AdditionalCandleArgs = [ "-ext WiXNetFxExtension" ];
    AdditionalLightArgs = [ "-ext WiXNetFxExtension"; "-ext WixUIExtension.dll"; "-ext WixUtilExtension.dll" ] }
   
/// Runs the [Candle tool](http://wixtoolset.org/documentation/manual/v3/overview/candle.html) on the given WiX script with the given parameters
let Candle (parameters:WiXParams) wixScript = 
    traceStartTask "Candle" wixScript  

    let fi = fileInfo wixScript
    let wixObj = fi.Directory.FullName @@ sprintf @"%s.wixobj" fi.Name

    let tool = parameters.ToolDirectory @@ "candle.exe"
    let args = 
        sprintf "-out \"%s\" \"%s\" %s" 
            wixObj
            (wixScript |> FullName)
            (separated " " parameters.AdditionalCandleArgs)

    tracefn "%s %s" parameters.ToolDirectory args
    if 0 = ExecProcess (fun info ->  
        info.FileName <- tool
        info.WorkingDirectory <- null
        info.Arguments <- args) parameters.TimeOut
    then
        failwithf "Candle %s failed." args
                    
    traceEndTask "Candle" wixScript
    wixObj

/// Runs the [Light tool](http://wixtoolset.org/documentation/manual/v3/overview/light.html) on the given WiX script with the given parameters
let Light (parameters:WiXParams) outputFile wixObj = 
    traceStartTask "Light" wixObj   

    let tool = parameters.ToolDirectory @@ "light.exe"
    let args = 
            sprintf "\"%s\" -spdb -dcl:high -out \"%s\" %s" 
                (wixObj |> FullName)
                (outputFile |> FullName)
                (separated " " parameters.AdditionalLightArgs)

    tracefn "%s %s" parameters.ToolDirectory args
    if 0 = ExecProcess (fun info ->  
        info.FileName <- tool
        info.WorkingDirectory <- null
        info.Arguments <- args) parameters.TimeOut
    then
        failwithf "Light %s failed." args
                    
    traceEndTask "Light" wixObj

/// Uses the WiX tools [Candle](http://wixtoolset.org/documentation/manual/v3/overview/candle.html) and [Light](http://wixtoolset.org/documentation/manual/v3/overview/light.html) to create an msi.
/// ## Parameters
///  - `setParams` - Function used to manipulate the WiX default parameters.
///  - `outputFile` - The msi output file path (given to Light).
///  - `wixScript` - The path to a WiX script that will be used with Candle.
///
/// ## Sample
///     Target "BuildSetup" (fun _ ->
///         // Copy all important files to the deploy directory
///         !! (buildDir + "/**/*.dll")
///           ++ (buildDir + "/**/*.exe")
///           ++ (buildDir + "/**/*.config")
///           |> Copy deployPrepDir 
///    
///         // replace tags in a template file in order to generate a WiX script
///         let ALLFILES = fun _ -> true
///     
///         let replacements = [
///             "@build.number@",if not isLocalBuild then buildVersion else "0.1.0.0"
///             "@product.productcode@",System.Guid.NewGuid().ToString()
///             "@HelpFiles@",getFilesAsWiXString helpFiles
///             "@ScriptFiles@",getFilesAsWiXString scriptFiles
///             "@icons@",wixDir ALLFILES true (directoryInfo(bundledDir @@ "icons"))]
///         
///         processTemplates replacements setupFiles
///     
///         // run the WiX tools
///         WiX (fun p -> {p with ToolDirectory = WiXPath}) 
///             setupFileName
///             (setupBuildDir + "Setup.wxs.template")
///     )
let WiX setParams outputFile wixScript =
    let parameters = setParams WiXDefaults     
    wixScript
      |> Candle parameters 
      |> Light parameters outputFile 