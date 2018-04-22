/// Contains helpers around deploying databases.
[<System.Obsolete("Please open Fake.Sql instead and use the Fake.Sql.DacPac module (FAKE0001 - package: Fake.Sql.DacPac, module: DacPac)")>]
module Fake.Sql.DacPac

open Fake.EnvironmentHelper
open Fake.ProcessHelper
open System.IO
open Fake.FileSystem
open System.Diagnostics
open System

/// The type of action to execute.
[<System.Obsolete("FAKE0001 Use `open Fake.Sql` and `DacPac.DeployAction`")>]
type DeployAction =
    /// Generate and apply a synchronisation script between two databases.
    | Deploy
    /// Generate a SQL script to sync two databases.
    | Script of OutputPath:string
    /// Generate an XML report for the differences between two databases.
    | Report of OutputPath:string

/// Configuration arguments for DacPac deploy
[<System.Obsolete("FAKE0001 Use `open Fake.Sql` and `DacPac.DeployDbArgs`")>]
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
    DropObjectsNotInSource : bool
    /// Recreates the database from scratch on publish (rather than an in-place update). Defaults to false.
    RecreateDb : bool
    /// Additional configuration parameters required by sqlpackage.exe
    AdditionalSqlPackageProperties : (string * string) list
    /// SQLCMD variables
    Variables : (string * string) list }

[<System.Obsolete("FAKE0001 Use `open Fake.Sql` and `DacPac.validPaths`")>]
let validPaths =
    let getSqlVersion (path:string) = path.Split '\\' |> Array.item 3 |> int
    let getVsVersion path = (Path.GetDirectoryName path |> DirectoryInfo).Name |> int
    let sql = !!(ProgramFilesX86 </> @"Microsoft SQL Server\**\DAC\bin\SqlPackage.exe") |> Seq.map(fun path -> path, getSqlVersion path)
    let vs = !!(ProgramFilesX86 </> @"Microsoft Visual Studio*\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\*\SqlPackage.exe") |> Seq.map(fun path -> path, getVsVersion path)

    [ sql; vs ]
    |> List.collect Seq.toList
    |> List.sortByDescending snd
    |> List.map fst

/// The default DacPac deployment arguments.
[<System.Obsolete("FAKE0001 Use `open Fake.Sql` and `DacPac.DefaultDeploymentArgs`")>]
let defaultDeploymentArgs = 
    { SqlPackageToolPath = 
        validPaths
        |> List.tryHead
        |> defaultArg <| ""
      Action = Deploy
      Source = ""
      Destination = ""
      Timeout = 120
      BlockOnPossibleDataLoss = true
      DropObjectsNotInSource = false
      RecreateDb = false
      AdditionalSqlPackageProperties = []
      Variables = [] }

module PropertyKeys =
    /// When creating a new SQL Azure database, specifies the database service tier to use e.g. S2, P1
    let sqlAzureDbSize = "DatabaseServiceObjective"

let private generateCommandLine args =
    let action, outputPath =
        match args with
        | Deploy -> "Publish", None
        | Script outputPath -> "Script", Some outputPath
        | Report outputPath -> "DeployReport", Some outputPath
    let outputPath = defaultArg(outputPath |> Option.map(sprintf """/OutputPath:"%s" """)) ""
    action, outputPath

/// Deploys a SQL DacPac or database to another database or DacPac.
[<System.Obsolete("FAKE0001 Use `open Fake.Sql` and `DacPac.deployDb`")>]
let deployDb setParams =
    let args = setParams defaultDeploymentArgs
    let action, outputPath = generateCommandLine args.Action

    let concat parameter =
        List.map (fun (key, value) -> sprintf "/%s:%s=%s" parameter key value)
        >> String.concat " "

    let additionalParameters = args.AdditionalSqlPackageProperties |> concat "p"

    let variables = args.Variables |> concat "v"

    if System.String.IsNullOrWhiteSpace args.SqlPackageToolPath then
        failwith "No SqlPackage.exe filename was given."

    if not (File.Exists args.SqlPackageToolPath) then
        let paths =
            if validPaths |> List.contains args.SqlPackageToolPath then validPaths
            else [ args.SqlPackageToolPath ]
        failwithf "Unable to find a valid instance of SqlPackage.exe. Paths checked were: %A." paths
    
    let result =
        ExecProcessWithLambdas
            (fun psi ->
                psi.Arguments <- sprintf """/Action:%s /SourceFile:"%s" /TargetConnectionString:"%s" %s /p:BlockOnPossibleDataLoss=%b /p:DropObjectsNotInSource=%b /p:CommandTimeout=%d /p:CreateNewDatabase=%b %s %s""" action args.Source args.Destination outputPath args.BlockOnPossibleDataLoss args.DropObjectsNotInSource args.Timeout args.RecreateDb additionalParameters variables
                psi.FileName <- args.SqlPackageToolPath)
            TimeSpan.MaxValue
            true
            (printfn "SqlPackage error: %s")
            (printfn "%s")

    match result with
    | 0 -> ()
    | _ -> failwith "Error executing DACPAC deployment. Please see output for error details."

