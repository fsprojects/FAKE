module Fake.DeploymentHelper
    
open System
open System.IO
open System.Net
open Fake

type DeploymentResponseStatus =
| Success
| Failure of obj
with 
    member x.GetError() = 
        match x with
        | Success -> null
        | Failure(err) -> err

type DeploymentResponse = {
        Status : DeploymentResponseStatus
        PackageName : string
    }
    with 
        static member Sucessful name =  { Status = Success; PackageName = name}
        static member Failure(name, error) = { Status = Failure error; PackageName = name}

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

let private getDirectoriesFor (appname : string) =
    let appName = appname.ToUpper()
    let dirs = 
        ["packages" @@ appName
         "packages" @@ appName @@ "backups"
         "packages" @@ appName @@ "active"]
        |> List.map (fun x -> ensureDirectory x; directoryInfo x)
    { App = dirs.[0]; Backups = dirs.[1]; Active = dirs.[2] }

let getActiveReleases() = 
    !! "packages/**/active/*.nupkg" 
      |> Seq.map NuGetHelper.getNuspecProperties

let getActiveReleasesFor (app : string) = 
    let dirs = getDirectoriesFor app
    !! (dirs.Active.FullName @@ "*.nupkg") 
        |> Seq.map NuGetHelper.getNuspecProperties

let getAllReleases() = 
    !! "packages/**/*.nupkg"
       |> Seq.map NuGetHelper.getNuspecProperties

let getAllReleasesFor (app : string) = 
    let dirs = getDirectoriesFor app
    !! (dirs.App.FullName @@ "*.nupkg") 
        |> Seq.map NuGetHelper.getNuspecProperties

let unpack packageBytes =
    let extractTempPath = Path.GetTempPath() @@ (Guid.NewGuid().ToString())
    let tempFile = Path.GetTempFileName()
    File.WriteAllBytes(tempFile, packageBytes)

    Unzip extractTempPath tempFile
    File.Delete(tempFile)

    let tempDir = directoryInfo extractTempPath

    let package = FindFirstMatchingFile "*.nuspec" tempDir |> NuGetHelper.getNuspecProperties

    let backupDir = directoryInfo ("Backup" @@ package.DirectoryName + "/" + (DateTime.Now.ToString("dd_MM_yyyy_hh_mm_ss")))
    let workDirectory = directoryInfo ("packages" @@ package.DirectoryName)
    if workDirectory.Exists then 
      FileUtils.cp_r workDirectory.FullName backupDir.FullName

    FileUtils.cp_r tempDir.FullName workDirectory.FullName

    let scriptFile = FindFirstMatchingFile "*.fsx" workDirectory

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
     let package,scriptFile = unpack packageBytes
     doDeployment package.Name scriptFile

let runDeploymentFromPackageFile packageFileName =
    try
        packageFileName
        |> ReadFileAsBytes
        |> runDeployment
    with e ->
        DeploymentResponse.Failure(packageFileName, e) 

let DeployPackageLocally packageFileName =
    match runDeploymentFromPackageFile packageFileName with
    | response when response.Status = Success -> tracefn "Deployment of %s successful" packageFileName
    | response -> failwithf "Deployment of %A failed\r\n%A" packageFileName (response.Status.GetError())     

let postDeploymentPackage url packagePath = 
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
    client.UploadDataAsync(uri, "POST", ReadFileAsBytes packagePath)
    waitHandle.WaitOne() |> ignore
    !result