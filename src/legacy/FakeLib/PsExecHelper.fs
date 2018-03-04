/// Contains functions for working with Sysinternals PsExec
module Fake.PsExecHelper

let private formatArgs host username password exe inputs =
    sprintf @"\\%s -u %s -p %s ""%s"" %s" host username password exe inputs

/// Use Sysinternals PsExec to execute a process on a remote machine.
/// ## Parameters
///
/// - `host` - The hostname of the machine to connect to.
/// - `username` - A username valid for connecting to the remote machine.
/// - `password` - The cleartext password of the given user.
/// - `exe` - The path to the file that is to be executed.
/// - `inputs` - The command-line arguments to pass to the remote process.
/// - `timeOut` - The timeout for PsExec.
let execRemote host username password exe inputs timeout = 
    let args = formatArgs host username password exe inputs 
    let exitCode =
        ExecProcess (fun info ->  
            info.FileName <- "PsExec.exe"
            info.Arguments <- args) timeout
    if exitCode <> 0
    then failwithf "Failed to execute %s as user %s on host %s with args %s" exe username host inputs
