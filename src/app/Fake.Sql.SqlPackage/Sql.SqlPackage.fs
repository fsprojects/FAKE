namespace Fake.Sql

/// Contains helpers around deploying databases.
[<RequireQualifiedAccess>]
module SqlPackage =

    open Fake.Core
    open Fake.IO.FileSystemOperators
    open Fake.IO.Globbing.Operators
    open System.IO
    open System

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
        /// The path to SqlPackage.exe.
        SqlPackageToolPath : string
        /// Type of action to execute. Defaults to Deploy.
        Action : DeployAction
        /// Azure AccessToken
        AccessToken: string
        /// Path to source (path to DACPAC or Connection String).
        Source : string
        /// Path to destination (path to DACPAC or Connection String).
        Destination : string
        /// Timeout for deploy (defaults to 120 seconds).
        Timeout : int option
        /// Block deployment if data loss can occur. Defaults to true.
        BlockOnPossibleDataLoss : bool option
        /// Drops objects in the destination that do not exist in the source. Defaults to false.
        DropObjectsNotInSource : bool option
        /// Recreates the database from scratch on publish (rather than an in-place update). Defaults to false.
        RecreateDb : bool option
        /// Additional configuration parameters required by sqlpackage.exe
        AdditionalSqlPackageProperties : (string * string) list
        /// SQLCMD variables
        Variables : (string * string) list
        ///Specifies the file path to a DAC Publish Profile. The profile defines a collection of properties and variables to use when generating outputs.
        Profile : string }

    let internal validPaths =
        let paths = [
            if Environment.isUnix then
                Seq.append Environment.pathDirectories ["/usr/local/bin"; "/usr/bin"] |> Seq.map (fun dir -> !!(dir </> "sqlpackage")) |> Seq.concat |> Seq.map (fun path -> path, 15)
            else
                let getSqlVersion (path:string) = path.Split '\\' |> Array.item 3 |> int
                let getVsVersion (path: string) = (Path.GetDirectoryName path |> DirectoryInfo).Name |> int
                !!(Environment.ProgramFilesX86 </> @"Microsoft SQL Server\**\DAC\bin\SqlPackage.exe") |> Seq.map(fun path -> path, getSqlVersion path)
                !!(Environment.ProgramFilesX86 </> @"Microsoft Visual Studio*\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\*\SqlPackage.exe") |> Seq.map(fun path -> path, getVsVersion path)
                !!(Environment.ProgramFilesX86 </> @"Microsoft Visual Studio\**\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\*\SqlPackage.exe") |> Seq.map(fun path -> path, getVsVersion path)
                !!(Environment.ProgramFiles </> @"Microsoft Visual Studio\**\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\SqlPackage.exe") |> Seq.map(fun path -> path, Reflection.Assembly.LoadFile(path).GetName().Version.Major)
        ]

        paths
        |> Seq.concat
        |> Seq.sortByDescending snd
        |> Seq.map fst
        |> Seq.cache

    /// The default DacPac deployment arguments.
    let internal DefaultDeploymentArgs =
        { SqlPackageToolPath =
            validPaths
            |> Seq.tryHead
            |> defaultArg <| ""
          Action = Deploy
          AccessToken = ""
          Source = ""
          Destination = ""
          Timeout = None
          BlockOnPossibleDataLoss = None
          DropObjectsNotInSource = None
          RecreateDb = None
          AdditionalSqlPackageProperties = []
          Variables = []
          Profile = "" }

    [<Literal>]
    let internal Action = "Action"

    [<Literal>]
    let internal AccessToken = "AccessToken"

    [<Literal>]
    let internal Source = "Source"

    [<Literal>]
    let internal Destination = "Destination"

    [<Literal>]
    let internal OutputPath = "OutputPath"

    [<Literal>]
    let internal BlockOnPossibleDataLoss = "BlockOnPossibleDataLoss"

    [<Literal>]
    let internal DropObjectsNotInSource = "DropObjectsNotInSource"

    [<Literal>]
    let internal Timeout = "Timeout"

    [<Literal>]
    let internal RecreateDb = "RecreateDb"

    [<Literal>]
    let internal AdditionalSqlPackageProperties = "AdditionalSqlPackageProperties"

    [<Literal>]
    let internal Variables = "Variables"

    [<Literal>]
    let internal Profile = "Profile"

    let (|NullOrEmptyString|NonEmptyString|) (x:string) = if String.isNullOrEmpty x then NullOrEmptyString else NonEmptyString x

    /// [omit]
    let formatArgument (args:DeployDbArgs) action outputPath additionalParameters variables argumentName =
        match argumentName, args with
        | Action, _ -> sprintf "/Action:%s" action
        | AccessToken, { AccessToken = NonEmptyString token } -> sprintf """/AccessToken:"%s" """ token
        | Source, _ -> sprintf """/SourceFile:"%s" """ args.Source
        | Destination, { Destination = NonEmptyString destination } -> sprintf """/TargetConnectionString:"%s" """ destination
        | OutputPath, _ -> sprintf "%s" outputPath
        | BlockOnPossibleDataLoss, { BlockOnPossibleDataLoss = Some value } -> sprintf "/p:BlockOnPossibleDataLoss=%b" value
        | BlockOnPossibleDataLoss, { Profile = NullOrEmptyString; BlockOnPossibleDataLoss = None } -> "/p:BlockOnPossibleDataLoss=false"
        | DropObjectsNotInSource, { DropObjectsNotInSource = Some value } -> sprintf "/p:DropObjectsNotInSource=%b" value
        | DropObjectsNotInSource, { Profile = NullOrEmptyString; DropObjectsNotInSource = None } -> "/p:DropObjectsNotInSource=false"
        | Timeout, { Timeout = Some timeout }
        | Timeout, { Profile = NullOrEmptyString; Timeout = Some timeout } ->
            sprintf "/p:CommandTimeout=%d" timeout
        | RecreateDb, { RecreateDb = Some value } -> sprintf "/p:CreateNewDatabase=%b" value
        | RecreateDb, { Profile = NullOrEmptyString; RecreateDb = None } -> "/p:CreateNewDatabase=false"
        | AdditionalSqlPackageProperties, _ when not(String.isNullOrEmpty(additionalParameters)) -> sprintf "%s" additionalParameters
        | Variables, _ when not(String.isNullOrEmpty(variables)) -> sprintf "%s" variables
        | Profile, { Profile = NonEmptyString profile } -> sprintf "/pr:%s" profile
        | _ -> ""

    module PropertyKeys =
        /// When creating a new SQL Azure database, specifies the database service tier to use e.g. S2, P1
        let sqlAzureDbSize = "DatabaseServiceObjective"

    let private generateCommandLine args =
        let action, outputPath =
            match args with
            | Deploy -> "Publish", None
            | Script outputPath -> "Script", Some outputPath
            | Report outputPath -> "DeployReport", Some outputPath
        let outputPath = defaultArg(outputPath |> Option.map(sprintf """/OutputPath:"%s" """)) ""
        action, outputPath

    let private formatDacPacArguments args action outputPath additionalParameters variables =
        [
            Action
            AccessToken
            Source
            Destination
            OutputPath
            BlockOnPossibleDataLoss
            DropObjectsNotInSource
            Timeout
            RecreateDb
            AdditionalSqlPackageProperties
            Variables
            Profile
        ]
        |> List.map (formatArgument args action outputPath additionalParameters variables)
        |> List.filter ((<>) "")
        |> String.concat " "

    /// Deploys a SQL DacPac or database to another database or DacPac.
    let deployDb setParams =
        let args = setParams DefaultDeploymentArgs
        let action, outputPath = generateCommandLine args.Action

        let concat parameter =
            List.map (fun (key, value) -> sprintf "/%s:%s=%s" parameter key value)
            >> String.concat " "

        let additionalParameters = args.AdditionalSqlPackageProperties |> concat "p"

        let variables = args.Variables |> concat "v"

        let arguments = formatDacPacArguments args action outputPath additionalParameters variables

        if System.String.IsNullOrWhiteSpace args.SqlPackageToolPath then
            failwith "No SqlPackage.exe filename was given."

        if not (File.Exists args.SqlPackageToolPath) then
            let paths =
                if validPaths |> Seq.contains args.SqlPackageToolPath then validPaths
                else [ args.SqlPackageToolPath ]
            failwithf "Unable to find a valid instance of SqlPackage.exe. Paths checked were: %A." paths

        CreateProcess.fromRawCommandLine args.SqlPackageToolPath arguments
        |> CreateProcess.withTimeout TimeSpan.MaxValue
        |> CreateProcess.addOnExited (fun data exitCode ->
            if exitCode <> 0 then
                failwithf "Process exit code '%d' <> 0." exitCode
            else
                data)
        |> Proc.run
        |> ignore
