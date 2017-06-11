/// Contains a task to run the msbuild runner of [Sonar Qube analyzer](http://sonarqube.org).
[<System.Obsolete("Use Fake.Testing.SonarQube instead (open Fake.Testing and use 'SonarQube.')")>]
module Fake.SonarQubeHelper
open TraceHelper

/// The supported commands of Sonar Qube. It is called with Begin before compilation, and End after compilation.
[<System.Obsolete("Use Fake.Testing.SonarQube instead (open Fake.Testing and use 'SonarQube.')")>]
type SonarQubeCall = Begin | End

[<System.Obsolete("Use Fake.Testing.SonarQube instead (open Fake.Testing and use 'SonarQube.')")>]
/// Parameter type to configure the sonar qube runner.
[<CLIMutable>]
type SonarQubeParams =
    { /// FileName of the sonar qube runner exe. 
      ToolsPath : string
      /// Key to identify the sonar qube project
      Key : string
      /// Name of the project
      Name : string
      /// Version number of the project
      Version : string
      /// Individual global settings for SonarQube
      Settings : List<string>
      /// Read settings from configuration file
      Config : string option
    }

[<System.Obsolete("Use Fake.Testing.SonarQube instead (open Fake.Testing and use 'SonarQube.')")>]
/// SonarQube default parameters - tries to locate MSBuild.SonarQube.exe in any subfolder.
let SonarQubeDefaults = 
    { ToolsPath = findToolInSubPath "MSBuild.SonarQube.Runner.exe" (currentDirectory @@ "tools" @@ "SonarQube")
      Key = null
      Name = null
      Version = "1.0"
      Settings = []
      Config = None }

[<System.Obsolete("Use Fake.Testing.SonarQube instead (open Fake.Testing and use 'SonarQube.')")>]
/// Execute the external msbuild runner of Sonar Qube. Parameters are fiven to the command line tool as required.
let SonarQubeCall (call: SonarQubeCall) (parameters : SonarQubeParams) =
  let sonarPath = parameters.ToolsPath 
  let setArgs = parameters.Settings |> List.fold (fun acc x -> acc + "/d:"+x+" ") ""

  let cfgArgs = 
    match parameters.Config with
    | Some(x) -> (" /s:"+x) 
    | None -> ""
  
  let args = 
    match call with
    | Begin -> "begin /k:\"" + parameters.Key + "\" /n:\"" + parameters.Name + "\" /v:\"" + parameters.Version + "\" " + setArgs + cfgArgs
    | End -> "end " + setArgs + cfgArgs

  let result =
    ExecProcess (fun info ->
      info.FileName <- sonarPath
      info.Arguments <- args) System.TimeSpan.MaxValue
  if result <> 0 then failwithf "Error during sonar qube call %s" (call.ToString())

/// This task to can be used to run [Sonar Qube](http://sonarqube.org/) on a project.
/// ## Parameters
///
///  - `call` - Begin or End, to start analysis or finish it
///  - `setParams` - Function used to overwrite the SonarQube default parameters.
///
/// ## Sample

///   SonarQube Begin (fun p ->
///    {p with
///      Key = "MyProject"
///      Name = "MainTool"
///      Version = "1.0 })
///
[<System.Obsolete("Use Fake.Testing.SonarQube instead (open Fake.Testing and use 'SonarQube.')")>]
let SonarQube (call: SonarQubeCall) setParams = 
    use __ = traceStartTaskUsing "SonarQube" (call.ToString())
    let parameters = setParams SonarQubeDefaults
    SonarQubeCall call parameters

/// This task can be used to run the end command of [Sonar Qube](http://sonarqube.org/) on a project.
///
/// ## Sample

///   SonarQubeEnd
///
[<System.Obsolete("Use Fake.Testing.SonarQube instead (open Fake.Testing and use 'SonarQube.')")>]
let SonarQubeEnd() =
    SonarQube End (fun p -> { p with Settings = [] })
