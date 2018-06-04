[<AutoOpen>]
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
/// Contains helper functions to run the XML documentation tool "docu".
module Fake.DocuHelper

open System

/// The parameter type for docu.
[<CLIMutable>]
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type DocuParams = 
    { /// The tool path - FAKE tries to find docu.exe automatically in any sub folder.
      ToolPath : string
      /// The HTML templates for the generated docs.
      TemplatesPath : string
      /// Allows to specify a timeout for docu. The default is 5 minutes.
      TimeOut : TimeSpan
      /// The output path of the generated docs. The default is "./output/".
      OutputPath : string }

/// The Docu default params
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let DocuDefaults = 
    let toolPath = findToolInSubPath "docu.exe" (currentDirectory @@ "tools" @@ "Fake")
    let fi = fileInfo toolPath
    { ToolPath = findToolInSubPath "docu.exe" (currentDirectory @@ "tools" @@ "Fake")
      TemplatesPath = fi.Directory.FullName @@ "templates"
      TimeOut = TimeSpan.FromMinutes 5.
      OutputPath = "./output" }

/// Generates a HTML documentation from XML docs via the docu.exe.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default docu parameters.
///  - `assemblies` - Sequence of one or more assemblies containing the XML docs.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let Docu setParams assemblies = 
    let details = assemblies |> separated ", "
    use __ = traceStartTaskUsing "Docu" details
    let parameters = DocuDefaults |> setParams
    
    let files = 
        assemblies
        |> Seq.map FullName
        |> separated " "
    
    let args = 
        files + " --output=" + (parameters.OutputPath |> FullName) + " --templates=" 
        + (parameters.TemplatesPath |> FullName)
    if 0 <> ExecProcess (fun info -> 
                info.FileName <- parameters.ToolPath |> FullName
                info.Arguments <- args) parameters.TimeOut
    then failwith "Documentation generation failed."
