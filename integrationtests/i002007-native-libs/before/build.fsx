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

module Imports =
    open System.Runtime.InteropServices
    [<DllImport("kernel32.dll")>]
    extern uint32 GetCurrentProcessId()
    [<DllImport("unknown_dependency.dll")>]
    extern uint32 UnknownFunctionInDll()
    [<DllImport("SQLite.Interop.dll")>]
    extern uint32 Fake_ShouldNotExistExtryPoint()

// Default target
Target.create "Default" (fun _ ->
  Trace.trace "Hello World from FAKE"
  if Environment.isWindows then
    // #2342: make sure defaults PATH dependencies still work, see https://github.com/fsharp/FAKE/issues/2342
    printfn "Current process: %d" (Imports.GetCurrentProcessId())
  
  use conn = openConn "temp.db"
  ()
)
Target.create "FailWithUnknown" (fun _ ->
  printfn "UnknownFunctionInDll: %d" (Imports.UnknownFunctionInDll())
)
Target.create "FailWithMissingEntry" (fun _ ->
  printfn "Fake_ShouldNotExistExtryPoint: %d" (Imports.Fake_ShouldNotExistExtryPoint())
)

// start build
Target.runOrDefault "Default"