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
            NuGetSourceUrl = "https://nuget.org/api/v2/"
            SourceDirectory = sourceDir
        }

[<AutoOpen>]
module private Implementation =
    open NuGet
    open System.Collections.Generic

    let GetManager (config: Config) =
        let repo = PackageRepositoryFactory.Default.CreateRepository(config.NuGetSourceUrl)
        PackageManager(repo, config.NuGetPackagesDirectory)

    let Install (mgr: PackageManager) (dep: NuGetDependency) : unit =
        match dep.Version with
        | Latest ->
            mgr.InstallPackage(dep.PackageId)
        | LatestPreRelease ->
            mgr.InstallPackage(dep.PackageId, null, false, true)
        | SemanticVersion v ->
            mgr.InstallPackage(dep.PackageId, SemanticVersion.Parse v)

    let FindPackage (mgr: PackageManager) (dep: NuGetDependency) =
        let repo = mgr.LocalRepository
        match dep.Version with
        | Latest ->
            repo.FindPackage(dep.PackageId)
        | LatestPreRelease ->
            repo.FindPackage(dep.PackageId, Unchecked.defaultof<SemanticVersion>, true, true)
        | SemanticVersion v ->
            repo.FindPackage(dep.PackageId, SemanticVersion.Parse v)

    let ComputeRefs (config: Config) (mgr: PackageManager) : seq<string> =
        let assemblyRefs = HashSet()
        let pkgRefs = Queue()
        let visitedPackages = HashSet()
        let refs = Queue()
        let rec visitPackage (pkg: IPackage) =
            let pkgKey = (pkg.Id, pkg.Version)
            if visitedPackages.Add(pkgKey) then
                let dir = mgr.PathResolver.GetPackageDirectory(pkg)
                for pd in pkg.GetCompatiblePackageDependencies(config.FrameworkName) do
                    mgr.LocalRepository.FindPackage(pd.Id, pd.VersionSpec, true, true)
                    |> visitPackage
                for r in pkg.FrameworkAssemblies do
                    if  r.SupportedFrameworks
                        |> Seq.exists (fun x -> x = config.FrameworkName)
                    then
                        assemblyRefs.Add(r.AssemblyName) |> ignore
                for r in pkg.AssemblyReferences do
                    if  r.SupportedFrameworks
                        |> Seq.exists (fun x -> x = config.FrameworkName)
                    then
                        pkgRefs.Enqueue(config.NuGetPackagesDirectory +/ dir +/ r.Path)
        for d in config.NuGetDependencies do
            let pkg = FindPackage mgr d
            visitPackage pkg
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

    let MakeBootScriptFile (config: Config) (refs: seq<string>) : unit =
        let dir = Path.GetDirectoryName(config.IncludesFile)
        if not (Directory.Exists dir) then
            Directory.CreateDirectory dir |> ignore
        File.WriteAllText(config.IncludesFile, GenerateBootScriptText refs, UTF8Encoding(false))

/// The main function intended to be executed in the BOOT phase of
/// boostrapping scripts.
let Prepare (config: Config) : unit =
    let mgr = GetManager config
    List.iter (Install mgr) config.NuGetDependencies
    let refs = ComputeRefs config mgr
    MakeBootScriptFile config refs
