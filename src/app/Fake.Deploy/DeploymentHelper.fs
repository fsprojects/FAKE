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
        Key : NuSpecPackage
    }
    with 
        static member Sucessful packageKey =  { Status = Success; Key =  packageKey}
        static member Failure(packageKey, error) = { Status = Failure error; Key = packageKey}

type DeploymentPushStatus = 
    | Cancelled
    | Error of exn
    | Ok of DeploymentResponse
    | Unknown


let private extractPackageToTempPath (package : byte[]) = 
    let extractTempPath = Path.GetTempPath() @@ (Guid.NewGuid().ToString())
    let tempFile = Path.GetTempFileName()
    File.WriteAllBytes(tempFile, package)

    Unzip extractTempPath tempFile
    File.Delete(tempFile)
    directoryInfo extractTempPath

let private getNuSpecDetails (dir:DirectoryInfo) = 
    match dir |> filesInDirMatching "*.nuspec" |> List.ofArray with
    | h :: t ->  dir, NuGetHelper.getNuspecProperties h.FullName
    | _ -> failwith "Could not find nuspec file"

let private copyAndUnpackDeployment (tempDir : DirectoryInfo, package : NuSpecPackage) =
    let id = (sprintf "%s_%s" package.Id package.Version |> replace "." "_")
    let backupDir = directoryInfo ("Backup" @@ id + "/" + (DateTime.Now.ToString("dd_MM_yyyy_hh_mm_ss")))
    let workDirectory = directoryInfo ("Work" @@ id)
    if not <| workDirectory.Exists then () else FileUtils.cp_r workDirectory.FullName backupDir.FullName
    FileUtils.cp_r tempDir.FullName workDirectory.FullName
    match workDirectory |> filesInDirMatching "*.fsx" |> List.ofArray with
    | h :: t -> package, h
    | _ -> failwith "Could not find deployment script"

let unpack (package : byte[]) =
    let package, scriptFile =
        extractPackageToTempPath package
        |> getNuSpecDetails 
        |> copyAndUnpackDeployment

    package, scriptFile.FullName
    
let doDeployment (package,script) =
    try
        let workingDirectory = DirectoryName script
        let fakeLibTarget = workingDirectory @@ "FakeLib.dll"
        if  not <| File.Exists fakeLibTarget then File.Copy("FakeLib.dll", fakeLibTarget)
        if FSIHelper.runBuildScriptAt workingDirectory true (FullName script) Seq.empty
        then DeploymentResponse.Sucessful(package)
        else DeploymentResponse.Failure(package, Exception("Deployment script didn't run successfully"))
    with e ->
        DeploymentResponse.Failure(package, e) 
       
let runDeployment (package : byte[]) =
     unpack package |> doDeployment

let getPackageFromFile fileName = File.ReadAllBytes(fileName)

let runDeploymentFromPackageFile packageFileName = 
    packageFileName
    |> getPackageFromFile
    |> runDeployment

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