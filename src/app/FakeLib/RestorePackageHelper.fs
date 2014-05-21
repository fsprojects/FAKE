[<AutoOpen>]
/// Contains tasks which allow to restore NuGet packages from a NuGet package feed like [nuget.org](http://www.nuget.org).
/// There is also a tutorial about [nuget package restore](../nuget.html) available.
module Fake.RestorePackageHelper

open System
open System.IO

/// Looks for a tool in all subfolders - returns the tool file name.
let findNuget defaultPath = 
    let tools = !! ("./**/" @@ "nuget.exe")
    if Seq.isEmpty tools then 
        let tools = !! ("./**/" @@ "NuGet.exe")
        if Seq.isEmpty tools then defaultPath @@ "NuGet.exe" else Seq.head tools
    else Seq.head tools

/// RestorePackages parameter path
type RestorePackageParams =
    { ToolPath: string
      Sources: string list
      TimeOut: TimeSpan
      /// Specifies how often nuget should try to restore the packages - default is 5
      Retries: int
      OutputPath: string}

/// RestorePackage defaults parameters
let RestorePackageDefaults =
    { ToolPath = findNuget (currentDirectory @@ "tools" @@ "NuGet")
      Sources = []
      TimeOut = TimeSpan.FromMinutes 5.
      Retries = 5
      OutputPath = "./packages" }

/// RestorePackages parameter path for single packages
type RestoreSinglePackageParams = 
    { ToolPath: string
      Sources: string list
      TimeOut: TimeSpan
      OutputPath: string
      Version: Version option
      ExcludeVersion: bool
      /// Specifies how often nuget should try to restore the packages - default is 5
      Retries: int
      IncludePreRelease: bool }

/// RestoreSinglePackageParams defaults parameters  
let RestoreSinglePackageDefaults =
    { ToolPath = RestorePackageDefaults.ToolPath
      Sources = []
      TimeOut = TimeSpan.FromMinutes 2.
      OutputPath = RestorePackageDefaults.OutputPath
      Version = None
      ExcludeVersion = false
      Retries = 5
      IncludePreRelease = false }

/// [omit]
let runNuGet toolPath timeOut args failWith =
    if 0 <> ExecProcess (fun info ->  
        info.FileName <- toolPath |> FullName
        info.Arguments <- args) timeOut
    then
        failWith()

/// [omit]
let rec runNuGetTrial retries toolPath timeOut args failWith =
    let f() = runNuGet toolPath timeOut args failWith
    TaskRunnerHelper.runWithRetries f retries
        
/// [omit]
let buildNuGetArgs setParams packageId = 
    let parameters = RestoreSinglePackageDefaults |> setParams
    let sources =
        parameters.Sources
        |> List.map (fun source -> " \"-Source\" \"" + source + "\"")
        |> separated ""

    let args = " \"install\" \"" + packageId + "\" \"-OutputDirectory\" \"" + (parameters.OutputPath |> FullName) + "\"" + sources

    match parameters.ExcludeVersion, parameters.IncludePreRelease, parameters.Version with
    | (true, false, Some(v))  -> args + " \"-ExcludeVersion\" \"-Version\" \"" + v.ToString() + "\""
    | (true, false, None)     -> args + " \"-ExcludeVersion\""
    | (false, _, Some(v))     -> args + " \"-Version\" \"" + v.ToString() + "\""
    | (false, false, None)    -> args
    | (false, true, _)        -> args + " \"-PreRelease\""
    | (true, true, _)         -> args + " \"-ExcludeVersion\" \"-PreRelease\""

/// Restores the given package from NuGet
let RestorePackageId setParams packageId = 
    traceStartTask "RestorePackageId" packageId
    let parameters = RestoreSinglePackageDefaults |> setParams

    let args = buildNuGetArgs setParams packageId
    runNuGetTrial parameters.Retries parameters.ToolPath parameters.TimeOut args (fun () -> failwithf "Package installation of package %s failed." packageId)
  
    traceEndTask "RestorePackageId" packageId

/// Restores the given package from NuGet
let RestorePackage setParams package = 
    traceStartTask "RestorePackage" package
    let (parameters:RestorePackageParams) = RestorePackageDefaults |> setParams

    let sources =
        parameters.Sources
        |> List.map (fun source -> " \"-Source\" \"" + source + "\"")
        |> separated ""

    let args =
        " \"install\" \"" + (package |> FullName) + "\"" +
        " \"-OutputDirectory\" \"" + (parameters.OutputPath |> FullName) + "\"" + sources

    runNuGetTrial parameters.Retries parameters.ToolPath parameters.TimeOut args (fun () -> failwithf "Package installation of %s generation failed." package)
                    
    traceEndTask "RestorePackage" package

/// Restores all packages from NuGet to the default directories
let RestorePackages() = 
    !! "./**/packages.config"
    |> Seq.iter (RestorePackage id)