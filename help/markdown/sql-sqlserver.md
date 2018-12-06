# Interacting with SQL Server Databases

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE version 5.0 or later. The old documentation can be found <a href="v4/fake-sql-sqlserver.html">here</a></p>
</div>

FAKE can be used to create, delete, and run scripts against a SQL Server database.

## Sample Usage

You need to have some edition of SQL Server installed on the machine running these tasks. Choose an edition from the [Microsoft SQL Server Downloads page](https://www.microsoft.com/en-us/sql-server/sql-server-downloads).

    open Fake.Sql

    Target.create "CreateIntegrationTestsDatabase" (fun _ ->
        let connectionString = "Data Source=.;Initial Catalog=DATABASE_NAME;Integrated Security=True;"
        SqlServer.dropAndCreateDatabase connectionString

        let scripts = [ "path/to/create/tables.sql"; "path/to/seed/data.sql" ]
                      |> Seq.ofList

        SqlServer.runScripts connectionString scripts
    )