#r "paket:
nuget Fake.Core.Target
nuget System.Data.SQLite.Core //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open System.Data.SQLite

let openConn (path:string) =
    let builder = SQLiteConnectionStringBuilder()
    builder.DataSource <- path
    let conn = new SQLiteConnection(builder.ToString())
    conn.OpenAndReturn()

// Default target
Target.create "Default" (fun _ ->
  Trace.trace "Hello World from FAKE"
  use conn = openConn "temp.db"
  ()
)

// start build
Target.runOrDefault "Default"