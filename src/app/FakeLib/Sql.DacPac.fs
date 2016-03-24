/// Contains helpers around deploying databases.
module Fake.Sql.DacPac

open Fake.EnvironmentHelper
open Fake.ProcessHelper

/// The type of action to execute.
type DeployAction =
    /// Generate and apply a synchronisation between two databases.
    | Deploy
    /// Generate a SQL script to sync two databases.
    | Script
    /// Generate an XML report for the differences between two databases.
    | Report

/// Configuration arguments for DacPac deploy
type DeployDbArgs = {
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

/// The default DacPac deployment arguments.
let defaultDeploymentArgs = { Action = Deploy; Source = ""; Destination = ""; Timeout = 120; BlockOnPossibleDataLoss = true; DropObjectsNotInSource = false }

/// Deploys a SQL DacPac or database to another database or DacPac.
let deployDb modifier =
    let args = modifier defaultDeploymentArgs
    shellExec {        
        Program = sprintf @"%s\IIS\Microsoft Web Deploy V3\MSDeploy.exe" ProgramFiles
        CommandLine = sprintf """-verb:sync -source:dbDacFx="%s" -dest:dbDacFx="%s",DacPacAction=%A,BlockOnPossibleDataLoss=%b,DropObjectsNotInSource=%b,CommandTimeout=%d""" args.Source args.Destination args.Action args.BlockOnPossibleDataLoss args.DropObjectsNotInSource args.Timeout
        WorkingDirectory = ""
        Args = [] }

