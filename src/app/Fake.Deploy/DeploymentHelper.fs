/// Contains helper functions for Fake.Deploy
module Fake.DeploymentHelper

open System
open System.IO
open Fake.FakeDeployAgentHelper

/// Allows to specify a deployment version
let predecessorPrefix = "head~"

type VersionInfo = 
    /// Allows to deploy a specific version
    | Specific of string
    /// Allows to deploy a version which is a predecessor to the current version
    | Predecessor of int
    static member Parse(s : string) = 
        let s = s.ToLower()
        if s.StartsWith predecessorPrefix then 
            s.Replace(predecessorPrefix, "")
            |> Int32.Parse
            |> Predecessor
        else Specific s

/// The root dir for Fake.Deploy - Dafault value is "./deployments"
let mutable deploymentRootDir = "deployments/"

/// Retrieves the NuSpec information for all active releases.
let getActiveReleases dir = !!(dir @@ deploymentRootDir @@ "**/active/*.nupkg") |> Seq.map GetMetaDataFromPackageFile

/// Retrieves the NuSpec information for the active release of the given app.
let getActiveReleaseFor dir (app : string) = 
    !!(dir @@ deploymentRootDir @@ app @@ "/active/*.nupkg")
    |> Seq.map GetMetaDataFromPackageFile
    |> Seq.head

/// Retrieves the NuSpec information of all releases.
let getAllReleases dir = !!(dir @@ deploymentRootDir @@ "**/*.nupkg") |> Seq.map GetMetaDataFromPackageFile

/// Retrieves the NuSpec information for all releases of the given app.
let getAllReleasesFor dir (app : string) = 
    !!(dir @@ deploymentRootDir @@ app @@ "/**/*.nupkg") |> Seq.map GetMetaDataFromPackageFile

/// Returns statistics about the machine environment.
let getStatistics() = getMachineEnvironment()

/// Gets the backup package file name for the given app and version
let getBackupFor dir (app : string) (version : string) = 
    let backupFileName = app + "." + version + ".nupkg"
    dir @@ deploymentRootDir @@ app @@ "backups"
    |> FindFirstMatchingFile backupFileName

let findScriptFile dir =
    let prioFiles = ["deploy.fsx",1; "install.fsx",2; "setup.fsx",3]
    let getWeight s =
        let idx = prioFiles |> List.tryFind(fun x -> (fst x) = s)
        if idx.IsSome then snd idx.Value else 999
    let pattern = "*.fsx"
    let fsxFiles = filesInDirMatching pattern (DirectoryInfo(dir))
    let weighted = fsxFiles |> Array.sortBy(fun n -> getWeight (Path.GetFileName(n.FullName).ToLower()))
    if weighted.Length > 0
    then weighted.[0].FullName
    else new FileNotFoundException(sprintf "Could not find file matching %s in %s" pattern dir) |> raise

/// Extracts the NuGet package
let unpack workDir isRollback packageBytes = 
    let tempFile = Path.GetTempFileName()
    WriteBytesToFile tempFile packageBytes
    let package = GetMetaDataFromPackageFile tempFile
    let activeDir = workDir @@ deploymentRootDir @@ package.Id @@ "active"
    let newActiveFilePath = activeDir @@ package.FileName
    match TryFindFirstMatchingFile "*.nupkg" activeDir with
    | Some activeFilePath -> 
        let backupDir = workDir @@ deploymentRootDir @@ package.Id @@ "backups"
        ensureDirectory backupDir
        if not isRollback then MoveFile backupDir activeFilePath
    | None -> ()
    CleanDir activeDir
    Unzip activeDir tempFile
    File.Delete tempFile
    WriteBytesToFile newActiveFilePath packageBytes
    let scriptFile = findScriptFile activeDir
    package, scriptFile

/// Runs a deployment script from the given package
let doDeployment scriptFileName scriptArgs =
    try
        TargetHelper.reset()
        let (result, messages) = FSIHelper.executeFSIWithScriptArgsAndReturnMessages (FullName scriptFileName) scriptArgs
        if result then 
            Success { Messages = messages
                      IsError = false
                      Exception = null }
        else 
            Failure { Messages = messages
                      IsError = true
                      Exception = (Exception "Deployment script didn't run successfully") }
    with e -> 
        traceException e
        Failure { Messages = Seq.empty
                  IsError = true
                  Exception = e }

/// Runs a deployment from the given package file name
let runDeploymentFromPackageFile workDir packageFileName scriptArgs = 
    try 
        let packageBytes = ReadFileAsBytes packageFileName
        let _, scriptFile = unpack workDir false packageBytes
        doDeployment scriptFile scriptArgs
    with e -> 
        Failure { Messages = Seq.empty
                  IsError = true
                  Exception = e }

/// Rolls the given app back to the specified version
let rollback workDir (app : string) (version : string) = 
    try 
        let currentPackageFileName = !!(workDir @@ deploymentRootDir @@ app @@ "/active/*.nupkg") |> Seq.head
        let backupPackageFileName = getBackupFor workDir app version
        if currentPackageFileName = backupPackageFileName then 
            Failure { Messages = Seq.empty
                      IsError = true
                      Exception = (Exception "Cannot rollback to currently active version") }
        else 
            let _, scriptFile = unpack workDir true (backupPackageFileName |> ReadFileAsBytes)
            doDeployment scriptFile [||]
    with
    | :? FileNotFoundException -> 
        let msg = 
            sprintf 
                "Failed to rollback to %s %s could not find package file or deployment script file ensure the version is within the backup directory and the deployment script is in the root directory of the *.nupkg file" 
                app version
        Failure { Messages = 
                      [ { IsError = true
                          Message = "Rollback failed: File not found"
                          Timestamp = DateTimeOffset.UtcNow } ]
                  IsError = true
                  Exception = (Exception msg) }
    | e -> 
        Failure { Messages = 
                      [ { IsError = true
                          Message = "Rollback failed"
                          Timestamp = DateTimeOffset.UtcNow } ]
                  IsError = true
                  Exception = e }

/// Returns the version no. which specified in the NuGet package
let getVersionFromNugetFileName (app : string) (fileName : string) = 
    Path.GetFileName(fileName).ToLower().Replace(".nupkg", "").Replace(app.ToLower() + ".", "")

/// Returns the version no. of the latest backup of the given app
let getPreviousPackageVersionFromBackup dir app versions = 
    let currentPackageFileName = 
        !!(dir @@ deploymentRootDir @@ app @@ "/active/*.nupkg")
        |> Seq.head
        |> getVersionFromNugetFileName app
    !!(dir @@ deploymentRootDir @@ app @@ "/backups/*.nupkg")
    |> Seq.map (getVersionFromNugetFileName app)
    |> Seq.filter (fun x -> x < currentPackageFileName)
    |> Seq.toList
    |> List.sort
    |> List.rev
    |> Seq.skip (versions - 1)
    |> Seq.head

/// Rolls the given app back to the specified version info
let rollbackTo workDir app versionInfo = 
    let previousVersion = predecessorPrefix + "1"
    let versionInfo = if String.IsNullOrEmpty versionInfo then previousVersion else  versionInfo
    try 
        let newVersion = 
            match VersionInfo.Parse versionInfo with
            | Specific version -> version
            | Predecessor p -> getPreviousPackageVersionFromBackup workDir app p
        rollback workDir app newVersion
    with e -> 
        Failure { Messages = 
                      [ { IsError = true
                          Message = sprintf "Rollback to version (%s-%s) failed" app versionInfo
                          Timestamp = DateTimeOffset.UtcNow } ]
                  IsError = true
                  Exception = e }