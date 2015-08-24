[<AutoOpen>]
/// Contains functions to run FluentMigrator database migrations
module Fake.FluentMigratorHelper

open System
open System.IO
open Fake
open Fake.TraceHelper
open FluentMigrator.Runner
open FluentMigrator.Runner.Initialization
open FluentMigrator.Runner.Announcers

let private horizontalRule = "".PadRight(79, '-')

type internal FakeAnnouncer() =
    inherit Announcer()

    override this.Say message =
        base.Say(sprintf "[+] %s" message);

    override this.Heading message =
        trace horizontalRule
        trace message
        trace horizontalRule

    override this.Emphasize message = 
        traceImportant (sprintf "[+] %s" message)

    override this.Error message =
        traceError (sprintf "!!! %s" message)

    override this.Write (message, escaped) =
        log message

/// <summary>MS SQL Server driver version</summary>
type SqlServerVersion =
    | Default
    | V2000
    | V2005
    | V2008
    | V2012
    | V2014
    | CE

/// <summary>Oracle database driver version</summary>
type OracleVersion =
    | Default
    | Managed
    | DotConnect

/// <summary>Fluent Migrator SQL syntax provider</summary>
type DatabaseProvider =
    | SqlServer of version: SqlServerVersion
    | Oracle of version: OracleVersion
    | DB2
    | Firebird
    | HANA
    | Jet
    | MySql
    | PostgreSQL
    | SQLite

///<summary>Database connection configuration</summary>
type DatabaseConnection =
    ///<summary>Explicit connection string</summary>
    | ConnectionString of connectionString: string * provider: DatabaseProvider
    ///<summary>Connection string specified in application config file</summary>
    | ConnectionStringFromConfig of name: string * configPath: string * provider: DatabaseProvider

///<summary>Fluent Migrator execution mode</summary>
type MigrationRunningMode =
    ///<summary>Execute migrations on the target database</summary>
    | Execute of connection: DatabaseConnection
    ///<summary>Execute migrations on the target database and script SQL to the output file</summary>
    | ExecuteAndScript of connection: DatabaseConnection * outputFileName: string
    ///<summary>Execute migrations in preview-only mode</summary>
    | Preview of connection: DatabaseConnection
    ///<summary>Create migration script</summary>
    | Script of startVersion: int64 * outputFileName: string * provider: DatabaseProvider

/// <summary>Database operation to execute</summary>
type DatabaseTask =
    | MigrateUp of mode: MigrationRunningMode * version: Option<int64>
    | MigrateDown of mode: MigrationRunningMode * version: int64
    | Rollback of mode: MigrationRunningMode * steps: int
    | ListAppliedMigrations of connection: DatabaseConnection

/// <summary>Fluent Migrator options</summary>
type MigrationOptions = {
    Namespace: Option<string * bool>;
    Profile: string;
    Tags: seq<string>;
    Timeout: int;
    Context: System.Object;
    TransactionPerSession: bool;
    ProviderSwitches: string;
    WorkingDirectory: string;
    Verbose: bool
}

/// <summary>Default migration options</summary>
let DefaultMigrationOptions = {
    Namespace = None;
    Profile = null;
    Tags = [];
    Timeout = 30;
    Context = null;
    TransactionPerSession = false;
    ProviderSwitches = null;
    WorkingDirectory = null;
    Verbose = false;
}

let private getProviderName provider =
    match provider with
        | SqlServer(SqlServerVersion.Default) -> "sqlserver"
        | SqlServer(V2000) -> "sqlserver2000"
        | SqlServer(V2005) -> "sqlserver2005"
        | SqlServer(V2008) -> "sqlserver2008"
        | SqlServer(V2012) -> "sqlserver2012"
        | SqlServer(V2014) -> "sqlserver2014"
        | SqlServer(CE) -> "sqlserverce"
        | Oracle(OracleVersion.Default) -> "oracle"
        | Oracle(Managed) -> "oraclemanaged"
        | Oracle(DotConnect) -> "dotconnectoracle"
        | DB2 -> "db2"
        | Firebird -> "firebird"
        | HANA -> "hana"
        | Jet -> "jet"
        | MySql -> "mysql"
        | PostgreSQL -> "postgres"
        | SQLite -> "sqlite"

let private validate assemblies =
    if ((assemblies = null) || (Seq.isEmpty assemblies))
        then invalidOp "At least one migration assembly should be specified"

let private createAnnouncer outputFileName verbose provider = 
    let fakeAnnouncer = new FakeAnnouncer()
    fakeAnnouncer.ShowSql <- verbose
    fakeAnnouncer.ShowElapsedTime <- verbose
    if (String.IsNullOrEmpty(outputFileName)) then
        ((fakeAnnouncer :> IAnnouncer), null)
    else
        let sw = new StreamWriter(outputFileName)
        let fileAnnouncer = 
            match provider with
                | SqlServer(_) -> (new TextWriterWithGoAnnouncer(sw) :> TextWriterAnnouncer)
                | _ -> (new TextWriterAnnouncer(sw)) 
        fileAnnouncer.ShowElapsedTime <- false
        fileAnnouncer.ShowSql <- true
        ((new CompositeAnnouncer(fakeAnnouncer, fileAnnouncer) :> IAnnouncer), sw)

let private getModeFromTask task = 
    match task with
        | MigrateUp(mode, _)
        | MigrateDown(mode, _)  
        | Rollback(mode, _) -> Some(mode)
        | _ -> None

let private getProviderFromConnection connection = 
    match connection with
        | ConnectionString(_, provider)
        | ConnectionStringFromConfig(_, _, provider) ->
            provider

let private getProviderFromMode mode =
    match mode with 
        | Execute(connection)
        | ExecuteAndScript(connection, _)
        | Preview(connection) -> getProviderFromConnection connection
        | Script(_, _, provider) -> provider

let private getProviderFromTask task =
    match task with
        | MigrateUp(mode, _)
        | MigrateDown(mode, _)  
        | Rollback(mode, _) -> getProviderFromMode mode
        | ListAppliedMigrations(connection) -> getProviderFromConnection connection

let private setupConnection (context: IRunnerContext) connection = 
    match connection with
        | ConnectionString(connectionString, _) -> 
            context.Connection <- connectionString
        | ConnectionStringFromConfig(name, configPath, _) ->
            context.Connection <- name
            context.ConnectionStringConfigPath <- configPath

let private setupConnectionForMode (context: IRunnerContext) mode =
    match mode with 
        | Execute(connection)
        | ExecuteAndScript(connection, _) ->
            setupConnection context connection
        | Preview(connection) -> 
            setupConnection context connection
            context.PreviewOnly <- true
        | Script(startVersion, _, _) ->
            context.NoConnection <- true
            context.StartVersion <- startVersion

let private toRunnerContext task assemblies options = 
    validate assemblies
    let provider = getProviderFromTask task
    let announcerFactory = 
        match getModeFromTask task with
            | Some(ExecuteAndScript(_, outputFileName)) ->
                createAnnouncer outputFileName
            | Some(Script(_, outputFileName, _)) ->
                createAnnouncer outputFileName
            | _ ->
                createAnnouncer null
    let announcer = announcerFactory options.Verbose provider
    let context = new RunnerContext(fst announcer)
    context.Database <- getProviderName provider
    match task with
        | MigrateUp(mode, None) ->
            context.Task <- "migrate"
            setupConnectionForMode context mode
        | MigrateUp(mode, Some(version)) -> 
            context.Task <- "migrate"
            setupConnectionForMode context mode
            context.Version <- version
        | MigrateDown(mode, version) ->
            context.Task <- "migrate:down"
            setupConnectionForMode context mode
            context.Version <- version
        | Rollback(mode, steps) -> 
            context.Task <- "rollback"
            setupConnectionForMode context mode
            context.Steps <- steps
        | ListAppliedMigrations(connection) -> 
            context.Task <- "ListAppliedMigrations"
            setupConnection context connection
    context.Targets <- Seq.toArray assemblies
    context.Tags <- Seq.toList options.Tags
    context.ApplicationContext <- options.Context
    context.Profile <- options.Profile
    context.ProviderSwitches <- options.ProviderSwitches
    context.Timeout <- options.Timeout
    context.TransactionPerSession <- options.TransactionPerSession
    context.WorkingDirectory <- options.WorkingDirectory
    match options.Namespace with
        | Some(name, nested) -> 
            context.Namespace <- name
            context.NestedNamespaces <- nested
        | None -> ()
    (context, (snd announcer))

/// <summary>Executes the specified task using configuration options</summary>
/// <param name="task"><see cref="DatabaseTask"/> to execute</param>
/// <param name="assemblies">Assembly files which contain migrations</param>
/// <param name="options"><see cref="MigrationOptions"/>options</param>
let ExecuteDatabaseTask task (assemblies: seq<string>) options =
    let (context, writer) = toRunnerContext task assemblies options
    try
        let executor = new TaskExecutor(context)
        executor.Execute()
    finally
        if (writer <> null) then writer.Dispose()

/// <summary>Migrates database up to the specified version</summary>
/// <param name="version">Version to migrate to (use None for the latest available version).</param>
/// <param name="connection">Database connection</param>
/// <param name="assemblies">Assembly files which contain migrations</param>
/// <param name="options"><see cref="MigrationOptions"/>options</param>
let MigrateUp version connection assemblies options = 
    let task = MigrateUp(Execute(connection), Some(version))
    ExecuteDatabaseTask task assemblies options

/// <summary>Migrates database up to the latest version</summary>
/// <param name="connection">Database connection</param>
/// <param name="assemblies">Assembly files which contain migrations</param>
/// <param name="options"><see cref="MigrationOptions"/>options</param>
let MigrateToLatest connection assemblies options = 
    let task = DatabaseTask.MigrateUp(Execute(connection), None)
    ExecuteDatabaseTask task assemblies options

/// <summary>Migrates database up to the specified version</summary>
/// <param name="version">Version to migrate to</param>
/// <param name="connection">Database connection</param>
/// <param name="assemblies">Assembly files which contain migrations</param>
/// <param name="options"><see cref="MigrationOptions"/>options</param>
let MigrateDown version connection assemblies options =
    let task = MigrateDown(Execute(connection), version)
    ExecuteDatabaseTask task assemblies options

/// <summary>Rollbacks several applied migrations</summary>
/// <param name="steps">Number of migrations to revert</param>
/// <param name="connection">Database connection</param>
/// <param name="assemblies">Assembly files which contain migrations</param>
/// <param name="options"><see cref="MigrationOptions"/>options</param>
let Rollback steps connection assemblies options =
    let task = Rollback(Execute(connection), steps)
    ExecuteDatabaseTask task assemblies options

/// <summary>Rollbacks latest applied migration</summary>
/// <param name="connection">Database connection</param>
/// <param name="assemblies">Assembly files which contain migrations</param>
/// <param name="options"><see cref="MigrationOptions"/>options</param>
let RollbackLatest connection assemblies options = 
    Rollback 1 connection assemblies options

/// <summary>Lists all migrations which were applied to the database</summary>
/// <param name="connection">Database connection</param>
/// <param name="assemblies">Assembly files which contain migrations</param>
let ListAppliedMigrations connection assemblies =
    let task = ListAppliedMigrations(connection)
    ExecuteDatabaseTask task assemblies DefaultMigrationOptions