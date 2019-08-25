/// Contains a task to run the msbuild runner of [SonarQube analyzer](http://sonarqube.org).
module Fake.Testing.SonarQube

    open System.IO
    open Fake.Core
    open Fake.IO.Globbing
    open Fake.IO.FileSystemOperators

    /// [omit]
    /// The supported commands of SonarQube. It is called with Begin before compilation, and End after compilation.
    type internal SonarQubeCall = Begin | End

    /// Parameter type to configure the SonarQube runner.
    type SonarQubeParams = {
        /// FileName of the SonarQube runner exe. 
        ToolsPath : string
        /// Organization which owns the SonarQube project
        Organization : string option
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
    let internal SonarQubeDefaults = 
        { ToolsPath = Tools.findToolInSubPath "MSBuild.SonarQube.Runner.exe" (Directory.GetCurrentDirectory() @@ "tools" @@ "SonarQube")
          Organization = None
          Key = null
          Name = null
          Version = "1.0"
          Settings = []
          Config = None }

    /// [omit]
    /// Execute the external msbuild runner of SonarQube. Parameters are given to the command line tool as required.
    let internal getSonarQubeCallParams (call: SonarQubeCall) (parameters : SonarQubeParams) =
        let beginInitialArguments =
            Arguments.Empty
            |> Arguments.appendRaw "begin"
            |> Arguments.appendRawEscapedNotEmpty "/k:" parameters.Key
            |> Arguments.appendRawEscapedNotEmpty "/n:" parameters.Name
            |> Arguments.appendRawEscapedNotEmpty "/v:" parameters.Version
            |> Arguments.appendRawEscapedOption "/o:" parameters.Organization
            |> Arguments.appendRawEscapedOption "/s:" parameters.Config
           
        let beginCall =
            parameters.Settings
            |> List.fold (fun arguments x ->  arguments |> Arguments.appendRawEscaped "/d:" x) beginInitialArguments
           
        let endInitialArguments =
            Arguments.Empty
            |> Arguments.appendRaw "end"
            |> Arguments.appendRawEscapedOption "/s:" parameters.Config
        let endCall =   
            parameters.Settings
            |> List.fold (fun arguments x ->  arguments |> Arguments.appendRawEscaped "/d:" x) endInitialArguments
        
        match call with
        | Begin -> beginCall
        | End -> endCall

    let private sonarQubeCall (call: SonarQubeCall) (parameters : SonarQubeParams) =
        let sonarPath = parameters.ToolsPath 
        let result =
            getSonarQubeCallParams call parameters
            |> Arguments.toStartInfo
            |> CreateProcess.fromRawCommandLine sonarPath
            |> CreateProcess.withFramework
            |> Proc.run
        if result.ExitCode <> 0 then failwithf "Error during sonar qube call %s" (call.ToString())

    /// This task to can be used to run the begin command of [Sonar Qube](http://sonarqube.org/) on a project.
    /// ## Parameters
    ///
    ///  - `setParams` - Function used to overwrite the SonarQube default parameters.
    ///
    /// ## Sample

    ///   open Fake.Testing
    ///
    ///   SonarQube.start (fun p ->
    ///    {p with
    ///      Key = "MyProject"
    ///      Name = "MainTool"
    ///      Version = "1.0 })
    ///
    let start setParams = 
        use __ = Trace.traceTask "SonarQube" "Begin"
        let parameters = setParams SonarQubeDefaults
        sonarQubeCall Begin parameters
        __.MarkSuccess()

    /// This task to can be used to run the end command of [Sonar Qube](http://sonarqube.org/) on a project.
    /// ## Parameters
    ///
    ///  - `setParams` - Function used to overwrite the SonarQube default parameters.
    ///
    /// ## Sample
    
    ///   open Fake.Testing
    ///
    ///   SonarQube.finish None
    ///
    ///   SonarQube.finish (Some (fun p ->
    ///    {p with
    ///      Settings = ["sonar.login=login"; "sonar.password=password"] }))
    ///
    let finish setParams = 
        use __ = Trace.traceTask "SonarQube" "End"
        let parameters = match setParams with
                         | Some setParams -> setParams SonarQubeDefaults
                         | None -> (fun p -> { p with Settings = [] }) SonarQubeDefaults
        sonarQubeCall End parameters
        __.MarkSuccess()
