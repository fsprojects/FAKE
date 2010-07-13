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
    { ToolPath = currentDirectory @@ "tools" @@ "FAKE" @@ "docu.exe"
      TemplatesPath = currentDirectory @@ "templates"
      TimeOut = TimeSpan.FromMinutes 5.
      OutputPath = currentDirectory @@ "output" }
   
let Docu setParams assemblies = 
    let details = assemblies |> separated ", "
    traceStartTask "Docu" details
    let parameters = DocuDefaults |> setParams

    let files = assemblies |> Seq.map FullName |> separated " "
    let args =
        files +
            " --output=" + parameters.OutputPath +
            " --templates=" + parameters.TemplatesPath

    tracefn  "%s %s" parameters.ToolPath args
    if not (execProcess3 (fun info ->  
        info.FileName <- parameters.ToolPath        
        info.Arguments <- args) parameters.TimeOut)
    then
        failwith "Documentation generation failed."
                    
    traceEndTask "Docu" details