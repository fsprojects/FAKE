module Fake.DeploymentHelper
    
open System
open System.IO
open System.Net
open Fake
open Fake.HttpClientHelper

let private extractNuspecFromPackageFile packageFileName =   
    packageFileName
    |> ZipHelper.UnzipFirstMatchingFileInMemory (fun ze -> ze.Name.EndsWith ".nuspec") 
    |> NuGetHelper.getNuspecProperties

let mutable workDir = "."
let mutable deploymentRootDir = "deployments/"

let getActiveReleasesInDirectory dir = 
    !! (dir @@ deploymentRootDir @@ "**/active/*.nupkg")
      |> Seq.map extractNuspecFromPackageFile

let getActiveReleases() = getActiveReleasesInDirectory workDir

let getActiveReleaseInDirectoryFor dir (app : string) = 
    !! (dir @@ deploymentRootDir + app + "/active/*.nupkg") 
      |> Seq.map extractNuspecFromPackageFile
        |> Seq.head 

let getActiveReleaseFor (app : string) = getActiveReleaseInDirectoryFor workDir app

let getAllReleasesInDirectory dir = 
    !! (dir @@ deploymentRootDir @@ "**/*.nupkg")
      |> Seq.map extractNuspecFromPackageFile


let getAllReleases() = getAllReleasesInDirectory workDir

let getAllReleasesInDirectoryFor dir (app : string) = 
    !! (dir @@ deploymentRootDir + app + "/**/*.nupkg") 
      |> Seq.map extractNuspecFromPackageFile

let getAllReleasesFor (app : string) = getAllReleasesInDirectoryFor workDir app

let getBackupFor dir (app : string) (version : string) =
    let backupFileName =  app + "." + version + ".nupkg"
    dir @@ deploymentRootDir @@ app @@ "backups"
    |> FindFirstMatchingFile backupFileName

let unpack isRollback packageBytes =
    let tempFile = Path.GetTempFileName()
    WriteBytesToFile tempFile packageBytes

    let package = extractNuspecFromPackageFile tempFile   
        
    let activeDir = workDir @@ deploymentRootDir @@ package.Id @@ "active"   
    let newActiveFilePath = activeDir @@ package.FileName

    match TryFindFirstMatchingFile "*.nupkg" activeDir with
    | Some activeFilePath ->
        let backupDir = workDir @@ deploymentRootDir @@ package.Id @@ "backups"
    
        ensureDirectory backupDir
        if not isRollback then
            MoveFile backupDir activeFilePath
    | None -> ()
    
    CleanDir activeDir
    Unzip activeDir tempFile
    File.Delete tempFile

    WriteBytesToFile newActiveFilePath packageBytes

    let scriptFile = FindFirstMatchingFile "*.fsx" activeDir
    package, scriptFile
    
let doDeployment packageName script =
    try
        let workingDirectory = DirectoryName script
        
        if FSIHelper.runBuildScriptAt workingDirectory true (FullName script) Seq.empty then 
            Success
        else 
            Failure(Exception "Deployment script didn't run successfully")
    with e -> Failure e
              
let runDeploymentFromPackageFile packageFileName =
    try
      let packageBytes =  ReadFileAsBytes packageFileName
      let package,scriptFile = unpack false packageBytes
      doDeployment package.Name scriptFile        
    with e -> Failure e

let rollbackFor dir (app : string) (version : string) =
    try 
        let currentPackageFileName = !! (dir @@ deploymentRootDir + app + "/active/*.nupkg") |> Seq.head
        let backupPackageFileName = getBackupFor dir app version
        if currentPackageFileName = backupPackageFileName
        then Failure "Cannot rollback to currently active version"
        else 
            let package,scriptFile = unpack true (backupPackageFileName |> ReadFileAsBytes)
            match doDeployment package.Name scriptFile with
            | Success -> RolledBack
            | x -> x
    with
        | :? FileNotFoundException as e -> Failure (sprintf "Failed to rollback to %s %s could not find package file or deployment script file ensure the version is within the backup directory and the deployment script is in the root directory of the *.nupkg file" app version)
        | _ as e -> Failure("Rollback failed: " + e.Message)

let rollback (app : string) (version : string) = rollbackFor workDir app version

let PostDeploymentPackage url packageFileName = 
    match HttpClientHelper.postDeploymentPackage url packageFileName with
    | Success -> tracefn "Deployment of %s successful" packageFileName
    | Failure exn -> failwithf "Deployment of %A failed\r\n%A" packageFileName exn
    | response -> failwithf "Deployment of %A failed\r\n%A" packageFileName response
