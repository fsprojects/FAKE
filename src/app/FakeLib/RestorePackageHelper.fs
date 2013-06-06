[<AutoOpen>]
module Fake.RestorePackageHelper

open System
open System.IO

type RestorePackageParams =
    { ToolPath: string
      Sources: string list
      TimeOut: TimeSpan
      OutputPath: string}

/// RestorePackage defaults params  
let RestorePackageDefaults =
    { ToolPath = "./tools/nuget/nuget.exe"
      Sources = []
      TimeOut = TimeSpan.FromMinutes 5.
      OutputPath = "./packages" }
   
let RestorePackage setParams package = 
    let parameters = RestorePackageDefaults |> setParams
    traceStartTask "RestorePackage" package
    
    let sources =
        parameters.Sources
        |> List.map (fun source -> " \"-Source\" \"" + source + "\"")
        |> separated ""

    let args =
        " \"install\" \"" + (package |> FullName) + "\"" +
        " \"-OutputDirectory\" \"" + (parameters.OutputPath |> FullName) + "\"" + sources

    if not (execProcess3 (fun info ->  
        info.FileName <- parameters.ToolPath |> FullName                 
        info.Arguments <- args) parameters.TimeOut)
    then
        failwithf "Package installation of %s generation failed." package
                    
    traceEndTask "RestorePackage" package

let RestorePackages() = 
  !! "./**/packages.config"
  |> Seq.iter (RestorePackage id)