namespace Fake.Sql

/// Contains helpers around interacting with SQL Server databases.
[<RequireQualifiedAccess>]
module SqlServer =

    open System
    open System.IO
    open System.Data.SqlClient
    open Microsoft.SqlServer.Management.Smo
    open Microsoft.SqlServer.Management.Common

    open Fake.Core
    open Fake.IO
    open Fake.IO.FileSystemOperators

    type ServerInfo = {
        Server: Server
        ConnBuilder: SqlConnectionStringBuilder }

    module ServerInfo =
        let create connectionString =
            let connBuilder = new SqlConnectionStringBuilder(connectionString)
            let conn = new ServerConnection()
            if not <| String.IsNullOrWhiteSpace(connBuilder.UserID) then
                conn.LoginSecure <- false
                conn.Login <- connBuilder.UserID

            if not <| String.IsNullOrWhiteSpace(connBuilder.Password) then
                conn.LoginSecure <- false
                conn.Password <- connBuilder.Password

            conn.ServerInstance <- connBuilder.DataSource
            conn.Connect()

            { Server = new Server(conn); ConnBuilder = connBuilder }

    /// Gets the `Database`s from the database server.
    let getDatabasesFromServer (serverInfo:ServerInfo) =
        seq { for db in serverInfo.Server.Databases -> db }

    /// Gets the Database names from the database server.
    let getDatabaseNamesFromServer (serverInfo:ServerInfo) =
        serverInfo
        |> getDatabasesFromServer
        |> Seq.map (fun db -> db.Name)

    /// Get the name of the InitialCatalog
    let getInitialCatalog serverInfo = serverInfo.ConnBuilder.InitialCatalog

    /// Gets the name or network address of the instance of SQL Server.
    let getServerName serverInfo = serverInfo.ConnBuilder.DataSource

    /// Checks that the specified `dbName` exists on the server.
    let databaseExistsOnServer serverInfo dbName =
        let names = getDatabaseNamesFromServer serverInfo
        let searched = getInitialCatalog serverInfo
        Trace.tracefn "Searching for database [%s] on server [%s]. Found: " searched (getServerName serverInfo)
        names
        |> Seq.iter (Trace.tracefn " - [%s] ")

        names
        |> Seq.exists ((=) dbName)

    /// Gets the Initial Catalog as a `Database` instance
    let getDatabase serverInfo = new Database(serverInfo.Server, getInitialCatalog serverInfo)

    /// Checks the specified initial catalog exists on the database server.
    let initialCatalogExistsOnServer serverInfo =
        getInitialCatalog serverInfo
        |> databaseExistsOnServer serverInfo

    /// Drops the database if it exists.
    let dropDatabase serverInfo =
        if initialCatalogExistsOnServer serverInfo then
            let initialCatalog = getInitialCatalog serverInfo
            Trace.logfn "Dropping database [%s] on server [%s]." initialCatalog (getServerName serverInfo)
            (getDatabase serverInfo).DropBackupHistory |> ignore
            initialCatalog |> serverInfo.Server.KillDatabase

    /// Kills all processes on the Initial Catalog.
    let killAllProcesses serverInfo =
        let initialCatalog = getInitialCatalog serverInfo
        Trace.logfn "Killing all processes from database [%s] on server [%s]." initialCatalog (getServerName serverInfo)
        serverInfo.Server.KillAllProcesses initialCatalog

    /// Detaches the Initial Catalog database.
    let detachDatabase serverInfo =
        killAllProcesses serverInfo
        let initialCatalog = getInitialCatalog serverInfo
        Trace.logfn "Detaching database [%s] on server [%s]." initialCatalog (getServerName serverInfo)
        serverInfo.Server.DetachDatabase(initialCatalog, true)

    /// Attaches a database that is made up of one or more files as the Initial Catalog database, and throws when any file does not exist.
    let attach serverInfo (attachOptions:AttachOptions) files =
        let sc = new Collections.Specialized.StringCollection()
        files |> Seq.iter (fun file ->
            sc.Add file |> ignore
            File.checkExists file)

        let initialCatalog = getInitialCatalog serverInfo

        Trace.logfn "Attaching database [%s] on server [%s]." initialCatalog (getServerName serverInfo)
        serverInfo.Server.AttachDatabase(initialCatalog, sc, attachOptions)

    /// Creates the Initial Catalog database on the server.
    let createDatabase serverInfo =
        Trace.logfn "Creating database [%s] on server [%s]." (getInitialCatalog serverInfo) (getServerName serverInfo)
        (getDatabase serverInfo).Create()

    /// Runs the sql file on the database.
    let runScript serverInfo sqlFile =
        Trace.logfn "Executing script %s." sqlFile
        sqlFile
        |> File.readAsString
        |> (getDatabase serverInfo).ExecuteNonQuery

    /// Closes the connection to the database server.
    let disconnect serverInfo =
        Trace.logfn "Disconnecting from server [%s]." (getServerName serverInfo)
        if isNull serverInfo.Server then
            failwith "Server is not configured."
        if isNull serverInfo.Server.ConnectionContext then
            failwith "Server.ConnectionContext is not configured."
        serverInfo.Server.ConnectionContext.Disconnect()

    /// Replaces the database files given some files given by a copying function `copyF`.
    let internal replaceDatabaseFilesF connectionString attachOptions copyF =
        let si = ServerInfo.create connectionString

        if databaseExistsOnServer si (getInitialCatalog si)
        then detachDatabase si

        copyF() |> attach si attachOptions

        disconnect si

    /// Replaces the database files with one or more files.
    let replaceDatabaseFiles connectionString targetDir files attachOptions =
        replaceDatabaseFilesF connectionString attachOptions
            (fun _ ->
                files
                |> Seq.map (fun fileName ->
                    let fi = new FileInfo(fileName)
                    Shell.copyFile targetDir fileName
                    targetDir @@ fi.Name))

    /// Replaces the database files with one or more files, and if the files are not cached
    /// or the original files have a different write time, the cache will refresh.
    let replaceDatabaseFilesWithCache connectionString targetDir cacheDir files attachOptions =
        replaceDatabaseFilesF connectionString attachOptions
            (fun _ -> Shell.copyCached targetDir cacheDir files)

    /// Drops and creates the database indicated by the connection string.
    let dropAndCreateDatabase connectionString =
        let si = ServerInfo.create connectionString
        dropDatabase si
        createDatabase si
        disconnect si

    /// Runs each sql file on the database.
    let runScripts connectionString scripts =
        let serverInfo = ServerInfo.create connectionString
        scripts |> Seq.iter (runScript serverInfo)
        disconnect serverInfo

    /// Run every *.sql file in the directory on the database.
    let runScriptsFromDirectory connectionString scriptDirectory =
        System.IO.Directory.GetFiles(scriptDirectory, "*.sql")
        |> runScripts connectionString