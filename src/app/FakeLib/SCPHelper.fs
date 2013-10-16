[<AutoOpen>]
module Fake.SCPHelper 

/// The SCP parameter type
type SCPParams =
    { ToolPath:string;
      PrivateKeyPath:string}

/// The SCP default parameters
let SCPDefaults:SCPParams =
    { ToolPath = "scp.exe"
      PrivateKeyPath = null}

/// Performs a SCP copy from the gicen source directory to the target directory
let SCP setParams source target =
    let (p:SCPParams) = setParams SCPDefaults
    tracefn "SCP %s %s" source target
    
    let args = 
        sprintf "-r %s \".\" %s"
            (if isNullOrEmpty p.PrivateKeyPath then "" else sprintf "-i \"%s\"" p.PrivateKeyPath)
            (toParam target)

    tracefn "%s %s" p.ToolPath args
    let result = 
        ExecProcess (fun info ->
            info.FileName <- p.ToolPath
            info.WorkingDirectory <- source |> FullName
            info.Arguments <- args) System.TimeSpan.MaxValue
               
    if result <> 0 then failwithf "Error during SCP From: %s To: %s" source target