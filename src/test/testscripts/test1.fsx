#r @"../../app/FAKE/bin/Debug/FakeLib.dll"

open Fake

Target "blah" (fun _ ->
    let simpleConfig = {
        ClassName = None
        AssemblyPath = "test.dll"
        Parameters = None
    }
    
    let nameConfig = {simpleConfig with ClassName = Some "simpleClass"}
    let simpleWithParams = {simpleConfig with Parameters = Some [("Verbosity", "High"); ("dupe2", "wat");]}
    let fullConfig = {simpleConfig with 
                        ClassName = nameConfig.ClassName 
                        Parameters = simpleWithParams.Parameters}

    let wcl = Some [
                    {
                        ClassName = Some "WorkflowCentralLogger"
                        AssemblyPath = "C:\Program Files\Microsoft Team Foundation Server 12.0\Tools\Microsoft.TeamFoundation.Build.Server.Logger.dll"
                        Parameters =
                            Some [
                                "Verbosity", "Normal"
                                "BuildUri", "vstfs:///Build/Build/364"
                                "IgnoreDuplicateProjects", "False"
                                "InformationNodeId", sprintf "%d" 8
                                "TargetsNotLogged", "GetNativeManifest,GetCopyToOutputDirectoryItems,GetTargetPath"
                                "TFSUrl", "https://ctaggart.visualstudio.com/DefaultCollection"
                            ]
                    }, 
                    Some {
                        ClassName = Some "WorkflowForwardingLogger"
                        AssemblyPath = "C:\Program Files\Microsoft Team Foundation Server 12.0\Tools\Microsoft.TeamFoundation.Build.Server.Logger.dll"
                        Parameters =
                            Some [
                                "Verbosity", "Normal"
                            ]
                    }
                ]

    let simpleRun = 
        [simpleConfig; nameConfig; simpleWithParams; fullConfig]
        |> List.map (fun x -> (x, None))
    let complexRun = 
        [simpleConfig; nameConfig; simpleWithParams; fullConfig]
        |> List.map (fun x -> (x, Some fullConfig))

    [wcl]
    |> List.map (fun x -> {MSBuildDefaults with DistributedLoggers = x})
    |> List.map MSBuildHelper.serializeMSBuildParams
    |> List.iter (logfn "%s")
)

RunTargetOrDefault "blah"

