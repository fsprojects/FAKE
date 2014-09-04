#r @"../../app/FAKE/bin/Debug/FakeLib.dll"

open Fake

Target "blah" (fun _ ->
    let simpleConfig = {
        ClassName = None
        AssemblyPath = "test.dll"
        Parameters = None
    }
    
    let nameConfig = {simpleConfig with ClassName = Some "simpleClass"}
    let simpleWithParams = {simpleConfig with Parameters = Some [("Verbosity", "High")]}
    let fullConfig = {simpleConfig with 
                        ClassName = nameConfig.ClassName 
                        Parameters = simpleWithParams.Parameters}

    let simpleRun = 
        [simpleConfig; nameConfig; simpleWithParams; fullConfig]
        |> List.map (fun x -> (x, None))
    let complexRun = 
        [simpleConfig; nameConfig; simpleWithParams; fullConfig]
        |> List.map (fun x -> (x, Some fullConfig))

    simpleRun @ complexRun
    |> List.map (fun x -> {MSBuildDefaults with DistributedLoggers = Some [x; x]})
    |> List.map MSBuildHelper.serializeMSBuildParams
    |> List.iter (logfn "%s")
)

RunTargetOrDefault "blah"

