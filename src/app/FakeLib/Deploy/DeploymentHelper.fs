module Fake.DeploymentHelper
    
open System
open System.IO
open System.Net
open Fake

type DeploymentResponseStatus =
| Success
| Failure of obj
| RolledBack
with 
    member x.GetError() = 
        match x with
        | Success | RolledBack -> null
        | Failure(err) -> err

type DeploymentResponse = {
        Status : DeploymentResponseStatus
        PackageName : string
    }
    with 
        static member Sucessful name =  { Status = Success; PackageName = name}
        static member RolledBack name = { Status = RolledBack; PackageName = name }
        static member Failure(name, error) = { Status = Failure error; PackageName = name}
        member x.SwitchTo(status) = { x with Status = status }

type DeploymentPushStatus = 
    | Cancelled
    | Error of exn
    | Ok of DeploymentResponse
    | Unknown

type Directories = {
    App : DirectoryInfo
    Backups : DirectoryInfo
    Active : DirectoryInfo
}

let private getNuspecInfos seq =
    seq
      |> Seq.map (fun pf ->
            pf
              |> ZipHelper.UnzipSingleFileInMemory ((fileInfo pf).Directory.Parent.Name + ".nuspec")
              |> NuGetHelper.getNuspecProperties)

let mutable workDir = "."
let mutable deploymentRootDir = "deployments/"

let getActiveReleasesInDirectory dir = 
    !! (dir @@ deploymentRootDir @@ "**/active/*.nupkg")
      |> getNuspecInfos

let getActiveReleases() = getActiveReleasesInDirectory workDir

let getActiveReleaseInDirectoryFor dir (app : string) = 
    !! (dir @@ deploymentRootDir + app + "/active/*.nupkg") 
      |> getNuspecInfos
      |> Seq.head

let getActiveReleaseFor (app : string) = getActiveReleaseInDirectoryFor workDir app

let getAllReleasesInDirectory dir = 
    !! (dir @@ deploymentRootDir @@ "**/*.nupkg")
      |> getNuspecInfos

let getAllReleases() = getAllReleasesInDirectory workDir

let getAllReleasesInDirectoryFor dir (app : string) = 
    !! (dir @@ deploymentRootDir + app + "/**/*.nupkg") 
      |> getNuspecInfos

let getAllReleasesFor (app : string) = getAllReleasesInDirectoryFor workDir app

let getBackupFor dir (app : string) (version : string) =
    let backupFileName =  app + "." + version + ".nupkg"
    dir @@ deploymentRootDir @@ app @@ "backups"
    |> FindFirstMatchingFile backupFileName

let unpack isRollback packageBytes =
    let extractTempPath = Path.GetTempPath() @@ (Guid.NewGuid().ToString())
    let tempFile = Path.GetTempFileName()
    WriteBytesToFile tempFile packageBytes

    Unzip extractTempPath tempFile
    File.Delete(tempFile)

    let nuSpecFile = FindFirstMatchingFile "*.nuspec" extractTempPath 
    let package = 
        nuSpecFile
        |> File.ReadAllText
        |> NuGetHelper.getNuspecProperties
        
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
          
    XCopy extractTempPath activeDir
    FileUtils.rm_rf extractTempPath

    WriteBytesToFile newActiveFilePath packageBytes

    let scriptFile = FindFirstMatchingFile "*.fsx" activeDir
    package, scriptFile
    
let doDeployment packageName script =
    try
        let workingDirectory = DirectoryName script
        
        if FSIHelper.runBuildScriptAt workingDirectory true (FullName script) Seq.empty then 
            DeploymentResponse.Sucessful(packageName)
        else 
            DeploymentResponse.Failure(packageName, Exception("Deployment script didn't run successfully"))
    with e ->
        DeploymentResponse.Failure(packageName, e) 
              
let runDeployment (packageBytes : byte[]) =
     let package,scriptFile = unpack false packageBytes
     doDeployment package.Name scriptFile

let runDeploymentFromPackageFile packageFileName =
    try
        packageFileName
        |> ReadFileAsBytes
        |> runDeployment
    with e ->
        DeploymentResponse.Failure(packageFileName, e) 

let rollbackFor dir (app : string) (version : string) =
    try 
        let currentPackageFileName = !! (dir @@ deploymentRootDir + app + "/active/*.nupkg") |> Seq.head
        let backupPackageFileName = getBackupFor dir app version
        if currentPackageFileName = backupPackageFileName
        then DeploymentResponse.Failure(app + "." + version + ".nupkg", "Cannot rollback to currently active version")
        else 
            let package,scriptFile = unpack true (backupPackageFileName |> ReadFileAsBytes)
            (doDeployment package.Name scriptFile).SwitchTo(RolledBack)
    with
        | :? FileNotFoundException as e -> DeploymentResponse.Failure(e.FileName, sprintf "Failed to rollback to %s %s could not find package file or deployment script file ensure the version is within the backup directory and the deployment script is in the root directory of the *.nupkg file" app version)
        | _ as e -> DeploymentResponse.Failure(app + "." + version + ".nupkg", "Rollback Failed: " + e.Message)

let rollback (app : string) (version : string) = rollbackFor workDir app version

let postDeploymentPackage url packageFileName = 
    let result = ref Unknown
    let waitHandle = new Threading.AutoResetEvent(false)
    let handle (event : UploadDataCompletedEventArgs) =
        if event.Cancelled then 
            result := Cancelled
            waitHandle.Set() |> ignore
        elif event.Error <> null then 
            result := Error(event.Error)
            waitHandle.Set() |> ignore
        else
            use ms = new MemoryStream(event.Result)
            use sr = new StreamReader(ms, Text.Encoding.UTF8)
            let res = sr.ReadToEnd()
            result := Json.deserialize<DeploymentResponse> res |> Ok
            waitHandle.Set() |> ignore

    let uri = new Uri(url, UriKind.Absolute)
    let client = new WebClient()
    let mutable uploaded = false
    client.Headers.Add(HttpRequestHeader.ContentType, "application/fake")
    client.UploadDataCompleted |> Event.add handle
    client.UploadDataAsync(uri, "POST", ReadFileAsBytes packageFileName)
    waitHandle.WaitOne() |> ignore
    !result


let PostDeploymentPackage url packageFileName = 
    match postDeploymentPackage url packageFileName with
    | Ok(_) -> tracefn "Deployment of %s successful" packageFileName
    | Error(exn) -> failwithf "Deployment of %A failed\r\n%A" packageFileName exn
    | response -> failwithf "Deployment of %A failed\r\n%A" packageFileName response
