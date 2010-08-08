namespace Fake

type SCPParams =
    { ToolPath:string;
      PrivateKeyPath:string}

[<AutoOpen>]
module SCPHelper =

    /// SCP default params  
    let SCPDefaults:SCPParams =
        { ToolPath = "scp.exe"
          PrivateKeyPath = null}

    /// <summary>Performs a SCP copy.</summary>
    /// <param name="source">The source directory (fileName)</param>
    /// <param name="destination">The target directory (fileName)</param>
    let SCP setParams source destination =
        let (p:SCPParams) = setParams SCPDefaults
        tracefn "SCP %s %s" source destination
    
        let args = 
          sprintf "-r %s \".\" %s"
            (if isNullOrEmpty p.PrivateKeyPath then "" else sprintf "-i \"%s\"" p.PrivateKeyPath)
            (destination |> toParam)

        tracefn "%s %s" p.ToolPath args
        let result = 
            ExecProcess (fun info ->
                info.FileName <- p.ToolPath
                info.WorkingDirectory <- source |> FullName
                info.Arguments <- args) System.TimeSpan.MaxValue
               
        if result <> 0 then failwithf "Error during SCP From: %s To: %s" source destination