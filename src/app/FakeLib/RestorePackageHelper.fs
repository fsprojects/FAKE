[<AutoOpen>]
module Fake.RestorePackageHelper

open System
open System.IO

type RestorePackageParams =
    { ToolPath: string;
      PackagePattern : string
      TimeOut: TimeSpan
      OutputPath: string}

/// RestorePackage defaults params  
let RestorePackageDefaults =
    { ToolPath = "./tools/nuget/nuget.exe"
      PackagePattern = "./**/packages.config"
      TimeOut = TimeSpan.FromMinutes 5.
      OutputPath = "./packages" }
   
let RestorePackages setParams = 
    let details = "bluB"
    traceStartTask "RestorePackages" details
    let parameters = RestorePackageDefaults |> setParams
    
    let install package =
        let args =
            " \"install\" \"" + (package |> FullName) + "\"" +
            " \"-OutputDirectory\" \"" + (parameters.OutputPath |> FullName) + "\""

        if not (execProcess3 (fun info ->  
            info.FileName <- parameters.ToolPath |> FullName                 
            info.Arguments <- args) parameters.TimeOut)
        then
            failwithf "Package installation of %s generation failed." package

    !! parameters.PackagePattern
    |> Seq.iter install
                    
    traceEndTask "RestorePackages" details
