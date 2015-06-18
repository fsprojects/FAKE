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

type SqlServerVersion =
    | Default
    | V2000
    | V2005
    | V2008
    | V2012
    | V2014
    | CE

type OracleVersion =
    | Default
    | Managed
    | DotConnect

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

type DatabaseTask =
    | MigrateUp of version : Option<int64>
    | MigrateDown of version: int64
    | Rollback of steps : int
    | ListMigrations
    | ValidateVersionOrder

type DatabaseConnection =
    | ConnectionString of connectionString: string
    | ConfigConnection of name: string * configPath: string

type MigrationRunningMode =
    | Execute of connection: DatabaseConnection
    | ExecuteAndScript of connection: DatabaseConnection * outputFileName: string
    | Preview of connection: DatabaseConnection
    | Script of startVersion: int64 * outputFileName: string

type MigrationOptions = {
    Mode: Option<MigrationRunningMode>;
    Assemblies: seq<string>
    Namespace: Option<string * bool>;
    Provider: Option<DatabaseProvider>;
    Profile: string;
    Tags: seq<string>;
    Timeout: int;
    Context: System.Object;
    TransactionPerSession: bool;
    ProviderSwitches: string;
    WorkingDirectory: string;
    Verbose: bool
}

let DefaultMigrationOptions = {
    Mode = None
    Assemblies = null;
    Namespace = None;
    Provider = None
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

let private validate options =
    match options.Mode with
        | None -> invalidOp "Migration mode with database connection is required"
        | _ -> ()
    match options.Provider with
        | None -> invalidOp "Database provider is required"
        | _ -> ()
    if ((options.Assemblies = null) || (Seq.isEmpty options.Assemblies))
        then invalidOp "At least one migration assembly should be specified"

let private toRunnerContext task options = 
    validate options
    let provider = options.Provider.Value
    let mode = options.Mode.Value
    let createAnnouncer (outputFileName: string) = 
        let fakeAnnouncer = new FakeAnnouncer()
        fakeAnnouncer.ShowSql <- options.Verbose
        fakeAnnouncer.ShowElapsedTime <- options.Verbose
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
    let announcer = 
        match mode with
            | ExecuteAndScript(_, outputFileName) ->
                createAnnouncer outputFileName
            | Script(_, outputFileName) ->
                createAnnouncer outputFileName
            | _ ->
                createAnnouncer null
    let context = new RunnerContext(fst announcer)
    context.Targets <- Seq.toArray options.Assemblies
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
        | None -> "nothing" |> ignore
    match mode with
        | Execute(ConnectionString(connectionString)) ->
            context.Connection <- connectionString
        | Execute(ConfigConnection(name, configPath)) ->
            context.Connection <- name
            context.ConnectionStringConfigPath <- configPath
        | Preview(ConnectionString(connectionString)) ->
            context.Connection <- connectionString
            context.PreviewOnly <- true
        | Preview(ConfigConnection(name, configPath)) ->
            context.Connection <- name
            context.ConnectionStringConfigPath <- configPath
            context.PreviewOnly <- true
        | ExecuteAndScript(ConnectionString(connectionString), outputFileName) ->
            context.Connection <- connectionString
        | ExecuteAndScript(ConfigConnection(name, configPath), outputFileName) ->
            context.Connection <- name
            context.ConnectionStringConfigPath <- configPath
        | Script(startVersion, outputFileName) ->
            context.NoConnection <- true
            context.StartVersion <- startVersion
    match task with
        | MigrateUp(None) ->
            context.Task <- "migrate"
        | MigrateUp(Some(version)) -> 
            context.Task <- "migrate"
            context.Version <- version
        | MigrateDown(version) ->
            context.Task <- "migrate:down"
            context.Version <- version
        | Rollback(steps) -> 
            context.Task <- "rollback"
            context.Steps <- steps
        | ListMigrations -> 
            context.Task <- "listmigrations"
        | ValidateVersionOrder ->
            context.Task <- "validateversionorder"
    context.Database <- getProviderName provider
    (context, (snd announcer))

let ExecuteDatabaseTask task options =
    let (context, writer) = toRunnerContext task options
    try
        let executor = new TaskExecutor(context)
        executor.Execute()
    finally
        if (writer <> null) then writer.Dispose()

let MigrateUp version assemblyPath connection database =
    let task = DatabaseTask.MigrateUp(version)
    let options = { 
        DefaultMigrationOptions with
            Assemblies = [assemblyPath];
            Mode = Some(Execute(connection));
            Provider = Some(database)
    }
    ExecuteDatabaseTask task options

let MigrateToLatest = MigrateUp None

let ScriptUp version startVersion assemblyPath outputFileName database = 
    let task = DatabaseTask.MigrateUp(version)
    let options = { 
        DefaultMigrationOptions with
            Assemblies = [assemblyPath];
            Mode = Some(Script(startVersion, outputFileName));
            Provider = Some(database)
    }
    ExecuteDatabaseTask task options

let ScriptAll = ScriptUp None 1L

let Rollback steps assemblyPath connection database =
    let task = DatabaseTask.Rollback(steps)
    let options = { 
        DefaultMigrationOptions with
            Assemblies = [assemblyPath];
            Mode = Some(Execute(connection));
            Provider = Some(database)
    }
    ExecuteDatabaseTask task options

let RollbackToPrevious = Rollback 1