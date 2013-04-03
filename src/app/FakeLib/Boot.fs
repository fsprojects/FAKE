/// Implements support for boostrapping FAKE scripts.  A bootstrapping
/// `build.fsx` script executes twice (in two stages), allowing to
/// download dependencies with NuGet and do other preparatory work in
/// the first stage, and have these dependencies available in the
/// second stage.
module Fake.Boot

open System.IO
open System.Runtime.Versioning
open System.Text

let private ( +/ ) a b = Path.Combine(a, b)

/// Checks if the F# script file is a bootstrapping script.
let IsBootScript (fullPath: string) : bool =
    File.ReadAllLines(fullPath)
    |> Array.exists (fun x -> x.StartsWith("#if BOOT"))

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
            IncludesFile = sourceDir +/ ".build" +/ "boot.fsx"
            NuGetDependencies = []
            NuGetPackagesDirectory = sourceDir +/ "packages"
            NuGetSourceUrl = "http://nuget.org/api/v2/"
            SourceDirectory = sourceDir
        }

[<AutoOpen>]
module private Implementation =
    open NuGet
    open System.Collections.Generic

    let GetManager (config: Config) =
        let factory = PackageRepositoryFactory.Default
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
        frameworks
        |> Seq.exists (fun x -> x = config.FrameworkName)

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
            |> Seq.map (fun r -> config.NuGetPackagesDirectory +/ dir +/ r.Path)
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
        writeRef (fakeDir +/ "FakeLib.dll")
        for r in refs do
            writeRef r
        w.ToString()

    let UTF8 = UTF8Encoding(false)

    let MakeBootScriptFile (config: Config) (refs: seq<string>) : unit =
        let dir = Path.GetDirectoryName(config.IncludesFile)
        if not (Directory.Exists dir) then
            Directory.CreateDirectory dir |> ignore
        File.WriteAllText(config.IncludesFile, GenerateBootScriptText refs, UTF8)

/// The main function intended to be executed in the BOOT phase of
/// boostrapping scripts.
let Prepare (config: Config) : unit =
    let mgr = GetManager config
    List.iter (Install mgr) config.NuGetDependencies
    let refs = ComputeRefs config mgr
    MakeBootScriptFile config refs
