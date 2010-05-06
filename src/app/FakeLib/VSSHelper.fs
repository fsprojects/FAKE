[<AutoOpen>]
module Fake.VSSHelper 

open System

let getVSSProjectWithLabel toolPath srcSafeIni username password project (localSpec:string) label =    
    let args' = 
        sprintf "get %s -R \"-GL%s\" -I-Y -Y%s,%s -GTM" 
          project ((localSpec |> FullName).Trim('\\')) username password

    let args = 
        if String.IsNullOrEmpty label then args' else
        args' + sprintf " -Vl%s" label 

    tracefn "%s %s" toolPath args
    let result = execProcess2 (fun info ->
        [("SSDIR", srcSafeIni)]
          |> setEnvironmentVariables info 

        info.FileName <- toolPath
        info.WorkingDirectory <- "." |> FullName
        info.Arguments <- args) true
    if result <> 0 then failwith "Could not get sources from VSS"
    
let getVSSProject toolPath srcSafeIni username password project localSpec = 
    getVSSProjectWithLabel toolPath srcSafeIni username password project localSpec String.Empty