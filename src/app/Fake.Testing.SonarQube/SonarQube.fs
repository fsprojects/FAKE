namespace Fake.Testing

/// <summary>
/// Contains a task to run the <a href="http://sonarqube.org">SonarQube</a> static code analyzer.
/// It uses the <a href="https://docs.sonarqube.org/latest/analysis/scan/sonarscanner-for-msbuild/">
/// SonarScanner for MSBuild</a>
/// </summary>
module SonarQube =

    open System.IO
    open Fake.Core
    open Fake.DotNet

    /// The supported commands of SonarQube. It is called with Begin before compilation, and End after compilation.
    type internal SonarQubeCall =
        | Begin
        | End

    /// <summary>
    /// Parameter type to configure the SonarQube runner.
    /// </summary>
    type SonarQubeParams =
        { /// The directory where the SonarQube scanner process will be started.
          WorkingDirectory: string
          /// Tool type
          ToolType: ToolType
          /// FileName of the SonarQube runner exe.
          ToolsPath: string
          /// Organization which owns the SonarQube project
          Organization: string option
          /// Key to identify the SonarQube project
          Key: string
          /// Name of the project
          Name: string
          /// Version number of the project
          Version: string
          /// Individual global settings for SonarQube
          Settings: string list
          /// Read settings from configuration file
          Config: string option }

    /// SonarQube default parameters
    let internal SonarQubeDefaults =
        { WorkingDirectory = Directory.GetCurrentDirectory()
          ToolType = ToolType.Create()
          ToolsPath = ProcessUtils.findLocalTool "TOOL" "MSBuild.SonarQube.Runner.exe" [ "." ]
          Organization = None
          Key = null
          Name = null
          Version = "1.0"
          Settings = []
          Config = None }

    /// Execute the external msbuild runner of SonarQube. Parameters are given to the command line tool as required.
    let internal getSonarQubeCallParams (call: SonarQubeCall) (parameters: SonarQubeParams) =
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
            |> List.fold (fun arguments x -> arguments |> Arguments.appendRawEscaped "/d:" x) beginInitialArguments

        let endInitialArguments =
            Arguments.Empty
            |> Arguments.appendRaw "end"
            |> Arguments.appendRawEscapedOption "/s:" parameters.Config

        let endCall =
            parameters.Settings
            |> List.fold (fun arguments x -> arguments |> Arguments.appendRawEscaped "/d:" x) endInitialArguments

        match call with
        | Begin -> beginCall
        | End -> endCall

    let private sonarQubeCall (call: SonarQubeCall) (parameters: SonarQubeParams) =
        let args = getSonarQubeCallParams call parameters

        CreateProcess.fromCommand (RawCommand(parameters.ToolsPath, args))
        |> CreateProcess.withToolType (parameters.ToolType.WithDefaultToolCommandName "sonarscanner")
        |> CreateProcess.withWorkingDirectory parameters.WorkingDirectory
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore

    /// <summary>
    /// This task can be used to run the begin command of <a href="http://sonarqube.org/">Sonar Qube</a> on a project.
    /// </summary>
    ///
    /// <param name="setParams">Function used to overwrite the SonarQube default parameters.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// open Fake.Testing
    ///
    ///   SonarQube.start (fun p ->
    ///     { p with
    ///           Key = "MyProject"
    ///           Name = "MainTool"
    ///           Version = "1.0 })
    /// </code>
    /// </example> 
    let start setParams =
        use __ = Trace.traceTask "SonarQube" "Begin"
        let parameters = setParams SonarQubeDefaults
        sonarQubeCall Begin parameters
        __.MarkSuccess()

    /// <summary>
    /// This task can be used to run the end command of <a href="http://sonarqube.org/">Sonar Qube</a> on a project.
    /// </summary>
    ///
    /// <param name="setParams">Function used to overwrite the SonarQube default parameters.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// open Fake.Testing
    ///
    ///   SonarQube.finish None
    ///
    ///   SonarQube.finish (Some (fun p ->
    ///    { p with
    ///         Settings = ["sonar.login=login"; "sonar.password=password"] }))
    /// </code>
    /// </example>
    let finish setParams =
        use __ = Trace.traceTask "SonarQube" "End"

        let parameters =
            match setParams with
            | Some setParams -> setParams SonarQubeDefaults
            | None -> (fun p -> { p with Settings = [] }) SonarQubeDefaults

        sonarQubeCall End parameters
        __.MarkSuccess()

    /// <summary>
    /// This task can be used to execute some code between
    /// the `begin` and `end` [Sonar Qube](http://sonarqube.org/) on a project.
    /// </summary>
    ///
    /// <param name="setParams">Function used to overwrite the SonarQube default parameters.</param>
    /// <param name="doBetween">Function executed between <c>begin</c> and <c>end</c>
    /// <a href="http://sonarqube.org/">Sonar Qube</a> commands.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// open Fake.Testing
    ///
    /// Target.create "StaticAnalysis" (fun _ ->
    ///   let setParams p =
    ///     { p with Key = "ProjectKey"
    ///              Name = "ProjectName"
    ///              Version = "3.2.1"
    ///              Settings =
    ///                 [ "sonar.host.url=" + SONAR_HOST_URL
    ///                   "sonar.login=" + SONAR_TOKEN
    ///                   "sonar.cs.opencover.reportsPaths=opencovercoverage.xml"
    ///                 ]
    ///              // choose what you need
    ///              // https://fake.build/guide/dotnet-cli.html#SDK-tools-local-global-clireference
    ///              ToolType = ToolType.CreateGlobalTool() // Start as dotnet global tool (`sonarscanner`)
    ///              ToolType = ToolType.CreateLocalTool()  // Start as dotnet local tool (`dotnet sonarscanner`)
    ///     }
    ///   SonarQube.scan setParams (fun () ->
    ///         DotNet.build id
    ///         DotNet.test id
    ///   )
    /// )
    /// </code>
    /// </example>
    let scan setParams doBetween =
        start setParams
        doBetween ()
        finish (Some setParams)
