/// Contains a task to run the msbuild runner of [SonarQube analyzer](http://sonarqube.org).
module Fake.Testing.SonarQube

    open System.IO
    open Fake.Core
    open Fake.Core.Globbing
    open Fake.Core.Process
    open Fake.IO.FileSystemOperators

    /// [omit]
    /// The supported commands of SonarQube. It is called with Begin before compilation, and End after compilation.
    type private SonarQubeCall = Begin | End

    /// Parameter type to configure the SonarQube runner.
    type SonarQubeParams = {
        /// FileName of the SonarQube runner exe. 
        ToolsPath : string
        /// Key to identify the SonarQube project
        Key : string
        /// Name of the project
        Name : string
        /// Version number of the project
        Version : string
        /// Individual global settings for SonarQube
        Settings : string list
        /// Read settings from configuration file
        Config : string option
    }

    /// SonarQube default parameters - tries to locate MSBuild.SonarQube.exe in any subfolder.
    let SonarQubeDefaults = 
        { ToolsPath = Tools.findToolInSubPath "MSBuild.SonarQube.Runner.exe" (Directory.GetCurrentDirectory() @@ "tools" @@ "SonarQube")
          Key = null
          Name = null
          Version = "1.0"
          Settings = []
          Config = None }

    /// [omit]
    /// Execute the external msbuild runner of SonarQube. Parameters are given to the command line tool as required.
    let private SonarQubeCall (call: SonarQubeCall) (parameters : SonarQubeParams) =
      let sonarPath = parameters.ToolsPath 
      let setArgs = parameters.Settings |> List.fold (fun acc x -> acc + "/d:" + x + " ") ""

      let cfgArgs = 
        match parameters.Config with
        | Some(x) -> (" /s:"+x) 
        | None -> ""
      
      let args = 
        match call with
        | Begin -> "begin /k:\"" + parameters.Key + "\" /n:\"" + parameters.Name + "\" /v:\"" + parameters.Version + "\" " + setArgs + cfgArgs
        | End -> "end " + setArgs + cfgArgs

      let result =
        ExecProcess ((fun info ->
        { info with
            FileName = sonarPath
            Arguments = args }) >> Process.withFramework) System.TimeSpan.MaxValue
      if result <> 0 then failwithf "Error during sonar qube call %s" (call.ToString())

    /// This task to can be used to run the begin command of [Sonar Qube](http://sonarqube.org/) on a project.
    /// ## Parameters
    ///
    ///  - `setParams` - Function used to overwrite the SonarQube default parameters.
    ///
    /// ## Sample

    ///   open Fake.Testing
    ///
    ///   SonarQube.Begin (fun p ->
    ///    {p with
    ///      Key = "MyProject"
    ///      Name = "MainTool"
    ///      Version = "1.0 })
    ///
    let Begin setParams = 
        use __ = Trace.traceTask "SonarQube" "Begin"
        let parameters = setParams SonarQubeDefaults
        SonarQubeCall Begin parameters

    /// This task to can be used to run the end command of [Sonar Qube](http://sonarqube.org/) on a project.
    /// ## Parameters
    ///
    ///  - `setParams` - Function used to overwrite the SonarQube default parameters.
    ///
    /// ## Sample
    
    ///   open Fake.Testing
    ///
    ///   SonarQube.End None
    ///
    ///   SonarQube.End (fun p ->
    ///    {p with
    ///      Settings = ["sonar.login=login", "sonar.password=password"] })
    ///
    let End setParams = 
        use __ = Trace.traceTask "SonarQube" "End"
        let parameters = match setParams with
                         | Some setParams -> setParams SonarQubeDefaults
                         | None -> (fun p -> { p with Settings = [] }) SonarQubeDefaults
        SonarQubeCall End parameters
