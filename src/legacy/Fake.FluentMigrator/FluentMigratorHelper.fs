/// Contains functions to run FluentMigrator
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
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

/// MS SQL Server driver version
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type SqlServerVersion = 
    | Default
    | V2000
    | V2005
    | V2008
    | V2012
    | V2014
    | CE

/// Oracle database driver version
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type OracleVersion =
    | Default
    | Managed
    | DotConnect

/// Fluent Migrator SQL syntax provider
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
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

/// Database connection configuration
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type DatabaseConnection =
    ///Explicit connection string
    | ConnectionString of connectionString: string * provider: DatabaseProvider
    ///Connection string specified in application config file
    | ConnectionStringFromConfig of name: string * configPath: string * provider: DatabaseProvider

/// Fluent Migrator execution mode
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type MigrationRunningMode =
    /// Execute migrations on the target database
    | Execute of connection: DatabaseConnection
    /// Execute migrations on the target database and script SQL to the output file
    | ExecuteAndScript of connection: DatabaseConnection * outputFileName: string
    /// Execute migrations in preview-only mode
    | Preview of connection: DatabaseConnection
    /// Create migration script
    | Script of startVersion: int64 * outputFileName: string * provider: DatabaseProvider

/// Database operation to execute
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type DatabaseTask =
    | MigrateUp of mode: MigrationRunningMode * version: Option<int64>
    | MigrateDown of mode: MigrationRunningMode * version: int64
    | Rollback of mode: MigrationRunningMode * steps: int
    | ListAppliedMigrations of connection: DatabaseConnection

/// Fluent Migrator options
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
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

/// Default migration options
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
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
    context.Timeout <- Nullable options.Timeout
    context.TransactionPerSession <- options.TransactionPerSession
    context.WorkingDirectory <- options.WorkingDirectory
    match options.Namespace with
        | Some(name, nested) -> 
            context.Namespace <- name
            context.NestedNamespaces <- nested
        | None -> ()
    (context, (snd announcer))

/// Executes the specified task using configuration options
/// ## Parameters
///  - `task` - Database task to execute
///  - `assemblies` - Assembly files which contain migrations
///  - `options` - Migration options which are passed to FluentMigrator
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let ExecuteDatabaseTask task (assemblies: seq<string>) options =
    let (context, writer) = toRunnerContext task assemblies options
    try
        let executor = new TaskExecutor(context)
        executor.Execute()
    finally
        if (writer <> null) then writer.Dispose()

/// Migrates database up to the specified version
/// ## Parameters
///  - `version` - Target version
///  - `connection` - Database connection
///  - `assemblies` - Assembly files which contain migrations
///  - `options` - Migration options which are passed to FluentMigrator
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let MigrateUp version connection assemblies options = 
    let task = MigrateUp(Execute(connection), Some(version))
    ExecuteDatabaseTask task assemblies options

/// Migrates database up to the latest version
/// ## Parameters
///  - `connection` - Database connection
///  - `assemblies` - Assembly files which contain migrations
///  - `options` - Migration options which are passed to FluentMigrator
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let MigrateToLatest connection assemblies options = 
    let task = DatabaseTask.MigrateUp(Execute(connection), None)
    ExecuteDatabaseTask task assemblies options

/// Migrates database up to the specified version
/// ## Parameters
///  - `version` - Target version
///  - `connection` - Database connection
///  - `assemblies` - Assembly files which contain migrations
///  - `options` - Migration options which are passed to FluentMigrator
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let MigrateDown version connection assemblies options =
    let task = MigrateDown(Execute(connection), version)
    ExecuteDatabaseTask task assemblies options

/// Rollbacks several applied migrations
/// ## Parameters
///  - `steps` - Number of migrations to rollback
///  - `connection` - Database connection
///  - `assemblies` - Assembly files which contain migrations
///  - `options` - Migration options which are passed to FluentMigrator
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let Rollback steps connection assemblies options =
    let task = Rollback(Execute(connection), steps)
    ExecuteDatabaseTask task assemblies options

/// Rollbacks latest applied migration
///  - `connection` - Database connection
///  - `assemblies` - Assembly files which contain migrations
///  - `options` - Migration options which are passed to FluentMigrator
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let RollbackLatest connection assemblies options = 
    Rollback 1 connection assemblies options

/// Lists all migrations which were applied to the database
///  - `connection` - Database connection
///  - `assemblies` - Assembly files which contain migrations
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let ListAppliedMigrations connection assemblies =
    let task = ListAppliedMigrations(connection)
    ExecuteDatabaseTask task assemblies DefaultMigrationOptions
