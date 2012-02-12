module Fake.DeploymentHelper
    
open System
open System.IO
open System.Net
open Fake

type DeploymentResponse = {
        Success : bool
        Id : string
        Version : string
        Error : obj
    }
    with 
        static member Sucessful(packageId, version) = 
            {
                Success = true;
                Id = packageId;
                Version = version;
                Error = null;
            }

        static member Failure(packageId, version, error) =
            {
                Success = false;
                Id = packageId;
                Version = version;
                Error = error;
            }

        override x.ToString() = 
            if x.Success 
            then sprintf "Deployment of %s %s successful" x.Id x.Version
            else sprintf "Deployment of %s %s failed\n\n%A" x.Id x.Version x.Error

type DeploymentPackage = {
        Id : string
        Version : string
        Script : byte[]
        Package : byte[]
    }
    with
        member x.TargetDir =
            x.Id + "_" + (x.Version.Replace('.','_'))

        override x.ToString() = 
            x.Id + " " + x.Version

let createDeploymentPackageFromZip packageName version fakescript archive output =
    ensureDirectory output
    let package = {
        Id = packageName
        Version = version
        Script =  fakescript |> FullName |> ReadFileAsBytes
        Package = archive |> FullName  |> ReadFileAsBytes
    }

    let fileName = output @@ (packageName + ".fakepkg")

    package
        |> Json.serialize
        |> Text.Encoding.UTF8.GetBytes
        |> WriteBytesToFile fileName

    File.Delete archive

let createDeploymentPackageFromDirectory packageName version fakescript dir output =
    let archive = packageName + ".zip"
    let files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
    Zip dir archive files
    createDeploymentPackageFromZip packageName version fakescript archive output

let ensureDeployDir (package : DeploymentPackage) = 
    let path = "Work" @@ package.TargetDir
    ensureDirectory path
    path,package

let unpack (dir,package) =
    let archive = package.Id + ".zip"

    package.Package |> WriteBytesToFile archive
    Unzip dir archive
    File.Delete archive

    let script = dir @@ (package.Id + ".fsx")
    package.Script |> WriteBytesToFile script
    script, package

let prepare = ensureDeployDir >> unpack
    
let doDeployment package = 
    let (script, _) = prepare package
    let workingDirectory = DirectoryName script
    let fakeLibTarget = workingDirectory @@ "FakeLib.dll"
    if  not <| File.Exists fakeLibTarget then File.Copy("FakeLib.dll", fakeLibTarget)
    (FSIHelper.runBuildScriptAt workingDirectory true (FullName script) Seq.empty, package)
       
let runDeployment package = 
    try
        doDeployment package |> Choice1Of2
    with e ->
        Choice2Of2(e)

let runDeploymentFromPackage packagePath = 
    try
        ReadFileAsString packagePath
        |> Json.deserialize
        |> runDeployment
    with e -> 
        Choice2Of2(e)


let postDeploymentPackage url packagePath = 
    let result = ref None
    let waitHandle = new Threading.AutoResetEvent(false)
    let handle (event : UploadDataCompletedEventArgs) =
        if event.Cancelled 
        then 
            result := Some <| Choice2Of2(OperationCanceledException() :> exn)
            waitHandle.Set() |> ignore
        elif event.Error <> null
        then 
            result := Some <| Choice2Of2(event.Error)
            waitHandle.Set() |> ignore
        else
            use ms = new MemoryStream(event.Result)
            use sr = new StreamReader(ms, Text.Encoding.UTF8)
            let res = sr.ReadToEnd()
            result := Json.deserialize<DeploymentResponse> res |> Choice1Of2 |> Some 
            waitHandle.Set() |> ignore

    let uri = new Uri(url, UriKind.Absolute)
    let client = new WebClient()
    let mutable uploaded = false
    client.Headers.Add(HttpRequestHeader.ContentType, "application/fake")
    client.UploadDataCompleted |> Event.add handle
    client.UploadDataAsync(uri, "POST", ReadFileAsBytes packagePath)
    waitHandle.WaitOne() |> ignore
    !result