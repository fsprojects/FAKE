namespace Fake.Installer

/// <summary>
/// Contains types and utility functions related to creating <a href="https://github.com/Squirrel/Squirrel.Windows">
/// Squirrel</a> installer.
/// </summary>
[<RequireQualifiedAccess>]
module Squirrel =

    open Fake.Core
    open Fake.IO
    open System
    open System.IO
    open System.Text

    /// <summary>
    /// The <a href="https://github.com/Squirrel/Squirrel.Windows">Squirrel</a> Console Parameters type.
    /// For reference, see: <a href="https://github.com/Squirrel/Squirrel.Windows/blob/master/docs/using/squirrel-command-line.md">
    /// Squirrel Command Line Options</a>
    /// </summary>
    type ReleasifyParams =
        {
          /// Path to a release directory to use with Releasify
          ReleaseDir: string

          /// The working directory.
          WorkingDir: string option

          /// The full path to an optional setup.exe template
          BootstrapperExe: string option

          /// The full path to an optional animated gif to be displayed during installation
          LoadingGif: string option

          /// The full path to an optional icon, which will be used for the generated installer.
          SetupIcon: string option

          /// Don't generate delta packages to save time
          NoDelta: bool

          /// Do not create an MSI file
          NoMsi: bool

          /// Mark the MSI as 64-bit, which is useful in Enterprise deployment scenarios
          MsiWin64: bool

          /// The path to Squirrel: `squirrel.exe`
          ToolPath: string

          /// Maximum time to allow Squirrel to run before being killed.
          TimeOut: TimeSpan

          /// Sign the installer with <c>signtool.exe</c>
          SignExecutable: bool option

          /// The code signing certificate to be used for signing
          SigningKeyFile: string option

          /// The secret key for the code signing certificate
          SigningSecret: string option

          /// Set the required .NET framework version, e.g. net461
          FrameworkVersion: string option

          /// Add other arguments, in case Squirrel adds new arguments in future
          AdditionalArguments: string option }

    let internal toolPath toolName =
        let toolPath =
            ProcessUtils.tryFindLocalTool "TOOL" toolName [ Directory.GetCurrentDirectory() ]

        match toolPath with
        | Some path -> path
        | None -> toolName

    let internal defaultParams =
        lazy
            (let toolName = "Squirrel.exe"

             { ReleaseDir = ""
               WorkingDir = None
               BootstrapperExe = None
               LoadingGif = None
               SetupIcon = None
               FrameworkVersion = None
               NoDelta = false
               NoMsi = false
               MsiWin64 = false
               ToolPath = toolPath toolName
               TimeOut = TimeSpan.FromMinutes 10.
               SignExecutable = None
               SigningKeyFile = None
               SigningSecret = None
               AdditionalArguments = None })

    /// The signing arguments for Squirrel
    let private createSigningArgs (parameters: ReleasifyParams) =
        StringBuilder()
        |> StringBuilder.appendWithoutQuotes "--signWithParams=\""
        |> StringBuilder.appendWithoutQuotes "/a"
        |> StringBuilder.appendIfSome parameters.SigningKeyFile (sprintf "/f %s")
        |> StringBuilder.appendIfSome parameters.SigningSecret (sprintf "/p %s")
        |> StringBuilder.appendWithoutQuotes "\""
        |> StringBuilder.toText

    let internal valueIfTrue (arg: bool) (value: string) =
        match arg with
        | true -> value
        | false -> ""

    let internal argIfSome (arg: string option) (prefix: string) =
        match arg with
        | Some v -> prefix + v
        | None -> ""

    let internal argIfNotEmpty (arg: string) (prefix: string) =
        match arg with
        | "" -> ""
        | _ -> prefix + arg

    /// Build the Squirrel arguments
    let internal buildSquirrelArgs parameters nugetPackage =
        let args =
            [ argIfSome parameters.AdditionalArguments ""
              argIfNotEmpty nugetPackage "--releasify="
              argIfNotEmpty parameters.ReleaseDir "--releaseDir="
              argIfSome parameters.FrameworkVersion "--framework-version="
              argIfSome parameters.LoadingGif "--loadingGif="
              argIfSome parameters.SetupIcon "--setupIcon="
              valueIfTrue parameters.NoDelta "--no-delta"
              valueIfTrue parameters.NoMsi "--no-msi"
              valueIfTrue parameters.MsiWin64 "--msi-win64"
              argIfSome parameters.BootstrapperExe "--bootstrapperExe="
              (match parameters.SignExecutable with
               | Some true -> createSigningArgs parameters
               | _ -> "") ]

        args |> List.filter (fun arg -> arg <> "")

    module internal ResultHandling =
        let (|OK|Failure|) =
            function
            | 0 -> OK
            | x -> Failure x

        let buildErrorMessage =
            function
            | OK -> None
            | Failure errorCode -> Some $"Squirrel reported an error (Error Code {errorCode})"

        let failBuildIfSquirrelReportedError = buildErrorMessage >> Option.iter failwith

    /// <summary>
    /// Creates a Squirrel installer for given NuGet package. Will fail if Squirrel terminates with non-zero exit code.
    /// <br/><br/>Defaults for setParams:
    /// <list type="number">
    /// <item>
    /// <c>ReleaseDir</c> - <c>""</c>
    /// </item>
    /// <item>
    /// <c>WorkingDir</c> - <c>None</c>
    /// </item>
    /// <item>
    /// <c>BootstrapperExe</c> - <c>None</c>
    /// </item>
    /// <item>
    /// <c>LoadingGif</c> - <c>None</c>
    /// </item>
    /// <item>
    /// <c>SetupIcon</c> - <c>None</c>
    /// </item>
    /// <item>
    /// <c>NoDelta</c> - <c>false</c>
    /// </item>
    /// <item>
    /// <c>NoMsi</c> - <c>false</c>
    /// </item>
    /// <item>
    /// <c>MsiWin64</c> - <c>false</c>
    /// </item>
    /// <item>
    /// <c>ToolPath</c> - The <c>squirrel.exe</c> path if it exists in a subdirectory of the current directory.
    /// </item>
    /// <item>
    /// <c>TimeOut</c> - <c>0</c> minutes
    /// </item>
    /// <item>
    /// <c>SignExecutable</c> - <c>None</c>
    /// </item>
    /// <item>
    /// <c>SigningKeyFile</c> - <c>None</c>
    /// </item>
    /// <item>
    /// <c>SigningSecret</c> - <c>None</c>
    /// </item>
    /// <item>
    /// <c>FrameworkVersion</c> - <c>None</c>
    /// </item>
    /// <item>
    /// <c>AdditionalArguments</c> - <c>None</c> A string with additional arguments that will be added to the command line
    /// </item>
    /// </list>
    /// </summary>
    ///
    /// <param name="nugetPackage">The package to create an installer for</param>
    /// <param name="setParams">Function used to manipulate the default <c>SquirrelParams</c> value.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// Target.create "CreatePackage" (fun _ ->
    ///         Squirrel.releasify "./my.nupkg" (fun p -> { p with ReleaseDir = "./squirrel_release")
    ///     )
    /// </code>
    /// </example>
    let releasify (nugetPackage: string) (setParams: ReleasifyParams -> ReleasifyParams) : unit =
        use __ = Trace.traceTask "Squirrel" nugetPackage
        let parameters = defaultParams.Value |> setParams
        let args = buildSquirrelArgs parameters nugetPackage
        Trace.tracefn $"{args}"

        let workingDir = defaultArg parameters.WorkingDir "."

        let result =
            Command.RawCommand(parameters.ToolPath, Arguments.OfArgs args)
            |> CreateProcess.fromCommand
            |> CreateProcess.withWorkingDirectory workingDir
            |> CreateProcess.withTimeout parameters.TimeOut
            |> Proc.run

        ResultHandling.failBuildIfSquirrelReportedError result.ExitCode

        __.MarkSuccess()
