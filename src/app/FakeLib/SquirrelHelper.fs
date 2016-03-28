/// Contains types and utility functions related to creating [Squirrel](https://github.com/Squirrel/Squirrel.Windows) installer.
module Fake.Squirrel

open Fake
open System
open System.Text

/// The [Squirrel](https://github.com/Squirrel/Squirrel.Windows) Console Parameters type.
/// FAKE will use [SquirrelDefaults](fake-squirrel.html) for values not provided.
///
/// For reference, see: [Squirrel Command Line Options](https://github.com/Squirrel/Squirrel.Windows/blob/master/docs/advanced-releasify.md)
type SquirrelParams =
    {
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

        /// Do not create an MSI file
        NoMsi : bool

        /// The path to Squirrel: `squirrel.exe`
        ToolPath : string

        /// Maximum time to allow Squirrel to run before being killed.
        TimeOut : TimeSpan

        /// Sign the installer with signtool.exe
        SignExecutable : bool option

        /// The code signing certificate to be used for signing
        SigningKeyFile : string option

        /// The secret key for the code signing certificate
        SigningSecret : string option }

/// The Squirrel default parameters.
///
/// ## Defaults
///
/// - `ReleaseDir` - `""`
/// - `WorkingDir` - `None`
/// - `BootstrapperExe` - `None`
/// - `LoadingGif` - `None`
/// - `SetupIcon` - `None`
/// - `NoMsi` - `false`
/// - `ToolPath` - The `squirrel.exe` path if it exists in a subdirectory of the current directory.
/// - `TimeOut` - 10 minutes
/// - `SignExecutable` - `None`
/// - `SigningKeyFile` - `None`
/// - `SigningSecret` - `None`
let SquirrelDefaults =
    let toolname = "Squirrel.exe"
    {
        ReleaseDir = ""
        WorkingDir = None
        BootstrapperExe = None
        LoadingGif = None
        SetupIcon = None
        NoMsi = false
        ToolPath = findToolInSubPath toolname (currentDirectory @@ "tools" @@ "Squirrel")
        TimeOut = TimeSpan.FromMinutes 10.
        SignExecutable = None
        SigningKeyFile = None
        SigningSecret = None }

let private createSigningArgs (parameters : SquirrelParams) =
    new StringBuilder()
    |> appendWithoutQuotes "--signWithParams=\""
    |> appendWithoutQuotes "/a"
    |> appendIfSome parameters.SigningKeyFile (sprintf "/f %s")
    |> appendIfSome parameters.SigningSecret  (sprintf "/p %s")
    |> appendWithoutQuotes "\""
    |> toText

let internal buildSquirrelArgs parameters nugetPackage =
    new StringBuilder()
    |> appendIfNotNullOrEmpty nugetPackage "--releasify="
    |> appendIfNotNullOrEmpty parameters.ReleaseDir "--releaseDir="
    |> appendIfSome parameters.LoadingGif (sprintf "\"--loadingGif=%s\"")
    |> appendIfSome parameters.SetupIcon (sprintf "\"--setupIcon=%s\"")
    |> appendIfTrue parameters.NoMsi "--no-msi"
    |> appendIfSome parameters.BootstrapperExe (sprintf "\"--bootstrapperExe=%s\"")
    |> appendIfSome parameters.SignExecutable (fun s -> createSigningArgs parameters)
    |> toText

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
///     Target "CreatePackage" (fun _ ->
///         SquirrelPack (fun p -> { p with WorkingDir = Some "./tmp" }) "./my.nupkg"
///     )
let SquirrelPack setParams nugetPackage =
    traceStartTask "Squirrel" ""
    let parameters = SquirrelDefaults |> setParams
    let args = buildSquirrelArgs parameters nugetPackage
    trace args

    let result =
        ExecProcess (fun info ->
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- defaultArg parameters.WorkingDir "."
            info.Arguments <- args) parameters.TimeOut

    ResultHandling.failBuildIfSquirrelReportedError result

    traceEndTask "Squirrel" ""
