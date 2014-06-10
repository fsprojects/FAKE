/// Contains tasks to run [RoundhousE](http://projectroundhouse.org/) database migrations.
module Fake.RoundhouseHelper

open Fake
open Fake.ProcessHelper
open System

/// Parameter type to configure the RoundhousE runner
type RoundhouseParams = {

    /// The database you want to create/migrate.
    DatabaseName: string

    /// The directory where your SQL scripts are.
    SqlFilesDirectory: string

    /// The server and instance you would like to run on. (local) and (local)\SQL2008 are both valid values. 
    ServerDatabase: string

    /// As an alternative to ServerName and Database - You can provide an entire connection string instead.
    ConnectionString: string

    /// This is used for connecting to master when you may have a different uid and password than normal.
    ConnectionStringAdmin: string

    /// This is the timeout when commands are run. This is not for admin commands or restore.
    CommandTimeout: int

    /// This is the timeout when administration commands are run (except for restore, which has its own). 
    CommandTimeoutAdmin: int

    /// Database Type (fully qualified class name implementing [roundhouse.sql.Database, roundhouse])
    DatabaseType: string

    /// Output path. Path to where migration artifacts are stored.
    OutputPath: string

    /// Path to the file to use for versioning. Either a .XML file, a .DLL or a .TXT file that a version can be resolved from.
    VersionFile: string

    /// Works in conjunction with an XML version file.
    VersionXPath: string

    /// Path to code repository to be able to correlate versions
    RepositoryPath: string

    /// This allows RH to be environment aware and only run scripts that are in a particular environment based on the namingof the script. LOCAL.something**.ENV.**sql would only be run in the LOCAL environment.
    Environment: string

    /// This instructs RH to use this script for creating a database instead of the default based on the SQLType.
    CustomCreateScript: string

    /// File path of back when Restore is set to true
    RestoreFilePath: string

    /// This instructs RH to remove a database and not run migration scripts.
    Drop: bool

    /// This instructs RH to set the database recovery mode to simple recovery. Only works with SqlServer.
    Simple: bool

    /// This instructs RH to run inside of a transaction.
    WithTransaction: bool

    /// This instructs RH to do a restore (with the restorefrompath parameter) of a database before running migration scripts.
    Restore: bool

    /// Tells RH not to ask for any input when it runs.
    Silent: bool

    /// The name of the folder where you keep your alter database scripts. Read up on token replacement. You will want to use {{DatabaseName}} here instead of specifying a database name.
    AlterDatabaseFolderName: string

    /// The name of the folder where you will keep scripts that ONLY run after a database is created.
    RunAfterCreateDatabaseFolderName: string

    /// The name of the folder where you keep scripts that you want to run before your update scripts.
    RunBeforeUpFolderName: string

    /// The name of the folder where you keep your update scripts.
    UpFolderName: string

    /// The name of the folder where you keep any functions, views, or sprocs that are order dependent. If you have a function that depends on a view, you definitely need the view in this folder.
    RunFirstAfterUpdateFolderName: string

    /// The name of the folder where you keep your functions.
    FunctionsFolderName: string

    /// The name of the folder where you keep your views.
    ViewsFolderName: string

    /// The name of the folder where you keep your stored procedures.
    SprocsFolderName: string

    /// The name of the folder where you keep your indexes.
    IndexesFolderName: string

    /// The name of the folder where you keep scripts that will be run after all of the other any time scripts complete.
    RunAfterOtherAnyTimeScriptsFolderName: string

    /// The name of the folder where you keep your permissions scripts.
    PermissionsFolderName: string

    /// Instructs RH to execute changed one time scripts (DDL/DML in Up folder) that have previously been run against the database instead of failing. A warning is logged for each one time scripts that is rerun.
    WarnOnOneTimeScriptChanges: bool

    /// FileName of the Roundhouse runner.
    ToolPath: string

    /// Working directory (optional).
    WorkingDir: string

    /// A timeout for the runner.
    TimeOut: TimeSpan}

/// Roundhouse default parameters - tries to locate rh.exe in any subfolder.
let RoundhouseDefaults = { 
    DatabaseName = null
    SqlFilesDirectory = null
    ServerDatabase = null
    ConnectionString = null
    ConnectionStringAdmin = null
    CommandTimeout = 60
    CommandTimeoutAdmin = 300
    DatabaseType = null
    OutputPath = null
    VersionFile = null
    VersionXPath = null
    RepositoryPath = null
    Environment = null
    CustomCreateScript = null
    RestoreFilePath = null
    Drop = false
    Simple = false
    WithTransaction = false
    Restore = false
    Silent = true // this overrides the normal RH default
    AlterDatabaseFolderName = null
    RunAfterCreateDatabaseFolderName = null
    RunBeforeUpFolderName = null
    UpFolderName = null
    RunFirstAfterUpdateFolderName = null
    FunctionsFolderName = null
    ViewsFolderName = null
    SprocsFolderName = null
    IndexesFolderName = null
    RunAfterOtherAnyTimeScriptsFolderName = null
    PermissionsFolderName = null
    WarnOnOneTimeScriptChanges = false
    ToolPath = findToolInSubPath "rh.exe" (currentDirectory @@ "tools" @@ "rh")
    WorkingDir = null
    TimeOut = TimeSpan.FromMinutes 5.}

/// This task to can be used to run [RoundhousE](http://projectroundhouse.org/) for database migrations.
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the Roundhouse default parameters.
///
/// ## Sample
///
///     Roundhouse (fun p -> { p with 
///        SqlFilesDirectory = ".\database"
///        ServerDatabase = "(local)"
///        DatabaseName = "atxc"
///        WarnOnOneTimeScriptChanges = true })
///
let Roundhouse setParams = 
    let parameters = setParams RoundhouseDefaults

    let drop = if parameters.Drop then "/drop" else ""
    let simple = if parameters.Simple then "/simple" else ""
    let transaction = if parameters.WithTransaction then "/t" else ""
    let restore = if parameters.Restore then "/restore" else ""
    let silent = if parameters.Silent then "/silent" else ""
    let warn = if parameters.WarnOnOneTimeScriptChanges then "/w" else ""

    let args = sprintf "/d=%s /f=%s /s=%s /cs=%s /csa=%s /ct=%i /cta=%i /dt=%s /o=%s /vf=%s /vx=%s /r=%s /env=%s /cds=%s /rfp=%s /ad=%s /ra=%s /racd=%s /rb=%s /u=%s /rf=%s /fu=%s /vw=%s /sp=%s /ix=%s /p=%s %s %s %s %s %s %s" parameters.DatabaseName parameters.SqlFilesDirectory parameters.ServerDatabase parameters.ConnectionString parameters.ConnectionStringAdmin parameters.CommandTimeout parameters.CommandTimeoutAdmin parameters.DatabaseType parameters.OutputPath parameters.VersionFile parameters.VersionXPath parameters.RepositoryPath parameters.Environment parameters.CustomCreateScript parameters.RestoreFilePath parameters.AlterDatabaseFolderName parameters.RunAfterOtherAnyTimeScriptsFolderName parameters.RunAfterCreateDatabaseFolderName parameters.RunBeforeUpFolderName parameters.UpFolderName parameters.RunFirstAfterUpdateFolderName parameters.FunctionsFolderName parameters.ViewsFolderName parameters.SprocsFolderName parameters.IndexesFolderName parameters.PermissionsFolderName drop simple transaction restore silent warn

    traceStartTask "Roundhouse" args

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut
    then
        failwithf "Roundhouse failed on %s" args
                  
    traceEndTask "Roundhouse" args