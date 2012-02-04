module Fake.DeploymentHelper
    
    open System
    open System.IO
    open Fake
    open Newtonsoft.Json

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

    let createDeploymentPackageFromZip packageName version fakescript archive =
        let package = {
            Id = packageName
            Version = version
            Script = File.ReadAllBytes(Path.GetFullPath(fakescript))
            Package = File.ReadAllBytes(Path.GetFullPath(archive))
        }
        IO.File.WriteAllBytes(packageName + ".fakepkg", Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(package)))
        
    let createDeploymentPackageFromDirectory packageName fakescript dir =
        let archive = packageName + ".zip"
        let files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
        Zip dir archive files
        createDeploymentPackageFromZip packageName fakescript archive

    let ensureDeployDir (package : DeploymentPackage) = 
        let path = Path.Combine("Work", package.TargetDir)
        ensureDirectory path
        path,package

    let unpack (dir,package) =
        let archive = package.Id + ".zip"
        File.WriteAllBytes(archive, package.Package)
        Unzip dir archive
        File.Delete archive
        let script = Path.Combine(dir, package.Id + ".fsx")
        File.WriteAllBytes(script, package.Script)
        script, package

    let prepare = ensureDeployDir >> unpack
    
    let doDeployment package = 
       let (script, _) = prepare package
       (FSIHelper.runBuildScript true script Seq.empty, package)
       
    let runDeployment package = 
        try
            doDeployment package |> Choice1Of2
        with e ->
            Choice2Of2(e)
        

     
        



