/// Contains tasks to package Azure Cloud Services.
module Fake.Azure.CloudServices

open System.IO
open Fake

/// Configuration details for packaging cloud services.
type PackageCloudServiceParams =
    { /// The name of the Cloud Service.
      CloudService : string
      /// The name of the role in the service.
      WorkerRole : string
      /// The SDK version to use e.g. 2.2. If None, the latest available version is used.
      SdkVersion : float option
      /// The output path for the .cspkg.
      OutputPath : string option }
    
let DefaultCloudServiceParams = { CloudService = ""; WorkerRole = ""; SdkVersion = None; OutputPath = None }
    
/// Packages a cloud service role into a .cspkg, ready for deployment.
let PackageRole packageCloudServiceParams =
    let csPack =
        let sdkRoots =
            [ @"C:\Program Files\Microsoft SDKs\Windows Azure\.NET SDK\"
              @"C:\Program Files\Microsoft SDKs\Azure\.NET SDK\" ]

        let availableCsPacks = 
            sdkRoots
            |> Seq.collect(fun sdkRoot ->    
                !! (sdkRoot + "**\cspack.exe")
                |> Seq.filter(fun path -> path.Substring(sdkRoot.Length).StartsWith "v")
                |> Seq.map(fun path -> sdkRoot, path))
            |> Seq.map(fun (sdkRoot, cspackPath) ->
                let version =
                    cspackPath.Substring(sdkRoot.Length).Split '\\'
                    |> Seq.head
                    |> fun version -> version.Substring 1
                    |> float
                version, sdkRoot, cspackPath)
            |> Seq.cache
    
        match packageCloudServiceParams.SdkVersion with
        | Some version ->
            availableCsPacks
            |> Seq.tryFind(fun (csPackVersion,_,_) -> csPackVersion = version)
            |> Option.map(fun (_,_,csPackFileInfo) -> csPackFileInfo)
        | None ->
            availableCsPacks
            |> Seq.sortBy(fun (v,_,_) -> -v)
            |> Seq.map(fun (_,_,csPackFileInfo) -> csPackFileInfo)
            |> Seq.tryFind(fun _ -> true)
        
    csPack
    |> Option.map(fun csPack ->
        packageCloudServiceParams.OutputPath |> Option.iter(fun path -> FileSystemHelper.ensureDirExists <| DirectoryInfo path)
        let outputFileArg =
            packageCloudServiceParams.OutputPath
            |> Option.map(fun path -> Path.Combine(path, (packageCloudServiceParams.CloudService + ".cspkg")))
            |> Option.map(sprintf "/out:%s")
            |> defaultArg
            <| ""        
            
        shellExec
            { defaultParams with
                Program = csPack
                CommandLine = sprintf @"%s\ServiceDefinition.csdef /role:%s;%s\bin\release;%s.dll %s" packageCloudServiceParams.CloudService packageCloudServiceParams.WorkerRole packageCloudServiceParams.WorkerRole packageCloudServiceParams.WorkerRole outputFileArg
                Args = [] })
    

