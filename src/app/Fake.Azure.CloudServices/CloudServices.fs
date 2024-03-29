﻿namespace Fake.Azure

/// <namespacedoc>
/// <summary>
/// Azure namespace contains tasks to interact with Azure Services, like CloudServices and Kudu
/// </summary>
/// </namespacedoc>
///
/// <summary>
/// Contains tasks to package Azure Cloud Services.
/// </summary>
[<RequireQualifiedAccess>]
module CloudServices =

    open System.IO
    open Fake.Core
    open Fake.IO
    open Fake.IO.Globbing.Operators

    /// Configuration details for packaging cloud services.
    type PackageCloudServiceParams =
        {
            /// The name of the Cloud Service.
            CloudService: string
            /// The name of the role in the service.
            WorkerRole: string
            /// The SDK version to use e.g. 2.2. If None, the latest available version is used.
            SdkVersion: float option
            /// The output path for the .cspkg.
            OutputPath: string option
        }

    let DefaultCloudServiceParams =
        { CloudService = ""
          WorkerRole = ""
          SdkVersion = None
          OutputPath = None }

    /// The virtual machine size
    module VmSizes =
        type VmSize = VmSize of size: string
        let ExtraSmall = VmSize "ExtraSmall"
        let Small = VmSize "Small"
        let Medium = VmSize "Medium"
        let Large = VmSize "Large"
        let ExtraLarge = VmSize "ExtraLarge"
        let A5 = VmSize "A5"
        let A6 = VmSize "A6"
        let A7 = VmSize "A7"
        let A8 = VmSize "A8"
        let A9 = VmSize "A9"

    /// <summary>
    /// Modifies the size of the Worker Role in the csdef.
    /// </summary>
    ///
    /// <param name="vmSize">The virtual machine size</param>
    /// <param name="cloudService">The cloud service instance to update</param>
    let modifyVMSize (VmSizes.VmSize vmSize) cloudService =
        let csdefPath = sprintf @"%s\ServiceDefinition.csdef" cloudService

        csdefPath
        |> File.ReadAllText
        |> Xml.createDoc
        |> Xml.replaceXPathNS
            "/svchost:ServiceDefinition/svchost:WorkerRole/@vmsize"
            vmSize
            [ "svchost", "http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceDefinition" ]
        |> fun doc ->
            use fileStream = new FileStream(csdefPath, FileMode.Create)
            doc.Save fileStream

    /// <summary>
    /// Packages a cloud service role into a <c>.cspkg</c>, ready for deployment.
    /// </summary>
    ///
    /// <param name="packageCloudServiceParams">Could service parameters of type <c>PackageCloudServiceParams</c></param>
    let packageRole packageCloudServiceParams =
        let csPack =
            let sdkRoots =
                [ @"C:\Program Files\Microsoft SDKs\Windows Azure\.NET SDK\"
                  @"C:\Program Files\Microsoft SDKs\Azure\.NET SDK\" ]

            let availableCsPacks =
                sdkRoots
                |> Seq.collect (fun sdkRoot ->
                    !!(sdkRoot + @"**\cspack.exe")
                    |> Seq.filter (fun path -> path.Substring(sdkRoot.Length).StartsWith "v")
                    |> Seq.map (fun path -> sdkRoot, path))
                |> Seq.map (fun (sdkRoot, cspackPath) ->
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
                |> Seq.tryFind (fun (csPackVersion, _, _) -> csPackVersion = version)
                |> Option.map (fun (_, _, csPackFileInfo) -> csPackFileInfo)
            | None ->
                availableCsPacks
                |> Seq.sortBy (fun (v, _, _) -> -v)
                |> Seq.map (fun (_, _, csPackFileInfo) -> csPackFileInfo)
                |> Seq.tryFind (fun _ -> true)

        csPack
        |> Option.map (fun csPack ->
            packageCloudServiceParams.OutputPath
            |> Option.iter (DirectoryInfo.ensure << DirectoryInfo.ofPath)

            let outputFileArg =
                packageCloudServiceParams.OutputPath
                |> Option.map (fun path -> Path.Combine(path, (packageCloudServiceParams.CloudService + ".cspkg")))
                |> Option.map (sprintf "/out:%s")
                |> defaultArg
                <| ""

            let args =
                sprintf
                    @"%s\ServiceDefinition.csdef /role:%s;%s\bin\release;%s.dll %s"
                    packageCloudServiceParams.CloudService
                    packageCloudServiceParams.WorkerRole
                    packageCloudServiceParams.WorkerRole
                    packageCloudServiceParams.WorkerRole
                    outputFileArg

            let processResult =
                args
                |> CreateProcess.fromRawCommandLine csPack
                |> CreateProcess.redirectOutput
                |> Proc.run

            processResult.ExitCode)
