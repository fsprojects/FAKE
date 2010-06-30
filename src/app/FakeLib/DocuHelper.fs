[<AutoOpen>]
module Fake.DocuHelper

type DocuParams =
    { ToolPath: string;
      TemplatesPath: string;
      OutputPath: string}

/// Docu default params  
let DocuDefaults =
    { ToolPath = currentDirectory @@ "tools" @@ "FAKE" @@ "docu.exe";
      TemplatesPath = currentDirectory @@ "templates";
      OutputPath = currentDirectory @@ "output" }
   
let Docu setParams assembly = 
    traceStartTask "Docu" assembly
    let parameters = DocuDefaults |> setParams

    let args =
        (assembly |> FullName) +
            " --output=" + parameters.OutputPath +
            " --templates=" + parameters.TemplatesPath

    tracefn  "%s %s" parameters.ToolPath args
    if not (execProcess3 (fun info ->  
        info.FileName <- parameters.ToolPath
        info.Arguments <- args))
    then
        failwith "Documentation generation failed."
                    
    traceEndTask "Docu" assembly