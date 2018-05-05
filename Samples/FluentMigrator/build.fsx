//#r @"../../packages/FluentMigrator/lib/netstandard2.0/FluentMigrator.dll"
//#r @"../../packages/FluentMigrator.Runner/lib/netstandard2.0/FluentMigrator.Runner.dll"
#r @"..\..\..\..\build\FakeLib.dll"

#load "..\FluentMigratorHelper.fs"

open Fake.CscHelper
open Fake.Sql
open Fake.Core

let root = __SOURCE_DIRECTORY__
let reference = root @@ "..\..\..\..\packages\FluentMigrator\lib\40\FluentMigrator.dll"
let assembly = root @@ "sample.migrations.dll"
let migrations = [
    root @@ "CatsDatabase.cs"
]
let connection = FluentMigrator.ConnectionString(@"Data Source=:memory:;Version=3;New=True;", Sqlite)

let compile() = 
    migrations |> Csc (fun p -> {p with References = [reference]; Output = assembly; Target = Library})

compile()

FluentMigrator.migrateToLatest connection [assembly] id
FluentMigrator.listAppliedMigrations connection [assembly]
FluentMigrator.rollbackLatest connection [assembly] id
FluentMigrator.migrateDown 0L connection [assembly] id
FluentMigrator.migrateUp 2L connection [assembly] id
