namespace Fake.JavaScript

open Fake.Core
open Fake.IO
open System
open System.IO

/// Helpers for running the yarn tool
///
/// ## Sample
///
///     Yarn.install (fun o ->
///                     { o with
///                         WorkingDirectory = "./src/FAKESimple.Web/"
///                     })
[<RequireQualifiedAccess>]
module Yarn =

    /// Default paths to Yarn
    let private yarnFileName =
        ProcessUtils.tryFindFileOnPath "yarn"
        |> function
            | Some yarn when File.Exists yarn -> yarn
            | _ ->
                match Environment.isWindows with
                | true -> "./packages/Yarnpkg.js/tools/yarn.cmd"
                | _ -> "/usr/bin/yarn"

    /// Arguments for the Yarn install command
    type InstallArgs =
    | Standard
    | Flat
    | Force
    | Har
    | NoLockFile
    | Production
    | PureLockFile
    | FrozenLockFile

    /// The list of supported Yarn commands. The `Custom` alternative
    /// can be used for other commands not in the list until they are
    /// implemented
    type YarnCommand =
    | Install of InstallArgs
    | Custom of string

    /// The Yarn parameter type
    type YarnParams =
        { Src: string
          YarnFilePath: string
          WorkingDirectory: string
          Timeout: TimeSpan }

    /// Yarn default parameters
    let defaultYarnParams =
        { Src = ""
          YarnFilePath = yarnFileName
          WorkingDirectory = "."
          Timeout = TimeSpan.MaxValue }

    let private parseInstallArgs = function
        | Standard -> ""
        | Flat -> " --flat"
        | Force -> " --force"
        | Har -> " --har"
        | NoLockFile -> " --no-lockfile"
        | Production -> " --production"
        | PureLockFile -> " --pure-lockfile"
        | FrozenLockFile -> " --frozen-lockfile"

    let private parse = function
        | Install installArgs -> sprintf "install%s" (installArgs |> parseInstallArgs)
        | Custom str -> str

    let private run yarnParams command =
        let yarnPath = Path.GetFullPath(yarnParams.YarnFilePath)
        let arguments = command |> parse
        arguments
        |> CreateProcess.fromRawCommandLine yarnPath
        |> CreateProcess.withWorkingDirectory yarnParams.WorkingDirectory
        |> CreateProcess.withTimeout yarnParams.Timeout
        |> CreateProcess.ensureExitCodeWithMessage (sprintf "'yarn %s' task failed" arguments)
        |> Proc.run
        |> ignore

    let private yarn setParams = defaultYarnParams |> setParams |> run

    /// Run `yarn install`
    /// ## Parameters
    /// - 'setParams' - set command parameters
    /// ## Sample
    ///
    ///     Yarn.install (fun o ->
    ///                     { o with
    ///                         WorkingDirectory = "./src/FAKESimple.Web/"
    ///                     })
    let install setParams = yarn setParams <| Install Standard

    /// Run `yarn install --production`
    /// ## Parameters
    /// - 'setParams' - set command parameters
    /// ## Sample
    ///
    ///     Yarn.installProduction (fun o ->
    ///                               { o with
    ///                                   WorkingDirectory = "./src/FAKESimple.Web/"
    ///                               })

    let installProduction setParams = yarn setParams <| Install Production
    /// Run `yarn install --force`
    /// ## Parameters
    /// - 'setParams' - set command parameters
    /// ## Sample
    ///
    ///     Yarn.installForced (fun o ->
    ///                           { o with
    ///                               WorkingDirectory = "./src/FAKESimple.Web/"
    ///                           })
    let installForced setParams = yarn setParams <| Install Force

    /// Run `yarn install --flat`
    /// ## Parameters
    /// - 'setParams' - set command parameters
    /// ## Sample
    ///
    ///     Yarn.installFlat (fun o ->
    ///                         { o with
    ///                             WorkingDirectory = "./src/FAKESimple.Web/"
    ///                         })
    let installFlat setParams = yarn setParams <| Install Flat

    /// Run `yarn install --har`
    /// ## Parameters
    /// - 'setParams' - set command parameters
    /// ## Sample
    ///
    ///     Yarn.installHar (fun o ->
    ///                        { o with
    ///                            WorkingDirectory = "./src/FAKESimple.Web/"
    ///                        })
    let installHar setParams = yarn setParams <| Install Har

    /// Run `yarn install --no-lockfile`
    /// ## Parameters
    /// - 'setParams' - set command parameters
    /// ## Sample
    ///
    ///     Yarn.installNoLock (fun o ->
    ///                           { o with
    ///                               WorkingDirectory = "./src/FAKESimple.Web/"
    ///                           })
    let installNoLock setParams = yarn setParams <| Install NoLockFile

    /// Run `yarn install --pure-lockfile`
    /// ## Parameters
    /// - 'setParams' - set command parameters
    /// ## Sample
    ///
    ///     Yarn.installPureLock (fun o ->
    ///                             { o with
    ///                                 WorkingDirectory = "./src/FAKESimple.Web/"
    ///                             })
    let installPureLock setParams = yarn setParams <| Install PureLockFile

    /// Run `yarn install --frozen-lockfile`
    /// ## Parameters
    /// - 'setParams' - set command parameters
    /// ## Sample
    ///
    ///     Yarn.installFrozenLockFile (fun o ->
    ///                             { o with
    ///                                 WorkingDirectory = "./src/FAKESimple.Web/"
    ///                             })
    let installFrozenLockFile setParams = yarn setParams <| Install FrozenLockFile

    /// Run `yarn <command>`
    /// ## Parameters
    /// - 'setParams' - set command parameters
    /// ## Sample
    ///
    ///     Yarn.exec "someCommand" (fun o ->
    ///                                { o with
    ///                                    WorkingDirectory = "./src/FAKESimple.Web/"
    ///                                })
    let exec command setParams = yarn setParams <| Custom command
