[<AutoOpen>]
/// Conatins a task which allows to perform file copies using [SCP](http://en.wikipedia.org/wiki/Secure_copy), which is based on the Secure Shell (SSH) protocol.
module Fake.SCPHelper

/// The SCP parameter type.
type SCPParams = 
    { /// Path of the scp.exe 
      ToolPath : string
      /// Path of the private key file (optional)
      PrivateKeyPath : string }

/// The SCP default parameters
let SCPDefaults : SCPParams = 
    { ToolPath = if isMono then "scp" else "scp.exe"
      PrivateKeyPath = null }

/// Performs a SCP copy from the given source directory to the target path.
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default SCPParams value.
///  - `source` - The source path. Can be something like user@host:directory/SourceFile or a local path.
///  - `target` - The target path. Can be something like user@host:directory/TargetFile or a local path.
///
/// ## Sample
///
///     SCP (fun p -> { p with ToolPath = "tools/scp.exe" }) source target
let SCP setParams source target = 
    let (p : SCPParams) = setParams SCPDefaults
    let source = FullName source
    let recursiveFlag,sourceArg,workingDir =
        if isDirectory source then
            "-r",".",source
        else
            let fi = fileInfo source
            "",fi.Name,fi.Directory.FullName

    let privateKey = if isNullOrEmpty p.PrivateKeyPath then "" else sprintf "-i \"%s\"" p.PrivateKeyPath
    let args = sprintf "%s %s %s %s" recursiveFlag privateKey (toParam sourceArg) (toParam target)

    tracefn "%s %s" p.ToolPath args
    let result = 
        ExecProcess (fun info -> 
            info.FileName <- p.ToolPath
            info.WorkingDirectory <- workingDir
            info.Arguments <- args) System.TimeSpan.MaxValue
    if result <> 0 then failwithf "Error during SCP. Source: %s Target: %s" source target
