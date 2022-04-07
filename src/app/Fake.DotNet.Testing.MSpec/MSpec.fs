namespace Fake.DotNet.Testing

/// Contains a task to run [machine.specifications](https://github.com/machine/machine.specifications) tests.
module MSpec =

    open Fake.Testing.Common
    open Fake.IO.FileSystemOperators
    open Fake.Core
    open System
    open System.IO
    open System.Text

    /// Parameter type to configure the MSpec runner.
    type MSpecParams =
        { /// The path to the mspec console runner. Use `mspec-clr4.exe` if you are on .NET 4.0 or above.
          ToolPath: string
          /// Output directory for html reports (optional).
          HtmlOutputDir: string
          /// Output file path for xml reports (optional).
          XmlOutputPath: string
          /// Working directory (optional)
          WorkingDir: string
          /// Can be used to run MSpec in silent mode.
          Silent: bool
          /// Tests with theses tags are ignored by MSpec
          ExcludeTags: string list
          /// Tests with theses tags are included by MSpec
          IncludeTags: string list
          /// A timeout for the test runner
          TimeOut: TimeSpan
          /// An error level setting to specify whether a failed test should break the build
          ErrorLevel: TestRunnerErrorLevel }

    let internal toolPath toolName =
        let toolPath =
            ProcessUtils.tryFindLocalTool
                "TOOL"
                toolName
                [ Directory.GetCurrentDirectory()
                  @@ "tools" @@ "MSpec" ]

        match toolPath with
        | Some path -> path
        | None -> toolName

    /// MSpec default parameters - tries to locate `mspec-clr4.exe` in any subfolder.
    let MSpecDefaults =
        { ToolPath = toolPath "mspec-clr4.exe"
          HtmlOutputDir = null
          XmlOutputPath = null
          WorkingDir = null
          Silent = false
          ExcludeTags = []
          IncludeTags = []
          TimeOut = TimeSpan.FromMinutes 5.
          ErrorLevel = Error }

    /// Builds the command line arguments from the given parameter record and the given assemblies.
    let buildArgs (parameters: MSpecParams) (assemblies: string seq) =
        let html, htmlText =
            if String.isNotNullOrEmpty parameters.HtmlOutputDir then
                true,
                sprintf "--html\" \"%s"
                <| parameters.HtmlOutputDir.TrimEnd Path.DirectorySeparatorChar
            else
                false, ""

        let xml, xmlText =
            if String.isNotNullOrEmpty parameters.XmlOutputPath then
                true,
                sprintf "--xml\" \"%s"
                <| parameters.XmlOutputPath.TrimEnd Path.DirectorySeparatorChar
            else
                false, ""

        let includes = parameters.IncludeTags |> String.separated ","
        let excludes = parameters.ExcludeTags |> String.separated ","

        StringBuilder()
        |> StringBuilder.appendIfTrue (BuildServer.buildServer = BuildServer.TeamCity) "--teamcity"
        |> StringBuilder.appendIfTrue parameters.Silent "-s"
        |> StringBuilder.appendIfTrue html "-t"
        |> StringBuilder.appendIfTrue html htmlText
        |> StringBuilder.appendIfTrue xml "-t"
        |> StringBuilder.appendIfTrue xml xmlText
        |> StringBuilder.appendIfTrue (String.isNotNullOrEmpty excludes) (sprintf "-x\" \"%s" excludes)
        |> StringBuilder.appendIfTrue (String.isNotNullOrEmpty includes) (sprintf "-i\" \"%s" includes)
        |> StringBuilder.appendFileNamesIfNotNull assemblies
        |> StringBuilder.toText

    /// This task to can be used to run [machine.specifications](https://github.com/machine/machine.specifications) on test libraries.
    /// ## Parameters
    ///  - `setParams` - Function used to overwrite the MSpec default parameters.
    ///  - `assemblies` - The file names of the test assemblies.
    ///
    /// ## Sample
    ///     !! (testDir @@ "Test.*.dll")
    ///       |> MSpec (fun p -> {p with ExcludeTags = ["HTTP"]; HtmlOutputDir = reportDir})
    ///
    /// ## Hint
    ///
    /// XmlOutputPath expects a full file path whereas the HtmlOutputDir expects a directory name
    let exec setParams assemblies =
        let details = String.separated ", " assemblies
        use __ = Trace.traceTask "MSpec" details
        let parameters = setParams MSpecDefaults
        let args = buildArgs parameters assemblies
        Trace.trace (parameters.ToolPath + " " + args)

        let processResult =
            CreateProcess.fromRawCommandLine parameters.ToolPath args
            |> CreateProcess.withWorkingDirectory parameters.WorkingDir
            |> CreateProcess.withTimeout parameters.TimeOut
            |> CreateProcess.withFramework
            |> Proc.run

        if processResult.ExitCode <> 0 then
            sprintf "MSpec test failed on %s." details
            |> match parameters.ErrorLevel with
               | Error
               | FailOnFirstError -> failwith
               | DontFailBuild -> Trace.traceImportant

        __.MarkSuccess()
