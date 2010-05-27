[<AutoOpen>]
module Fake.XUnitHelper

open System
open System.IO
open System.Text

type XUnitParams =
 { ToolPath: string;
   ConfigFile :string;
   HtmlOutput: bool;
   NUnitXmlOutput: bool;
   XmlOutput: bool;
   WorkingDir:string; 
   ShadowCopy :bool;
   Verbose:bool;
   OutputDir: string}

/// xUnit default params  
let XUnitDefaults =
 { ToolPath = currentDirectory @@ "tools" @@ "xUnit" @@ "xunit.console.exe";
   ConfigFile = null;
   HtmlOutput = false;
   NUnitXmlOutput = false;
   WorkingDir = null;
   ShadowCopy = true;
   Verbose = false;
   XmlOutput = false
   OutputDir = null}

let xUnit setParams assemblies = 
    let details = separated ", " assemblies
    traceStartTask "xUnit" details
    let parameters = setParams XUnitDefaults
    assemblies
      |> Seq.iter (fun assembly ->
          let commandLineBuilder =          
              let fi = fileInfo assembly
              let name = fi.Name

              let dir = 
                if isNullOrEmpty parameters.OutputDir then String.Empty else
                Path.GetFullPath parameters.OutputDir

              new StringBuilder()
                |> appendFileNamesIfNotNull [assembly]
                |> appendIfFalse parameters.ShadowCopy "/noshadow"
                |> appendIfTrue (buildServer = TeamCity) "/teamcity"
                |> appendIfTrue parameters.XmlOutput (sprintf "/xml\" \"%s%s.xml" dir name) 
                |> appendIfTrue parameters.HtmlOutput (sprintf "/html\" \"%s%s.html" dir name) 
                |> appendIfTrue parameters.NUnitXmlOutput (sprintf "/nunit\" \"%s%s.xml" dir name) 
      
          if not (execProcess3 (fun info ->  
              info.FileName <- parameters.ToolPath
              info.WorkingDirectory <- parameters.WorkingDir
              info.Arguments <- commandLineBuilder.ToString()))
          then
              failwith "xUnit test failed.")
                  
    traceEndTask "xUnit" details