[<AutoOpen>]
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
/// Conatins a task which allows to perform SSH operations
module Fake.SSHHelper

/// The SSH parameter type.
[<CLIMutable>]
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type SSHParams = 
    { /// Path of the scp.exe 
      ToolPath : string
      /// Path of the private key file (optional)
      PrivateKeyPath : string 
      /// remote User
      RemoteUser : string
      RemoteHost : string
      RemotePort : string
      }


/// The SSH default parameters
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let SSHDefaults : SSHParams = 
    { ToolPath = if isMono then "ssh" else "ssh.exe"
      RemoteUser = "fake"
      RemoteHost = "localhost"
      RemotePort = "22"
      PrivateKeyPath = null }


/// Performs a command via SSH.
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the default SSHParams value.
///  - `command` - The target path. Can be something like user@host:directory/TargetFile or a local path.
///
/// ## Sample
///
///     SSH (fun p -> { p with ToolPath = "tools/ssh.exe" }) command
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let SSH setParams command = 
    let (p : SSHParams) = setParams SSHDefaults
    let target =
        if p.RemotePort = "22" then
            sprintf "%s@%s" p.RemoteUser p.RemoteHost
        else
            sprintf "%s@%s:%s" p.RemoteUser p.RemoteHost p.RemotePort

    let privateKey = if isNullOrEmpty p.PrivateKeyPath then "" else sprintf "-i \"%s\"" p.PrivateKeyPath
    let args = sprintf "%s %s %s" privateKey target (toParam command)

    tracefn "%s %s" p.ToolPath args
    let result = 
        ExecProcess (fun info -> 
            info.FileName <- p.ToolPath
            info.Arguments <- args) System.TimeSpan.MaxValue
    if result <> 0 then failwithf "Error during SSH. Target: %s Command: %s" target command
