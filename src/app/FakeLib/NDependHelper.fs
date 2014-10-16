/// Contains a task which allows to run [NDepend](http://www.ndepend.com/) on .NET project files.
module Fake.NDepend

open Fake
open System
open System.IO
open System.Text

let getWorkingDir workingDir =
    Seq.find isNotNullOrEmpty [workingDir; environVar("teamcity.build.workingDir"); "."]  // TODO: other build servers?
    |> Path.GetFullPath
    
type NDependParams = 
    { ToolPath : string
      WorkingDir : string
      ProjectFile : string
      CoverageFiles : string list }

let NDependDefaults = 
    { ToolPath = findToolInSubPath "ndepend.console.exe" (currentDirectory @@ "tools" @@ "NDepend")
      WorkingDir = ""
      ProjectFile = ""
      CoverageFiles = [] }

let buildNDependArgs parameters = 
    new StringBuilder()
    |> append parameters.ProjectFile
    |> appendWithoutQuotes "/CoverageFiles "
    |> appendFileNamesIfNotNull parameters.CoverageFiles
    |> toText

/// Runs [NDepend](http://www.ndepend.com/) on a .NET project file.
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default NDependDefaults value.
///
/// ## Sample
///
///      NDepend (fun p -> { p with 
///                         ProjectFile = currentDirectory @@ "NDependProjectFile.ndproj"
///                         CoverageFiles = [artifactsDir @@ "DotCover.xml" ]
///              })
let NDepend(setParams : NDependParams -> NDependParams) = 
    let taskName = "NDepend"
    traceStartTask taskName ""
    let parameters = (NDependDefaults |> setParams)
    let args = buildNDependArgs parameters
    trace (parameters.ToolPath + " " + args)
    let result = 
        ExecProcess (fun info -> 
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- getWorkingDir parameters.WorkingDir
            info.Arguments <- args) TimeSpan.MaxValue
    if result <> 0 then failwithf "Error running %s" parameters.ToolPath
    traceEndTask taskName ""