#r @"..\..\..\..\packages\FluentMigrator\lib\40\FluentMigrator.dll"
#r @"..\..\..\..\packages\FluentMigrator.Runner\lib\40\FluentMigrator.Runner.dll"
#r @"..\..\..\..\build\FakeLib.dll"

#load "..\FluentMigratorHelper.fs"

open Fake
open Fake.CscHelper
open Fake.FluentMigratorHelper

let root = __SOURCE_DIRECTORY__
let reference = root @@ "..\..\..\..\packages\FluentMigrator\lib\40\FluentMigrator.dll"
let assembly = root @@ "sample.migrations.dll"
let migrations = [
    root @@ "CatsDatabase.cs"
]
let connection = 
    ConnectionString(@"Data Source=(localdb)\MSSQLLocalDb;Initial Catalog=FAKE.DB;Integrated Security=True", SqlServer(V2014))

let options = DefaultMigrationOptions

let compile() = 
    migrations |> Csc (fun p -> {p with References = [reference]; Output = assembly; Target = Library})

compile()
MigrateToLatest connection [assembly] options
ListAppliedMigrations connection [assembly]
RollbackLatest connection [assembly] options
MigrateDown 0L connection [assembly] options
MigrateUp 2L connection [assembly] options
