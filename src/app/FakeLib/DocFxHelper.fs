/// Contains helper functions to run the documentation tool "docfx".
module Fake.DocFxHelper

open System

/// The parameter type for DocFx.
type DocFxParams = 
    { /// The tool path - FAKE tries to find docfx.exe automatically in any sub folder.
      ToolPath : string
      /// the DocFxJson Config-File. Default: docs/docfx.json
      DocFxJson : string
      /// DocFx WorkingDirectory. Default: docs
      WorkingDirectory  : string
      /// Allows to specify a timeout for DocFx. Default: 5 min
      Timeout : TimeSpan
      /// Serves the generated documentation on localhost. Default: false
      Serve : bool
    }

/// The default parameters
let DocFxDefaults = 
    let toolPath = findToolInSubPath "docfx.exe" (currentDirectory @@ "tools" @@ "docfx.msbuild")
    let docsPath = currentDirectory @@ "docs"
    { ToolPath = toolPath
      DocFxJson = docsPath @@ "docfx.json"
      WorkingDirectory = docsPath
      Timeout = TimeSpan.FromMinutes 5.
      Serve = false
    }

/// Generates a DocFx documentation.
/// ## Parameters
///  - `setParams` - Function used to manipulate the default DocFx parameters. See `DocFxDefaults`
/// ## Sample
///
///     DocFx (fun p -> 
///      { p with 
///          DocFxJson = "foo" @@ "bar" @@ "docfx.json"
///          Timeout = TimeSpan.FromMinutes 10.
///      })
let DocFx setParams = 
    let parameters = DocFxDefaults |> setParams
    
    traceStartTask "DocFx" parameters.DocFxJson
    
    let serveArg = if parameters.Serve then "--serve" else ""
    let configArg = parameters.DocFxJson |> FullName

    let args = sprintf "%s %s" configArg serveArg

    if 0 <> ExecProcess (fun info -> 
          info.FileName <- parameters.ToolPath |> FullName
          info.Arguments <- args
          info.WorkingDirectory <- parameters.WorkingDirectory
        ) parameters.Timeout
      then failwith "DocFx generation failed."

    traceEndTask "DocFx" parameters.DocFxJson
    
