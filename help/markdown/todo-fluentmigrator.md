# Running database migrations with FluentMigrator

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE.exe before version 5 (or the non-netcore version). The documentation needs te be updated, please help!</p>
</div>

[FluentMigrator](https://github.com/schambers/fluentmigrator/) is a .NET library which helps to version database schema using incremental migrations which are described in C#.
The basic idea of the FAKE helper is to run FluentMigrator over the existing database using compiled assembly with migrations.

## Migrating your database to the latest version

FAKE's support for FluentMigrator ships with a separate NuGet package called [FAKE.FluentMigrator](http://www.nuget.org/packages/FAKE.FluentMigrator). 
Usually your FAKE setup will look as follows:

    // Reference required dlls
    #r @"packages/FAKE/tools/FakeLib.dll"
    #r @"packages/FAKE/tools/Fake.FluentMigrator.dll"

    open Fake
    open Fake.FluentMigratorHelper

    // Assemblies with migrations
    let assembly = "Migrations.dll"
    
    // Using SQL Server 2014 LocalDB
    let connection = 
      ConnectionString(
          @"Data Source=(localdb)\MSSQLLocalDb;Initial Catalog=MyDB;Integrated Security=True", 
          SqlServer(V2014))
          
    // Specify additional options or just use the defaults
    let options = {DefaultMigrationOptions with Profile="Staging"; Tags = ["US"; "Canada"]}

    Target "Build" (fun _ ->
        // Build your Migrations.dll assembly using MSBuild or whatever
    )

    Target "MigrateDatabase" (fun _ ->
        MigrateToLatest connection [assembly] options
    )

    "Build" ==> "MigrateDatabase"

    RunTargetOrDefault "MigrateDatabase"

## ConnectionStrings

Specify connection string in build script:

    let connection = ConnectionString("Server=.;Database=TestDb;User Id=admin;Password=pss;", SqlServer(V2008))


Use connection string from config file:

    let connection = ConnectionStringFromConfig("ConnectionStringKey", "Project\\Web.config", SqlServer(V2012))

## Providers / drivers / SQL dialects

The following drivers are supported:

- SqlServer (SqlServerVersion.Default, V2000, V2005, V2008, V2012, V2014) 
- SqlServer (CE) 
- Oracle (OracleVersion.Default, Managed, DotConnect) 
- DB2 
- Firebird 
- HANA 
- Jet 
- MySql 
- PostgreSQL 
- SQLite 

## Available commands

    // Migrate to the latest available version
    MigrateToLatest connection assemblies options

    // Migrate to the specified version
    MigrateUp version connection assemblies options

    // Rollback to the specified version
    MigrateDown version connection assemblies options

    // Rollback N migrations
    Rollback N connection assemblies options

    // Rollback N migrations
    Rollback N connection assemblies options

    // List applied migrations
    ListAppliedMigrations connection assemblies

## Advanced usage

For advanced usage see the [source code](https://github.com/fsharp/FAKE/blob/master/src/app/Fake.FluentMigrator/FluentMigratorHelper.fs).
