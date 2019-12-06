/// Contains types and utility functions related to creating [Squirrel](https://github.com/Squirrel/Squirrel.Windows) installer.
[<RequireQualifiedAccess>]
module Fake.Installer.Squirrel

open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing
open System
open System.IO
open System.Text

/// The [Squirrel](https://github.com/Squirrel/Squirrel.Windows) Console Parameters type.
///
/// For reference, see: [Squirrel Command Line Options](https://github.com/Squirrel/Squirrel.Windows/blob/master/docs/advanced-releasify.md)
type ReleasifyParams = {
    /// The output directory for the generated installer
    ReleaseDir : string

    /// The working directory.
    WorkingDir : string option

    /// The full path to an optional setup.exe template
    BootstrapperExe : string option

    /// The full path to an optional animated gif to be displayed during installation
    LoadingGif : string option

    /// The full path to an optional icon, which will be used for the generated installer.
    SetupIcon : string option

    /// Don't generate delta packages to save time
    NoDelta : bool

    /// Do not create an MSI file
    NoMsi : bool

    /// Mark the MSI as 64-bit, which is useful in Enterprise deployment scenarios
    MsiWin64 : bool

    /// The path to Squirrel: `squirrel.exe`
    ToolPath : string

    /// Maximum time to allow Squirrel to run before being killed.
    TimeOut : TimeSpan

    /// Sign the installer with signtool.exe
    SignExecutable : bool option

    /// The code signing certificate to be used for signing
    SigningKeyFile : string option

    /// The secret key for the code signing certificate
    SigningSecret : string option

    /// Set the required .NET framework version, e.g. net461
    FrameworkVersion : string option

    /// Add other arguments, in case Squirrel adds new arguments in future
    AdditionalArguments : string option
}

let internal defaultParams = lazy(
    let toolname = "Squirrel.exe"
    { ReleaseDir = ""
      WorkingDir = None
      BootstrapperExe = None
      LoadingGif = None
      SetupIcon = None
      FrameworkVersion = None
      NoDelta = false
      NoMsi = false
      MsiWin64 = false
      ToolPath = Tools.findToolInSubPath toolname ( Directory.GetCurrentDirectory() </> "tools" </> "Squirrel")
      TimeOut = TimeSpan.FromMinutes 10.
      SignExecutable = None
      SigningKeyFile = None
      SigningSecret = None
      AdditionalArguments = None })

let private createSigningArgs (parameters : ReleasifyParams) =
    new StringBuilder()
    |> StringBuilder.appendWithoutQuotes "--signWithParams=\""
    |> StringBuilder.appendWithoutQuotes "/a"
    |> StringBuilder.appendIfSome parameters.SigningKeyFile (sprintf "/f %s")
    |> StringBuilder.appendIfSome parameters.SigningSecret  (sprintf "/p %s")
    |> StringBuilder.appendWithoutQuotes "\""
    |> StringBuilder.toText

let internal valueIfTrue (arg : bool) (value : string) =
    match arg with
    | true -> value
    | false -> ""

let internal argIfSome (arg : string option) (prefix : string) =
    match arg with
    | Some v -> prefix + v
    | None -> ""

let internal argIfNotEmpty (arg : string) (prefix : string) =
    match arg with
    | "" -> ""
    | _ -> prefix + arg

let internal buildSquirrelArgs parameters nugetPackage =
    let args = [
        argIfSome parameters.AdditionalArguments ""
        argIfNotEmpty nugetPackage "--releasify="
        argIfNotEmpty parameters.ReleaseDir "--releaseDir="
        argIfSome parameters.FrameworkVersion "--framework-version="
        argIfSome parameters.LoadingGif "--loadingGif="
        argIfSome parameters.SetupIcon "--setupIcon="
        valueIfTrue parameters.NoDelta "--no-delta"
        valueIfTrue parameters.NoMsi "--no-msi"
        valueIfTrue parameters.MsiWin64 "--msi-win64"
        argIfSome parameters.BootstrapperExe "--bootstrapperExe="
        (match parameters.SignExecutable with | Some true -> createSigningArgs parameters | _ -> "")
        ]

    args |> List.filter (fun arg -> arg <> "")

module internal ResultHandling =
    let (|OK|Failure|) = function
        | 0 -> OK
        | x -> Failure x

    let buildErrorMessage = function
        | OK -> None
        | Failure errorCode ->
            Some (sprintf "Squirrel reported an error (Error Code %d)" errorCode)

    let failBuildIfSquirrelReportedError =
        buildErrorMessage
        >> Option.iter failwith

/// Creates a Squirrel installer for given NuGet package
/// Will fail if Squirrel terminates with non-zero exit code.
///
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default `SquirrelParams` value.
///  - `nugetPackage` - The package to create an installer for
///
/// ## Sample usage
///
///     Target.create "CreatePackage" (fun _ ->
///         Squirrel.releasify "./my.nupkg" (fun p -> { p with ReleaseDir = "./squirrel_release")
///     )
///
/// ## Defaults for setParams
///
/// - `ReleaseDir` - `""`
/// - `WorkingDir` - `None`
/// - `BootstrapperExe` - `None`
/// - `LoadingGif` - `None`
/// - `SetupIcon` - `None`
/// - `NoDelta` - `false`
/// - `NoMsi` - `false`
/// - `MsiWin64` - `false`
/// - `ToolPath` - The `squirrel.exe` path if it exists in a subdirectory of the current directory.
/// - `TimeOut` - 10 minutes
/// - `SignExecutable` - `None`
/// - `SigningKeyFile` - `None`
/// - `SigningSecret` - `None`
/// - `FrameworkVersion` - `None`
/// - `AdditionalArguments` - `None` A string with additional arguments that will be added to the command line
let releasify (nugetPackage: string) (setParams: ReleasifyParams -> ReleasifyParams): unit =
    use __ = Trace.traceTask "Squirrel" nugetPackage
    let parameters = defaultParams.Value |> setParams
    let args = buildSquirrelArgs parameters nugetPackage
    Trace.tracefn "%O" args

    let workingDir = defaultArg parameters.WorkingDir "."

    let result = Command.RawCommand(parameters.ToolPath, Arguments.OfArgs args)
                    |> CreateProcess.fromCommand
                    |> CreateProcess.withWorkingDirectory workingDir
                    |> CreateProcess.withTimeout parameters.TimeOut
                    |> Proc.run

    ResultHandling.failBuildIfSquirrelReportedError result.ExitCode

    __.MarkSuccess()
