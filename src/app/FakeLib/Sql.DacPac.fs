/// Contains helpers around deploying databases.
module Fake.Sql.DacPac

open Fake.EnvironmentHelper
open Fake.ProcessHelper
open System.IO

/// The type of action to execute.
type DeployAction =
    /// Generate and apply a synchronisation script between two databases.
    | Deploy
    /// Generate a SQL script to sync two databases.
    | Script of OutputPath:string
    /// Generate an XML report for the differences between two databases.
    | Report of OutputPath:string

/// Configuration arguments for DacPac deploy
type DeployDbArgs = {
    /// The path to SqlPackage.exe.
    SqlPackageToolPath : string
    /// Type of action to execute. Defaults to Deploy.
    Action : DeployAction
    /// Path to source (path to DACPAC or Connection String).
    Source : string
    /// Path to destination (path to DACPAC or Connection String).
    Destination : string
    /// Timeout for deploy (defaults to 120 seconds).
    Timeout : int
    /// Block deployment if data loss can occur. Defaults to true.
    BlockOnPossibleDataLoss : bool
    /// Drops objects in the destination that do not exist in the source. Defaults to false.
    DropObjectsNotInSource : bool }

let pathsToCheck =
    [ ProgramFilesX86 </> @"Microsoft SQL Server\130\DAC\bin\SqlPackage.exe"
      ProgramFilesX86 </> @"Microsoft Visual Studio 14.0\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\130\SqlPackage.exe" ]

/// The default DacPac deployment arguments.
let defaultDeploymentArgs = 
    { SqlPackageToolPath = 
        pathsToCheck
        |> List.tryFind File.Exists
        |> function
        | Some path -> path
        | None -> ""
      Action = Deploy
      Source = ""
      Destination = ""
      Timeout = 120
      BlockOnPossibleDataLoss = true
      DropObjectsNotInSource = false }

let private generateCommandLine args =
    let action, outputPath =
        match args with
        | Deploy -> "Publish", None
        | Script outputPath -> "Script", Some outputPath
        | Report outputPath -> "DeployReport", Some outputPath
    let outputPath = defaultArg(outputPath |> Option.map(sprintf """/OutputPath:"%s" """)) ""
    action, outputPath

/// Deploys a SQL DacPac or database to another database or DacPac.
let deployDb setParams =
    let args = setParams defaultDeploymentArgs
    let action, outputPath = generateCommandLine args.Action

    if System.String.IsNullOrWhiteSpace args.SqlPackageToolPath then
        failwith "No SqlPackage.exe filename was given."

    if not (File.Exists args.SqlPackageToolPath) then
        failwithf "Unable to find a valid instance of SqlPackage.exe. Paths checked were: %A." pathsToCheck
          
    shellExec {
        Program = args.SqlPackageToolPath
        CommandLine = sprintf """/Action:%s /SourceFile:"%s" /TargetConnectionString:"%s" %s /p:BlockOnPossibleDataLoss=%b /p:DropObjectsNotInSource=%b /p:CommandTimeout=%d""" action args.Source args.Destination outputPath args.BlockOnPossibleDataLoss args.DropObjectsNotInSource args.Timeout
        WorkingDirectory = ""
        Args = [] }

    |> function
    | 0 -> ()
    | _ -> failwith "Error executing DACPAC deployment. Please see output for error details."

