[<AutoOpen>]
/// Contains tasks which allow to restore NuGet packages from a NuGet package feed like [nuget.org](http://www.nuget.org).
/// There is also a tutorial about [nuget package restore](../nuget.html) available.
module Fake.RestorePackageHelper

open System

/// Looks for NuGet.exe in [1] the specified defaultPath, [2] a list of standard tool folders, [3] any subfolder in the current directory, [4] the PATH - returns the first path where NuGet.exe was found.
let findNuget defaultPath = 
    try
        let priorityList = 
            [defaultPath
             currentDirectory @@ "tools" @@ "NuGet"
             currentDirectory @@ ".nuget"             
             currentDirectory @@ "packages" @@ "NuGet.Commandline" @@ "tools"
             currentDirectory @@ "packages" @@ "Nuget.Commandline" @@ "tools"]
            
        let exeNames = ["nuget.exe"; "NuGet.exe"; "Nuget.exe"]

        let findInFolders folders =
            seq { for path in folders do
                    for name in exeNames do
                      let fi = fileInfo(path @@ name)
                      if fi.Exists then yield fi.FullName }

        // Find in defaultPath or priorityFolders
        let priorityPaths = findInFolders priorityList
        if not <| Seq.isEmpty priorityPaths then Seq.head priorityPaths else

        // Find in ANY subfolder
        let tools = !! ("./**/" @@ "nuget.exe")
                    ++ ("./**/" @@ "NuGet.exe")
                    ++ ("./**/" @@ "Nuget.exe")
        if not <| Seq.isEmpty tools then Seq.head tools else

        // Find in PATH
        let nugetInPATH = findInFolders pathDirectories
        if not <| Seq.isEmpty nugetInPATH then Seq.head nugetInPATH else

        defaultPath @@ "NuGet.exe"
    with
    | _ -> defaultPath @@ "NuGet.exe"

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
let buildSources sources = 
    sources
    |> List.map (fun source -> " \"-Source\" \"" + source + "\"")
    |> separated ""
        
/// [omit]
let buildNuGetArgs setParams packageId = 
    let parameters = RestoreSinglePackageDefaults |> setParams
    let sources = parameters.Sources |> buildSources
        
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

/// Restores the packages in the given packages.config file from NuGet.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default NuGet parameters.
///  - `packageFile` - The packages.config file name.
///
/// ## Sample
///
///     Target "RestorePackages" (fun _ -> 
///          "./scr/ProjectA/packages.config"
///          |> RestorePackage (fun p ->
///              { p with
///                  Sources = "http:://myNugetSources.com" :: p.Sources
///                  OutputPath = outputDir
///                  Retries = 4 })
///      )
let RestorePackage setParams packageFile = 
    traceStartTask "RestorePackage" packageFile
    let (parameters:RestorePackageParams) = RestorePackageDefaults |> setParams

    let sources = parameters.Sources |> buildSources

    let args =
        " \"install\" \"" + (packageFile |> FullName) + "\"" +
        " \"-OutputDirectory\" \"" + (parameters.OutputPath |> FullName) + "\"" + sources

    runNuGetTrial parameters.Retries parameters.ToolPath parameters.TimeOut args (fun () -> failwithf "Package installation of %s generation failed." packageFile)
                    
    traceEndTask "RestorePackage" packageFile

/// Restores all packages from NuGet to the default directories by scanning for packages.config files in any subdirectory.
let RestorePackages() = 
    !! "./**/packages.config"
    |> Seq.iter (RestorePackage id)

/// Restores the packages in the given solution file file from NuGet.
/// ## Parameters
/// 
///  - `setParams` - Function used to manipulate the default NuGet parameters.
///  - `solutionFile` - The microsoft sln file name.
///
/// ## Sample
///
///     Target "RestorePackages" (fun _ -> 
///          "./scr/Everything.sln"
///          |> RestoreMSSolutionPackages (fun p ->
///              { p with
///                  Sources = "http:://myNugetSources.com" :: p.Sources
///                  OutputPath = outputDir
///                  Retries = 4 })
///      )
let RestoreMSSolutionPackages setParams solutionFile =
    traceStartTask "RestoreSolutionPackages" solutionFile
    let (parameters:RestorePackageParams) = RestorePackageDefaults |> setParams

    let sources = parameters.Sources |> buildSources

    let args = 
        "\"restore\" \"" + (solutionFile |> FullName) + "\"" +
        " \"-OutputDirectory\" \"" + (parameters.OutputPath |> FullName) + "\"" + sources

    runNuGetTrial parameters.Retries parameters.ToolPath parameters.TimeOut args (fun () -> failwithf "Package restore of %s failed" solutionFile)

    traceEndTask "RestoreSolutionPackages" solutionFile