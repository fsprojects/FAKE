module Fake.NDepend

open Fake
open System
open System.IO
open System.Text

let getWorkingDir workingDir =
    Seq.find isNotNullOrEmpty [workingDir; environVar("teamcity.build.workingDir"); "."]
    |> Path.GetFullPath

let buildParamsAndExecute parameters buildArguments toolPath workingDir =
    let args = buildArguments parameters
    trace (toolPath + " " + args)
    let result = ExecProcess (fun info ->  
              info.FileName <- toolPath
              info.WorkingDirectory <- getWorkingDir workingDir
              info.Arguments <- args) TimeSpan.MaxValue
    if result <> 0 then failwithf "Error running %s" toolPath
    
type NDependParams = 
    { ToolPath: string
      WorkingDir: string
      ProjectFile: string
      CoverageFiles: string list }

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

let NDepend (setParams: NDependParams -> NDependParams) =
    let parameters = (NDependDefaults |> setParams)
    buildParamsAndExecute parameters buildNDependArgs parameters.ToolPath parameters.WorkingDir
