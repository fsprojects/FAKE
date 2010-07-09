namespace Fake

open System
open System.Data.SqlClient
open Microsoft.SqlServer.Management.Smo
open Microsoft.SqlServer.Management.Common
open System.IO

type ServerInfo =
  { Server: Server;
    ConnBuilder: SqlConnectionStringBuilder}
    
[<AutoOpen>]
module SqlServerSmoHelper =   
  /// Gets a connection to the SQL server and an instance to the ConnectionStringBuilder
  let getServerInfo connectionString = 
    let connbuilder = new SqlConnectionStringBuilder(connectionString)
    let conn = new ServerConnection()
    if connbuilder.UserID <> "" then
        conn.LoginSecure <- false
        conn.Login <- connbuilder.UserID
        
    if connbuilder.Password <> "" then
        conn.LoginSecure <- false
        conn.Password <- connbuilder.Password
        
    conn.ServerInstance <- connbuilder.DataSource
    conn.Connect()
    
    let server = new Server(conn)    
    {Server = server; ConnBuilder = connbuilder}
    
  /// gets the DatabaseNames from the server
  let getDatabaseNamesFromServer (serverInfo:ServerInfo) = 
    seq {for db in serverInfo.Server.Databases -> db.Name}
                    
  /// Checks wether the given Database exists on the server
  let existDBOnServer serverInfo dbName = 
    serverInfo
      |> getDatabaseNamesFromServer
      |> Seq.exists (fun d -> d = dbName)
      
  /// Gets the name of the sercer
  let getServerName serverInfo = serverInfo.ConnBuilder.DataSource   
  
  /// Gets the initial catalog name
  let getDBName serverInfo = serverInfo.ConnBuilder.InitialCatalog 
  
  /// Gets the initial catalog as database instance
  let getDatabase serverInfo =
    new Database(serverInfo.Server,getDBName serverInfo )
      
  /// Checks wether the given InitialCatalog exists on the server    
  let intitialCatalogExistsOnServer serverInfo =  
    getDBName serverInfo |> existDBOnServer serverInfo  
  
  /// Drops the given InitialCatalog from the server (if it exists)
  let DropDb serverInfo = 
    if intitialCatalogExistsOnServer serverInfo then
      log <| sprintf "Dropping database %s on server %s" (getDBName serverInfo) (getServerName serverInfo)
      (getDatabase serverInfo).DropBackupHistory |> ignore
      getDBName serverInfo |> serverInfo.Server.KillDatabase
    serverInfo
      
  /// Drops the given InitialCatalog from the server (if it exists)
  let CreateDb serverInfo = 
    log <| sprintf "Creating database %s on server %s" (getDBName serverInfo) (getServerName serverInfo)
    (getDatabase serverInfo).Create()  
    serverInfo
    
  /// Runs a sql script on the server
  let runScript serverInfo sqlFile =
    log <| sprintf "Executing script %s" sqlFile
    sqlFile
      |> StringHelper.ReadFileAsString
      |> (getDatabase serverInfo).ExecuteNonQuery
      
  /// Closes the connection to the server
  let Disconnect serverInfo =
    serverInfo.Server.ConnectionContext.Disconnect()
   
  /// Drops and creates the database (dropped if db exists. created nonetheless)
  let DropAndCreateDatabase connectionString = 
    connectionString 
      |> getServerInfo
      |> DropDb
      |> CreateDb
      |> Disconnect          

  /// Runs the given sql scripts on the server
  let RunScripts connectionString scripts = 
    let serverInfo = getServerInfo connectionString 
    scripts |> Seq.iter (runScript serverInfo)
    Disconnect serverInfo
  
  /// Runs all sql scripts from the given directory on the server  
  let RunScriptsFromDirectory connectionString scriptDirectory = 
    let scripts = System.IO.Directory.GetFiles(scriptDirectory, "*.sql")
    RunScripts connectionString scripts
    