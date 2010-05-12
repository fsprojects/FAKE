[<AutoOpen>]
module Fake.VSSHelper 

let getVSSProjectWithLabel toolPath srcSafeIni username password project (localSpec:string) label =    
    let args' = 
        sprintf "get %s -R \"-GL%s\" -I-Y -Y%s,%s -GTM" 
          project (localSpec |> FullName |> trimSeparator) username password

    let args = 
        if isNullOrEmpty label then args' else
        sprintf "%s -Vl%s" args' label 

    tracefn "%s %s" toolPath args
    let result = execProcess2 (fun info ->
        ["SSDIR", srcSafeIni]
          |> setEnvironmentVariables info 

        info.FileName <- toolPath
        info.WorkingDirectory <- currentDirectory
        info.Arguments <- args) true
    if result <> 0 then failwith "Could not get sources from VSS"
    
let getVSSProject toolPath srcSafeIni username password project localSpec = 
    getVSSProjectWithLabel toolPath srcSafeIni username password project localSpec ""