namespace Fake.Sql

/// Contains helpers around deploying databases.
[<RequireQualifiedAccess>]
module DacPac =

    open Microsoft.SqlServer.Dac
    open System
    open System.Data.SqlClient
    open System.IO
    open System.Reflection

    /// The type of action to execute.
    type DeployAction =
        /// Generate and apply a synchronisation script between two databases.
        | Deploy
        /// Generate a SQL script to sync two databases.
        | Script of OutputPath:string
        /// Generate an XML report for the differences between two databases.
        | Report of OutputPath:string

    /// Configuration arguments for DacPac deploy
    type DeployDbArgs = {
        /// Type of action to execute. Defaults to Deploy.
        Action : DeployAction
        /// Path to source (path to DACPAC or Connection String).
        Source : string
        /// Path to destination (path to DACPAC or Connection String).
        Destination : string
        /// Timeout for deploy (defaults to 120 seconds).
        Timeout : int
        /// Block deployment if data loss can occur. Defaults to true.
        BlockOnPossibleDataLoss : bool
        /// Drops objects in the destination that do not exist in the source. Defaults to false.
        DropObjectsNotInSource : bool
        /// Recreates the database from scratch on publish (rather than an in-place update). Defaults to false.
        RecreateDb : bool
        /// Additional configuration parameters required by sqlpackage.exe
        AdditionalSqlPackageProperties : (string * string) list
        /// SQLCMD variables
        Variables : (string * string) list }

    /// The default DacPac deployment arguments.
    let DefaultDeploymentArgs = 
        { Action = Deploy
          Source = ""
          Destination = ""
          Timeout = 120
          BlockOnPossibleDataLoss = true
          DropObjectsNotInSource = false
          RecreateDb = false
          AdditionalSqlPackageProperties = []
          Variables = [] }

    module PropertyKeys =
        /// When creating a new SQL Azure database, specifies the database service tier to use e.g. S2, P1
        let sqlAzureDbSize = "DatabaseServiceObjective"

    /// Deploys a SQL DacPac or database to another database or DacPac.
    let deployDb setParams =
        let args = setParams DefaultDeploymentArgs
        let connectionStringBuilder = SqlConnectionStringBuilder(args.Destination)
        let package = DacPackage.Load(args.Source)
        // https://docs.microsoft.com/en-us/dotnet/api/microsoft.sqlserver.dac.dacservices?view=sql-dacfx-140.3881.1
        let dacServices = DacServices(args.Destination)

        // https://docs.microsoft.com/en-us/dotnet/api/microsoft.sqlserver.dac.dacdeployoptions?view=sql-dacfx-140.3881.1
        let deployOptions =
            DacDeployOptions(
               BlockOnPossibleDataLoss=args.BlockOnPossibleDataLoss,
               DropObjectsNotInSource=args.DropObjectsNotInSource,
               CommandTimeout=args.Timeout,
               CreateNewDatabase=args.RecreateDb)

        let additionalProps = dict args.AdditionalSqlPackageProperties
        // Find the writeable, public properties.
        let props =
            typeof<DacDeployOptions>.GetProperties(BindingFlags.Instance ||| BindingFlags.Public)
            |> Array.filter (fun p -> p.CanWrite)

        let boolProps = props |> Array.filter (fun p -> p.PropertyType = typeof<bool>)
        for bp in boolProps do
            if additionalProps.ContainsKey(bp.Name) then
                deployOptions.IncludeCompositeObjects <- bool.Parse additionalProps.[bp.Name]

        let objectTypeProps = props |> Array.filter (fun p -> p.PropertyType = typeof<ObjectType[]>)
        for otp in objectTypeProps do
            if additionalProps.ContainsKey(otp.Name) then
                let objectTypes =
                    additionalProps.[otp.Name].Split(',')
                    |> Array.map (fun v -> Enum.Parse(typeof<ObjectType>,v) :?> ObjectType)
                deployOptions.ExcludeObjectTypes <- objectTypes

        // TODO: support more properties?

        // Set SqlCmd variable values.
        for key, value in args.Variables do
            deployOptions.SqlCommandVariableValues.Add(key, value)

        try
            match args.Action with
            | Deploy ->
                // https://docs.microsoft.com/en-us/dotnet/api/microsoft.sqlserver.dac.dacservices.publish?view=sql-dacfx-140.3881.1
                let publishOptions = PublishOptions(DeployOptions=deployOptions)
                dacServices.Publish(package, connectionStringBuilder.InitialCatalog, publishOptions) |> ignore
            | Script outputPath ->
                // https://docs.microsoft.com/en-us/dotnet/api/microsoft.sqlserver.dac.dacservices.script?view=sql-dacfx-140.3881.1
                let publishOptions =
                    PublishOptions(
                        DeployOptions=deployOptions,
                        GenerateDeploymentScript=true,
                        DatabaseScriptPath=outputPath)
                dacServices.Script(package, connectionStringBuilder.InitialCatalog, publishOptions) |> ignore
            | Report outputPath ->
                // https://docs.microsoft.com/en-us/dotnet/api/microsoft.sqlserver.dac.dacservices.script?view=sql-dacfx-140.3881.1
                let publishOptions =
                    PublishOptions(
                        DeployOptions=deployOptions,
                        GenerateDeploymentReport=true)
                let result = dacServices.Script(package, connectionStringBuilder.InitialCatalog, publishOptions)
                use file = new IO.FileStream(outputPath, FileMode.Create, FileAccess.Write)
                use writer = new IO.StreamWriter(file)
                writer.Write(result.DeploymentReport)
        with
        | ex ->
            eprintfn "SqlPackage error: %s" ex.Message
            failwith "Error executing DACPAC deployment. Please see output for error details."
