namespace Fake.Net

open System
open Fake.Core

/// <summary>
/// Contains a task which allows to perform SSH operations
/// </summary>
[<RequireQualifiedAccess>]
module SSH =

    /// <summary>
    /// The SSH parameter type.
    /// </summary>
    type SSHParams =
        {
            /// Path of the scp.exe
            ToolPath: string

            /// Path of the private key file (optional)
            PrivateKeyPath: string

            /// remote User
            RemoteUser: string

            /// The remote host
            RemoteHost: string

            /// The remote host port to use
            RemotePort: string
        }

    /// <summary>
    /// The SSH default parameters
    /// </summary>
    let SSHDefaults: SSHParams =
        { ToolPath = if Environment.isMono then "ssh" else "ssh.exe"
          RemoteUser = "fake"
          RemoteHost = "localhost"
          RemotePort = "22"
          PrivateKeyPath = null }

    let private getTarget sshParams =
        match sshParams.RemotePort with
        | "22" -> $"%s{sshParams.RemoteUser}@%s{sshParams.RemoteHost}"
        | _ -> $"%s{sshParams.RemoteUser}@%s{sshParams.RemoteHost}:%s{sshParams.RemotePort}"

    let private getPrivateKey privateKeyPath =
        if String.IsNullOrEmpty privateKeyPath then
            ""
        else
            $"-i \"%s{privateKeyPath}\""

    let internal buildArguments sshParams command =
        let target = sshParams |> getTarget
        let privateKey = sshParams.PrivateKeyPath |> getPrivateKey

        $"%s{privateKey} %s{target} %s{Args.toWindowsCommandLine [ command ]}"
        |> String.trim

    /// <summary>
    /// Performs a command via SSH.
    /// </summary>
    ///
    /// <param name="setParams">Function used to manipulate the default SSHParams value.</param>
    /// <param name="command">The target path. Can be something like <c>user@host:directory/TargetFile</c> or a local
    /// path.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// SSH (fun p -> { p with ToolPath = "tools/ssh.exe" }) command
    /// </code>
    /// </example>
    let SSH setParams command =
        let (sshParams: SSHParams) = setParams SSHDefaults
        let target = sshParams |> getTarget
        let args = buildArguments sshParams command

        Trace.tracefn $"%s{sshParams.ToolPath} %s{args}"

        let result =
            CreateProcess.fromRawCommandLine sshParams.ToolPath args
            |> CreateProcess.withTimeout (TimeSpan.MaxValue)
            |> Proc.run

        if result.ExitCode <> 0 then
            failwithf $"Error during SSH. Target: %s{target} Command: %s{command}"
