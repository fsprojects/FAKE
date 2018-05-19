# Packaging and Deploying SQL Databases

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE.exe before version 5 (or the non-netcore version). The documentation for FAKE 5 can be found <a href="sql-dacpac.html">here </a></p>
</div>

FAKE can be used to create a SQL DACPAC and also deploy it to a SQL Server using the MSDeploy executable. This is installed by default with Visual Studio and with the SQL Server Data Tools (SSDT) package.

DACPACs automatically diff from the source to the destination and generate the SQL script dynamically.

You can read up more on DACPac and MSDeploy arguments [here](https://msdn.microsoft.com/en-us/library/hh550081%28v=vs.103%29.aspx).

## Sample usage

Ensure that you have already built your database project (you can do this with standard MSBuild). Then, use the ``deployDb`` command to deploy the ``dbProject.dacpac`` to the ``myDatabase``.

    open Fake.Sql.DacPac
    
    /// the database for local development + compile
    Target "DeployLocalDb" (fun _ ->
        let connectionString = "Data Source=(localdb)\MSSQLLocalDB;Integrated Security=True;Database=MyDatabase;Pooling=False"
        let dacPacPath = "path/to/dbProject.dacpac"
        deployDb (fun args -> { args with Source = dacPacPath; Destination = localDbConnectionString }) |> ignore)

## Deployment Options

You can optionally specify the type of command to use (again, refer to the documentation above for more detail): -

* Deploy - full deploy to destination
* Script - SQL script
* Report - XML report of diff

In addition, you can also elect to deploy to Dacpac files rather than SQL databases - simply pass in the pass to the dacpac that you wish to generate.