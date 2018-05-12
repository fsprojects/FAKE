/// Contains a task which allows to run [NDepend](http://www.ndepend.com/) on .NET project files.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Fake.NDepend

open Fake
open System
open System.IO
open System.Text

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let getWorkingDir workingDir =
    Seq.find isNotNullOrEmpty [workingDir; environVar("teamcity.build.workingDir"); "."]  // TODO: other build servers?
    |> Path.GetFullPath
    
[<CLIMutable>]
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type NDependParams = 
    { ToolPath : string
      WorkingDir : string
      ProjectFile : string
      CoverageFiles : string list }

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let NDependDefaults = 
    { ToolPath = findToolInSubPath "ndepend.console.exe" (currentDirectory @@ "tools" @@ "NDepend")
      WorkingDir = ""
      ProjectFile = ""
      CoverageFiles = [] }

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
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
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let NDepend(setParams : NDependParams -> NDependParams) = 
    let taskName = "NDepend"
    use __ = traceStartTaskUsing taskName ""
    let parameters = (NDependDefaults |> setParams)
    let args = buildNDependArgs parameters
    trace (parameters.ToolPath + " " + args)
    let result = 
        ExecProcess (fun info -> 
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- getWorkingDir parameters.WorkingDir
            info.Arguments <- args) TimeSpan.MaxValue
    if result <> 0 then failwithf "Error running %s" parameters.ToolPath
