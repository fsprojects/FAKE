/// Implements support for boostrapping FAKE scripts.  A bootstrapping
/// `build.fsx` script executes twice (in two stages), allowing to
/// download dependencies with NuGet and do other preparatory work in
/// the first stage, and have these dependencies available in the
/// second stage.
module Fake.Boot

open System
open System.Collections.Generic
open System.IO
open System.Net
open System.Runtime.Versioning
open System.Text
open NuGet

/// Specifies which version of the NuGet package to install.
type NuGetVersion =
/// Pick the latest available version.
| Latest
/// Pick the latest available version, including pre-release versions.
| LatestPreRelease
/// Pick the given semantic version, such as "2.1.170-alpha".
| SemanticVersion of string

/// Specifies NuGet package dependencies.
type NuGetDependency =
    {
        /// The identifer of the package, such as "FAKE".
        PackageId : string

        /// The version specification.
        Version : NuGetVersion
    }

    /// The default pacakage dependency - take latest including pre-release.
    static member Create(id: string) =
        {
            PackageId = id
            Version = LatestPreRelease
        }

/// Configures the boostrapping process.
type Config =
    {
        /// Framework name for assembly resolution.
        FrameworkName : FrameworkName

        /// Full path to the auto-generated fsx file with include statements.
        IncludesFile : string

        /// The credentials to use when authenticating to NuGet, if any.
        NuGetCredentials : option<ICredentials>

        /// List of automatically installed and NuGet dependencies.
        NuGetDependencies : list<NuGetDependency>

        /// Full path to the `packages` directory for storing NuGet packages.
        NuGetPackagesDirectory : string

        /// The URL of the NuGet source to use.
        NuGetSourceUrl : string

        /// The full path to the root folder.
        SourceDirectory : string

    }

    /// The default configuration for a given source directory.
    static member Default(sourceDir: string) : Config =
        {
            FrameworkName = FrameworkName(".NETFramework,Version=v4.0")
            IncludesFile = sourceDir @@ ".build" @@ "boot.fsx"
            NuGetCredentials = None
            NuGetDependencies = []
            NuGetPackagesDirectory = sourceDir @@ "packages"
            NuGetSourceUrl = "http://nuget.org/api/v2/"
            SourceDirectory = sourceDir
        }

/// Stage of execution for a boot system.
type Stage =
| BuildStage
| ConfigureStage

/// Abstracts over command-line environment features.
[<AbstractClass>]
type CommandEnvironment() =
    abstract SendMessage : message: string -> unit
    abstract CurrentDirectory : string
    abstract FakeDirectory : string

    /// The default environment.
    static member Default : CommandEnvironment =
        {
            new CommandEnvironment() with
                member this.SendMessage(msg: string) = stdout.WriteLine(msg)
                member this.CurrentDirectory = Directory.GetCurrentDirectory()
                member this.FakeDirectory = Path.GetDirectoryName(typeof<Stage>.Assembly.Location)
        }

/// Represents a command line handler.
type CommandHandler =
    {
        Run : CommandEnvironment -> bool
    }

    /// Runs the handler with the default environment.
    member this.Interact() =
        if this.Run(CommandEnvironment.Default) |> not then
            Environment.ExitCode <- 1

[<AutoOpen>]
module private Implementation =

    [<Sealed>]
    type CustomHttpClient(uri, cred: option<ICredentials>) =
        inherit HttpClient(uri)

        interface IHttpClient with
            override this.InitializeRequest(req) =
                this.InitializeRequest(req)
                match cred with
                | None -> ()
                | Some c -> req.Credentials <- c

    let GetManager (config: Config) =
        let factory = PackageRepositoryFactory.Default
        let old = factory.HttpClientFactory
        factory.HttpClientFactory <-
            fun uri -> CustomHttpClient(uri, config.NuGetCredentials) :> _
        let repo = factory.CreateRepository(config.NuGetSourceUrl)
        PackageManager(repo, config.NuGetPackagesDirectory)

    let Install (mgr: PackageManager) (dep: NuGetDependency) : unit =
        match dep.Version with
        | Latest ->
            mgr.InstallPackage(dep.PackageId)
        | LatestPreRelease ->
            mgr.InstallPackage(dep.PackageId, null, false, true)
        | SemanticVersion v ->
            mgr.InstallPackage(dep.PackageId, SemanticVersion.Parse v)

    let TopologicalSort<'K,'T when 'K : equality>
            (getKey: 'T -> 'K)
            (getPreceding: 'T -> seq<'T>)
            (roots: seq<'T>) : seq<'T> =
        let visited = HashSet()
        let trace = Queue()
        let rec visit (node: 'T) =
            let key = getKey node
            if visited.Add(key) then
                Seq.iter visit (getPreceding node)
                trace.Enqueue(node)
        Seq.iter visit roots
        trace.ToArray() :> seq<_>

    let Memoize (f: 'A -> 'B) : ('A -> 'B) =
        let d = Dictionary()
        fun k ->
            match d.TryGetValue(k) with
            | true, v -> v
            | _ ->
                let v = f k
                d.[k] <- v
                v

    let SortAssemblyFilesByReferenceOrder (paths: seq<string>) : seq<string> =
        let getDefn =
            Memoize (fun (path: string) ->
                Mono.Cecil.AssemblyDefinition.ReadAssembly(path))
        let getFullName path =
            let aD = getDefn path
            aD.FullName
        let getPreceding path =
            let aD = getDefn path
            if aD.MainModule.HasAssemblyReferences then
                paths
                |> Seq.filter (fun path ->
                    let fullName = getFullName path
                    aD.MainModule.AssemblyReferences
                    |> Seq.exists (fun ref ->
                        ref.FullName = fullName))
            else Seq.empty
        TopologicalSort getFullName getPreceding paths

    let MostRecent (pkgs: seq<IPackage>) : seq<IPackage> =
        pkgs
        |> Seq.groupBy (fun pkg -> pkg.Id)
        |> Seq.map (fun (_, packages) ->
            packages
            |> Seq.maxBy (fun pkg -> pkg.Version))

    let CompleteAndSortPackages
            (config: Config)
            (mgr: PackageManager)
            (pkgs: seq<IPackage>) : seq<IPackage> =
        let getKey (pkg: IPackage) = pkg.Id
        let getPreceding (pkg: IPackage) =
            pkg.GetCompatiblePackageDependencies(config.FrameworkName)
            |> Seq.choose (fun dep ->
                let r = mgr.LocalRepository
                let pkg = r.FindPackage(dep.Id, dep.VersionSpec, true, true)
                match pkg with
                | null -> None
                | _ -> Some pkg)
            |> MostRecent
        TopologicalSort getKey getPreceding pkgs

    let IsRequiredPackage (config: Config) (pkg: IPackage) : bool =
        config.NuGetDependencies
        |> List.exists (fun dep ->
            dep.PackageId = pkg.Id
            &&
            match dep.Version with
            | Latest -> pkg.IsReleaseVersion()
            | LatestPreRelease -> true
            | SemanticVersion v -> SemanticVersion.Parse(v) = pkg.Version)

    let IsSupported (config: Config) (frameworks: seq<FrameworkName>) =
        VersionUtility.IsCompatible(config.FrameworkName, frameworks)

    let ComputeRefs (config: Config) (mgr: PackageManager) : seq<string> =
        let assemblyRefs = HashSet()
        let pkgRefs = Queue()
        mgr.LocalRepository.GetPackages()
        |> Seq.filter (IsRequiredPackage config)
        |> MostRecent
        |> CompleteAndSortPackages config mgr
        |> Seq.iter (fun pkg ->
            let dir = mgr.PathResolver.GetPackageDirectory(pkg)
            pkg.FrameworkAssemblies
            |> Seq.filter (fun r -> IsSupported config r.SupportedFrameworks)
            |> Seq.iter (fun r -> assemblyRefs.Add(r.AssemblyName) |> ignore)
            pkg.AssemblyReferences
            |> Seq.filter (fun r -> IsSupported config r.SupportedFrameworks)
            |> Seq.map (fun r -> config.NuGetPackagesDirectory @@ dir @@ r.Path)
            |> SortAssemblyFilesByReferenceOrder
            |> Seq.iter pkgRefs.Enqueue)
        Seq.append assemblyRefs pkgRefs

    let GenerateBootScriptText (refs: seq<string>) : string =
        let fakeDir = Path.GetDirectoryName(typeof<Config>.Assembly.Location)
        use w = new StringWriter()
        let writeString (s: string) : unit =
            let q = @""""
            w.Write("@")
            w.Write(q)
            w.Write(s.Replace(q, @""""""))
            w.Write(q)
        let writeRef (path: string) =
            w.Write("#r ")
            writeString path
            w.WriteLine()
        writeRef (fakeDir @@ "NuGet.Core.dll")
        writeRef (fakeDir @@ "FakeLib.dll")
        for r in refs do
            writeRef r
        w.ToString()

    let UTF8 = UTF8Encoding(false)

    let MakeBootScriptFile (config: Config) (refs: seq<string>) : unit =
        let dir = Path.GetDirectoryName(config.IncludesFile)
        if not (Directory.Exists dir) then
            Directory.CreateDirectory dir |> ignore
        File.WriteAllText(config.IncludesFile, GenerateBootScriptText refs, UTF8)

    type Command =
        | ConfigureAndRun
        | ConfigureOnly
        | Init
        | InitSingle
        | RunOnly

    let ParseCommand (args: seq<string>) : option<Command> =
        match Seq.toList args with
        | [] -> Some RunOnly
        | ["auto"] -> Some ConfigureAndRun
        | ["conf"] -> Some ConfigureOnly
        | ["init"] -> Some Init
        | ["init"; "one"] -> Some InitSingle
        | _ -> None

    /// Computes extra command-line arguments to enable bootstrapping FAKE scripts.
    let FsiArgs (env: CommandEnvironment) (stage: Stage) : list<string> =
        let quote (s: string) : string =
            String.Format(@"""{0}""", s.Replace(@"""", @"\"""))
        [
            match stage with
            | BuildStage -> ()
            | ConfigureStage -> yield "--define:BOOT"
            yield "-I"
            yield quote env.FakeDirectory
            yield "-r"
            yield "FakeLib"
        ]

    let Fsi (env: CommandEnvironment) stage script =
        FSIHelper.executeFSIWithArgs env.CurrentDirectory script
            (FsiArgs env stage) []

    /// Checks if the F# script file is a bootstrapping script.
    let IsBootScript (fullPath: string) : bool =
        File.ReadAllLines(fullPath)
        |> Array.exists (fun x -> x.StartsWith("#if BOOT"))

    let DoConfigureOnly (env: CommandEnvironment) =
        let bootScript = env.CurrentDirectory @@ "conf.fsx"
        let buildScript = env.CurrentDirectory @@ "build.fsx"
        if File.Exists bootScript then
            Fsi env ConfigureStage bootScript
        elif File.Exists buildScript  && IsBootScript buildScript then
            Fsi env ConfigureStage buildScript
        else
            env.SendMessage("Could not find conf.fsx or boot-aware build.fsx")
            false

    let DoRunOnly (env: CommandEnvironment) =
        let buildScript = env.CurrentDirectory @@ "build.fsx"
        if File.Exists buildScript then
            Fsi env BuildStage buildScript
        else
            env.SendMessage("Could not find build.fsx")
            false

    let DoConfigureAndRun (env: CommandEnvironment) =
        DoConfigureOnly env && DoRunOnly env

    let DoInit (env: CommandEnvironment) =
        let bootScript = env.CurrentDirectory @@ "conf.fsx"
        let buildScript = env.CurrentDirectory @@ "build.fsx"
        if File.Exists bootScript || File.Exists buildScript then
            env.SendMessage("Already exists: conf.fsx or build.fsx")
            false
        else
            let bootScriptLines =
                [
                    "open Fake"
                    "module FB = Fake.Boot"
                    "FB.Prepare {"
                    "    FB.Config.Default __SOURCE_DIRECTORY__ with"
                    "        NuGetDependencies ="
                    "            let ( ! ) x = FB.NuGetDependency.Create x"
                    "            ["
                    "            ]"
                    "}"
                ]
            let buildScriptLines =
                [
                    @"#load "".build/boot.fsx"""
                    "open Fake"
                ]
            File.WriteAllLines(bootScript, bootScriptLines, UTF8)
            File.WriteAllLines(buildScript, buildScriptLines, UTF8)
            env.SendMessage("Generated conf.fsx and build.fsx")
            true

    let DoInitSingle (env: CommandEnvironment) =
        let buildScript = env.CurrentDirectory @@ "build.fsx"
        if File.Exists buildScript then
            env.SendMessage("Already exists: build.fsx")
            false
        else
            let scriptLines =
                [
                    "#if BOOT"
                    "open Fake"
                    "module FB = Fake.Boot"
                    "FB.Prepare {"
                    "    FB.Config.Default __SOURCE_DIRECTORY__ with"
                    "        NuGetDependencies ="
                    "            let ( ! ) x = FB.NuGetDependency.Create x"
                    "            ["
                    "            ]"
                    "}"
                    "#else"
                    @"#load "".build/boot.fsx"""
                    "open Fake"
                    "#endif"
                ]
            File.WriteAllLines(buildScript, scriptLines, UTF8)
            env.SendMessage("Generated a boot-aware build.fsx")
            true

    let DoHelp (env: CommandEnvironment) =
        use w = new StringWriter()
        w.WriteLine("FAKE boot module: bootstrapping builds. Commands: ")
        w.WriteLine("   FAKE boot           Runs the normal build stage only")
        w.WriteLine("   FAKE boot auto      Runs configure and then build stage")
        w.WriteLine("   FAKE boot conf      Runs configure stage only")
        w.WriteLine("   FAKE boot init      Inits a boot project in current folder")
        w.WriteLine("   FAKE boot init one  Inits a single-file boot project here")
        env.SendMessage (w.ToString())
        true

    let RunCommandLine (args: list<string>) (env: CommandEnvironment) : bool =
        match ParseCommand args with
        | Some ConfigureAndRun -> DoConfigureAndRun env
        | Some ConfigureOnly -> DoConfigureOnly env
        | Some Init -> DoInit env
        | Some InitSingle -> DoInitSingle env
        | Some RunOnly -> DoRunOnly env
        | None -> DoHelp env

/// Creates the CommandHandler from the 
let HandlerForArgs args = { Run = RunCommandLine args }

/// Detects boot-specific commands.
let ParseCommandLine (args: seq<string>) : option<CommandHandler> =
    match Seq.toList args with
    | "boot" :: xs
    | _ :: "boot" :: xs -> Some (HandlerForArgs xs)
    | _ -> None


/// The main function intended to be executed in the BOOT phase of
/// boostrapping scripts.
let Prepare (config: Config) : unit =
    let mgr = GetManager config
    List.iter (Install mgr) config.NuGetDependencies
    let refs = ComputeRefs config mgr
    MakeBootScriptFile config refs
