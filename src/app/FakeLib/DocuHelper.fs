[<AutoOpen>]
module Fake.DocuHelper

open System

type DocuParams =
    { ToolPath: string;
      TemplatesPath: string;
      TimeOut: TimeSpan;
      OutputPath: string}

/// Docu default params  
let DocuDefaults =
    let toolPath = findToolInSubPath "docu.exe" (currentDirectory @@ "tools" @@ "Fake")
    let fi = fileInfo toolPath
    { ToolPath = findToolInSubPath "docu.exe" (currentDirectory @@ "tools" @@ "Fake");
      TemplatesPath = fi.Directory.FullName @@ "templates"
      TimeOut = TimeSpan.FromMinutes 5.
      OutputPath = "./output" }
   
let Docu setParams assemblies = 
    let details = assemblies |> separated ", "
    traceStartTask "Docu" details
    let parameters = DocuDefaults |> setParams

    let files = assemblies |> Seq.map FullName |> separated " "
    let args =
        files +
            " --output=" + (parameters.OutputPath |> FullName) +
            " --templates=" + (parameters.TemplatesPath |> FullName)

    if not (execProcess3 (fun info ->  
        info.FileName <- parameters.ToolPath |> FullName                 
        info.Arguments <- args) parameters.TimeOut)
    then
        failwith "Documentation generation failed."
                    
    traceEndTask "Docu" details