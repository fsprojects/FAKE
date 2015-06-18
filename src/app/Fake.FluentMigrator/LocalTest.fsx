#r @"..\..\..\packages\FluentMigrator\lib\40\FluentMigrator.dll"
#r @"..\..\..\packages\FluentMigrator.Runner\lib\40\FluentMigrator.Runner.dll"
#r @"..\..\..\build\FakeLib.dll"

#load "FluentMigratorHelper.fs"

open Fake
open Fake.FluentMigratorHelper

let connection = "Data Source=.\\SQLEXPRESS;Initial Catalog=Fake.FluentMigrator.Test;Integrated Security=True"
let testAssemblyPath = __SOURCE_DIRECTORY__ @@ "Migrations\TestMigrations.dll"
let scriptPath = __SOURCE_DIRECTORY__ @@ "Script.sql"

MigrateToLatest testAssemblyPath (ConnectionString(connection)) (SqlServer(V2012))

RollbackToPrevious testAssemblyPath (ConnectionString(connection)) (SqlServer(V2012))

ScriptAll testAssemblyPath scriptPath (SqlServer(V2012))