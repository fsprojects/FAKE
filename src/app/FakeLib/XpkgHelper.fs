[<AutoOpen>]
/// Contains tasks to create packages in [Xamarin's xpkg format](http://components.xamarin.com/)
/// Using the xpkg/xamarin-component.exe tool.
module Fake.XpkgHelper

open System
open System.Text

/// Parameter type for xpkg tasks
type xpkgParams = 
    { ToolPath : string
      WorkingDir : string
      TimeOut : TimeSpan

      /// Name to identify the component, without version and extension.
      /// Must be composed of lowercase letters, numbers, and - or _.
      Package : string

      /// Version of the component
      Version : string

      /// Path where the generated .xam component package should be written to.
      OutputPath : string

      /// Human readable name of the component.
      /// Corresponds to the --name argument.
      Project : string

      /// A short description of the component (max 160 characters).
      /// Corresponds to the --summary argument.
      Summary : string

      /// Human readable name of the publisher/author of the component.
      /// Corresponds to the --publisher argument.
      Publisher : string

      /// A URL to the website of the publisher/author.
      /// Corresponds to the --publisher-url argument.
      PublisherUrl : string

      /// A URL to the website of the component.
      /// Corresponds to the --website argument.
      Website : string

      /// A URL to an online source code repository (GitHub etc.)
      /// Corresponds to the --srcurl argument.
      SourcesUrl : string

      /// A URL for any online documentation or support resources associated with the component.
      /// Corresponds to the --docs argument.
      DocsUrl : string

      /// Path to the Details.md file.
      /// Corresponds to the --details argument.
      Details : string

      /// Path to the License.md file.
      /// Corresponds to the --license argument.
      License : string

      /// Path to the GerringStarted.md file.
      /// Corresponds to the --getting-started argument.
      GettingStarted : string

      /// Path to an optional popover image (320x200).
      /// Corresponds to the --popover argument.
      Popover : string

      /// List of paths to icon files, whose names should end with _512x512.png and _128x128.png.
      /// The recommended sizes are 512x512 and 128x128.
      /// Corresponds to the --icon argument.
      Icons : string list

      /// List of framework id * path tuples, specifying the assemblies for each platform.
      /// Possible Framework ids include "android", "ios", "winphone-7.0", "winphone-7.1" and "mobile" for all in one.
      /// Corresponds to the --library argument.
      Libraries : (string * string) list

      /// List of description * path tuples, specifying sample solutions.
      /// Each description must contain at least two sentences. The first sentence will be used as title, the rest as summary.
      /// The sample solutions must not have any project references; reference the assemblies directly.
      /// Corresponds to the --sample argument.
      Samples : (string * string) list

      /// List of title * path tuples, specifying optional screenshots. The recommended size is 700x400.
      /// Corresponds to the --screenshot argument.
      Screenshots : (string * string) list }

/// Creates xpkg default parameters
let XpkgDefaults() = 
    { ToolPath = findToolInSubPath "xpkg.exe" (currentDirectory @@ "tools" @@ "xpkg")
      WorkingDir = "./"
      TimeOut = TimeSpan.FromMinutes 5.
      Package = null
      Version = 
          if not isLocalBuild then buildVersion
          else "0.1.0.0"
      OutputPath = "./xpkg"
      Project = null
      Summary = null
      Publisher = null
      PublisherUrl = null
      Website = null
      SourcesUrl = null
      DocsUrl = null
      Details = "Details.md"
      License = "License.md"
      GettingStarted = "GettingStarted.md"
      Popover = null
      Icons = []
      Libraries = []
      Samples = []
      Screenshots = [] }

let private getPackageFileName parameters = sprintf "%s-%s.xam" parameters.Package parameters.Version

/// Creates a new xpkg package based on the package file name
///
/// ## Sample
///
///     Target "PackageXamarinDistribution" (fun _ -> 
///          xpkgPack (fun p ->
///              {p with
///                  ToolPath = xpkgExecutable;
///                  Package = "Portable.Licensing";
///                  Version = assemblyFileVersion;
///                  OutputPath = publishDir
///                  Project = "Portable.Licensing"
///                  Summary = "Portable.Licensing is a cross platform licensing tool"
///                  Publisher = "Nauck IT KG"
///                  Website = "http://dev.nauck-it.de"
///                  Details = "./Xamarin/Details.md"
///                  License = "License.md"
///                  GettingStarted = "./Xamarin/GettingStarted.md"
///                  Icons = ["./Xamarin/Portable.Licensing_512x512.png"
///                           "./Xamarin/Portable.Licensing_128x128.png"]
///                  Libraries = ["mobile", "./Distribution/lib/Portable.Licensing.dll"]
///                  Samples = ["Android Sample.", "./Samples/Android/Android.Sample.sln"
///                             "iOS Sample.", "./Samples/iOS/iOS.Sample.sln"]
///              }
///          )
///      )
let xpkgPack setParams = 
    let parameters = XpkgDefaults() |> setParams
    let packageFileName = getPackageFileName parameters
    traceStartTask "xpkgPack" packageFileName
    let fullPath = parameters.OutputPath @@ packageFileName
    
    let commandLineBuilder = 
        new StringBuilder()
        |> append "create-manually"
        |> append (sprintf "\"%s\"" fullPath)
        |> appendQuotedIfNotNull parameters.Project "--name="
        |> appendQuotedIfNotNull parameters.Summary "--summary="
        |> appendQuotedIfNotNull parameters.Publisher "--publisher="
        |> appendQuotedIfNotNull parameters.PublisherUrl "--publisher-url="
        |> appendQuotedIfNotNull parameters.Website "--website="
        |> appendQuotedIfNotNull parameters.Details "--details="
        |> appendQuotedIfNotNull parameters.License "--license="
        |> appendQuotedIfNotNull parameters.GettingStarted "--getting-started="
        |> appendQuotedIfNotNull parameters.Popover "--popover="
        |> appendQuotedIfNotNull parameters.DocsUrl "--docs="
        |> appendQuotedIfNotNull parameters.SourcesUrl "--srcurl="
    parameters.Icons
    |> List.map (fun icon -> sprintf " --icon=\"%s\"" icon)
    |> List.iter (fun x -> commandLineBuilder.Append x |> ignore)
    parameters.Libraries
    |> List.map (fun (platform, library) -> sprintf " --library=\"%s\":\"%s\"" platform library)
    |> List.iter (fun x -> commandLineBuilder.Append x |> ignore)
    parameters.Samples
    |> List.map (fun (sample, solution) -> sprintf " --sample=\"%s\":\"%s\"" sample solution)
    |> List.iter (fun x -> commandLineBuilder.Append x |> ignore)
    parameters.Screenshots
    |> List.map (fun (title, path) -> sprintf " --screenshot=\"%s\":\"%s\"" title path)
    |> List.iter (fun x -> commandLineBuilder.Append x |> ignore)
    let args = commandLineBuilder.ToString()
    trace (parameters.ToolPath + " " + args)
    let result = 
        ExecProcess (fun info -> 
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- parameters.WorkingDir
            info.Arguments <- args) parameters.TimeOut
    if result = 0 then traceEndTask "xpkgPack" packageFileName
    else failwithf "Create xpkg package failed. Process finished with exit code %d." result

/// Validates a xpkg package based on the package file name
let xpkgValidate setParams = 
    let parameters = XpkgDefaults() |> setParams
    let packageFileName = getPackageFileName parameters
    traceStartTask "xpkgValidate" packageFileName
    let fullPath = parameters.OutputPath @@ packageFileName
    
    let commandLineBuilder = 
        new StringBuilder()
        |> append "validate"
        |> append (sprintf "\"%s\"" fullPath)
    
    let args = commandLineBuilder.ToString()
    trace (parameters.ToolPath + " " + args)
    let result = 
        ExecProcess (fun info -> 
            info.FileName <- parameters.ToolPath
            info.WorkingDirectory <- parameters.WorkingDir
            info.Arguments <- args) parameters.TimeOut
    if result = 0 then traceEndTask "xpkgValidate" packageFileName
    else failwithf "Validate xpkg package failed. Process finished with exit code %d." result
