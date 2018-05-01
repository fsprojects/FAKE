namespace Fake.JavaScript

open Fake.Core
open Fake.IO
open Fake.Testing.Common
open System
open System.IO

/// Helpers to run the npm tool.
/// 
/// ## Sample
///
///     Npm.install (fun o -> 
///                    { o with 
///                        WorkingDirectory = "./src/FAKESimple.Web/"
///                    })
[<RequireQualifiedAccess>]
module Npm =

    /// Default paths to Npm
    let private npmFileName =
            Process.tryFindFileOnPath "npm"
                |> function
                        | Some npm when File.Exists npm -> npm
                        | _ -> 
                            match Environment.isWindows with
                            | true -> "./packages/Npm.js/tools/npm.cmd"
                            | _ -> "/usr/bin/npm"
                            
    /// Arguments for the Npm install command
    type InstallArgs =
    | Standard
    | Forced

    /// The list of supported Npm commands.
    type NpmCommand =
    | Install of InstallArgs
    | Run of string
    | RunSilent of string
    | RunTest of string
    | Test
    | Custom of string

    /// The Npm parameter type
    type NpmParams = 
        { Src: string
          NpmFilePath: string
          WorkingDirectory: string
          Timeout: TimeSpan }

    /// Npm default parameters
    let defaultNpmParams = 
        { Src = ""
          NpmFilePath = npmFileName
          WorkingDirectory = "."
          Timeout = TimeSpan.MaxValue }

    let private parseInstallArgs = function
        | Standard -> ""
        | Forced -> " --force"

    let private parse = function
        | Install installArgs -> sprintf "install %s" (installArgs |> parseInstallArgs)
        | Run str -> sprintf "run %s" str
        | RunSilent str -> sprintf "run --silent %s" str
        | RunTest str -> sprintf "run --silent %s" str
        | Custom str -> str
        | Test -> "test --silent"

    /// Runs the given process and returns the process result.
    let private execute npmParams command =
        let result = ref None
        let npmPath = Path.GetFullPath(npmParams.NpmFilePath)
        let args = command |> parse
        try 
            let exitCode = 
                Process.execSimple (fun info -> 
                    { info with
                         WorkingDirectory = npmParams.WorkingDirectory
                         FileName = npmPath
                         Arguments = args
                    }
                   ) npmParams.Timeout
            if exitCode <> 0 then result := Some(sprintf "exit code: %d" exitCode)
        with exn ->
            let message = ref exn.Message
            if not (isNull exn.InnerException) then message := !message + Environment.NewLine + exn.InnerException.Message
            result := Some(!message)
        match !result with
        | None -> ()
        | Some msg ->
            match command with
            | RunTest str -> raise (FailedTestsException("Test(s) Failed"))
            | Test -> raise (FailedTestsException("Test(s) Failed"))
            | _ -> failwith msg


    let private npm setParams = defaultNpmParams |> setParams |> execute

  
    /// Run `npm install --force`
    /// ## Parameters
    ///
    /// - 'setParams' - set command parameters
    /// ## Sample
    ///
    ///     Npm.installForced (fun o -> 
    ///                          { o with 
    ///                              WorkingDirectory = "./src/FAKESimple.Web/"
    ///                          })
    let installForced setParams = npm setParams <| Install Forced

    /// Run `npm install`
    /// ## Parameters
    ///
    /// - 'setParams' - set command parameters
    /// ## Sample
    ///
    ///     Npm.install (fun o -> 
    ///                    { o with 
    ///                        WorkingDirectory = "./src/FAKESimple.Web/"
    ///                    })
    let install setParams = npm setParams <| Install Standard 

    /// Run `npm run <command>`
    /// ## Parameters
    ///
    /// - 'command' - command to run
    /// - 'setParams' - set command parameters
    /// ## Sample
    ///
    ///     Npm.run "someCommand" (fun o -> 
    ///                              { o with 
    ///                                  WorkingDirectory = "./src/FAKESimple.Web/"
    ///                              })
    let run command setParams = npm setParams <| Run command
    
    /// Run `npm run --silent <command>`. Suppresses npm error output. See [npm:8821](https://github.com/npm/npm/issues/8821).
    /// ## Parameters
    ///
    /// - 'command' - command to run
    /// - 'setParams' - set command parameters
    /// ## Sample
    ///
    ///     Npm.runSilent "someCommand" (fun o -> 
    ///                                    { o with 
    ///                                        WorkingDirectory = "./src/FAKESimple.Web/"
    ///                                    })
    let runSilent command setParams = npm setParams <| RunSilent command
   
    /// Run `npm run --silent <command>`. Suppresses npm error output and will raise an FailedTestsException exception after the script execution instead of failing, useful for CI. See [npm:8821](https://github.com/npm/npm/issues/8821).
    /// ## Parameters
    ///
    /// - 'command' - command to run
    /// - 'setParams' - set command parameters
    /// ## Sample
    ///
    ///     Npm.runTest "test" (fun o -> 
    ///                           { o with 
    ///                               WorkingDirectory = "./src/FAKESimple.Web/"
    ///                           })
    let runTest command setParams = npm setParams <| RunTest command
   
    /// Run `npm test --silent`. Suppresses npm error output and will raise an FailedTestsException exception after the script execution instead of failing, useful for CI. See [npm:8821](https://github.com/npm/npm/issues/8821).
    /// ## Parameters
    ///
    /// - 'setParams' - set command parameters
    /// ## Sample
    ///
    ///     Npm.test (fun o -> 
    ///                 { o with 
    ///                     WorkingDirectory = "./src/FAKESimple.Web/"
    ///                 })
    let test setParams = npm setParams Test
   
    /// Run `npm <command>`. Used to run any command.
    /// ## Parameters
    ///
    /// - 'command' - command to run
    /// - 'setParams' - set command parameters
    /// ## Sample
    ///
    ///     Npm.exec "--v" (fun o -> 
    ///                       { o with 
    ///                           WorkingDirectory = "./src/FAKESimple.Web/"
    ///                       })
    let exec command setParams = npm setParams <| Custom command