#if BOOTSTRAP && DOTNETCORE

#r "paket:
source nuget/dotnetcore
source https://api.nuget.org/v3/index.json
nuget FSharp.Core ~> 4.1
nuget System.AppContext prerelease
nuget Paket.Core prerelease
nuget Fake.Api.GitHub prerelease
nuget Fake.BuildServer.AppVeyor prerelease
nuget Fake.BuildServer.TeamCity prerelease
nuget Fake.BuildServer.Travis prerelease
nuget Fake.BuildServer.TeamFoundation prerelease
nuget Fake.Core.Target prerelease
nuget Fake.Core.SemVer prerelease
nuget Fake.IO.FileSystem prerelease
nuget Fake.IO.Zip prerelease
nuget Fake.Core.ReleaseNotes prerelease
nuget Fake.DotNet.AssemblyInfoFile prerelease
nuget Fake.DotNet.MSBuild prerelease
nuget Fake.DotNet.Cli prerelease
nuget Fake.DotNet.NuGet prerelease
nuget Fake.DotNet.Paket prerelease
nuget Fake.DotNet.FSFormatting prerelease
nuget Fake.DotNet.Testing.MSpec prerelease
nuget Fake.DotNet.Testing.XUnit2 prerelease
nuget Fake.DotNet.Testing.NUnit prerelease
nuget Fake.Windows.Chocolatey prerelease
nuget Fake.Tools.Git prerelease
nuget Mono.Cecil prerelease
nuget Octokit //"
#endif


#if DOTNETCORE
// We need to use this for now as "regular" Fake breaks when its caching logic cannot find "intellisense.fsx".
// This is the reason why we need to checkin the "intellisense.fsx" file for now...
#load ".fake/build.fsx/intellisense.fsx"

open System.Reflection

#else
// Load this before FakeLib, see https://github.com/fsharp/FSharp.Compiler.Service/issues/763
#r "packages/Mono.Cecil/lib/net40/Mono.Cecil.dll"
//#if DESIGNTIME
#I "packages/build/FAKE/tools/"
#r "FakeLib.dll"
#r "Paket.Core.dll"
#r "packages/build/System.Net.Http/lib/net46/System.Net.Http.dll"
#r "packages/build/Octokit/lib/net45/Octokit.dll"
//#else
//#r "src/app/FakeLib/bin/Debug/FakeLib.dll"
//#endif
#I "packages/build/SourceLink.Fake/tools/"
//#load "packages/build/SourceLink.Fake/tools/SourceLink.fsx"

#endif

//#if !FAKE
//let execContext = Fake.Core.Context.FakeExecutionContext.Create false "build.fsx" []
//Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
//#endif

open System.IO
open Fake.Api
open Fake.Core
open Fake.BuildServer
open Fake.Tools
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Windows
open Fake.DotNet
open Fake.DotNet.Testing

// Set this to true if you have lots of breaking changes, for small breaking changes use #if BOOTSTRAP, setting this flag will not be accepted
let disableBootstrap = false

// properties
let projectName = "FAKE"
let projectSummary = "FAKE - F# Make - Get rid of the noise in your build scripts."
let projectDescription = "FAKE - F# Make - is a build automation tool for .NET. Tasks and dependencies are specified in a DSL which is integrated in F#."
let authors = ["Steffen Forkmann"; "Mauricio Scheffer"; "Colin Bull"; "Matthias Dittrich"]
let gitRaw = Environment.environVarOrDefault "gitRaw" "https://raw.github.com/fsharp"

let gitOwner = "fsharp"
let gitHome = "https://github.com/" + gitOwner

// The name of the project on GitHub
let gitName = "FAKE"

let release = ReleaseNotes.load "RELEASE_NOTES.md"

let packages =
    ["FAKE.Core",projectDescription
     "FAKE.Gallio",projectDescription + " Extensions for Gallio"
     "FAKE.IIS",projectDescription + " Extensions for IIS"
     "FAKE.FluentMigrator",projectDescription + " Extensions for FluentMigrator"
     "FAKE.SQL",projectDescription + " Extensions for SQL Server"
     "FAKE.Experimental",projectDescription + " Experimental Extensions"
     "FAKE.Deploy.Lib",projectDescription + " Extensions for FAKE Deploy"
     projectName,projectDescription + " This package bundles all extensions."
     "FAKE.Lib",projectDescription + " FAKE helper functions as library"]

let buildDir = "./build"
let testDir = "./test"
let docsDir = "./docs"
let apidocsDir = "./docs/apidocs/"
let nugetDncDir = "./nuget/dotnetcore"
let nugetLegacyDir = "./nuget/legacy"
let reportDir = "./report"
let packagesDir = "./packages"
let buildMergedDir = buildDir </> "merged"

let root = __SOURCE_DIRECTORY__
let srcDir = root</>"src"
let appDir = srcDir</>"app"
let legacyDir = srcDir</>"legacy"

let additionalFiles = [
    "License.txt"
    "README.markdown"
    "RELEASE_NOTES.md"
    "./packages/FSharp.Core/lib/net45/FSharp.Core.sigdata"
    "./packages/FSharp.Core/lib/net45/FSharp.Core.optdata"]

BuildServer.install [
    AppVeyor.Installer
    TeamCity.Installer
    Travis.Installer
    TeamFoundation.Installer
]

let dotnetSdk = lazy DotNet.install DotNet.Release_2_1_4
let inline dtntWorkDir wd =
    DotNet.Options.lift dotnetSdk.Value
    >> DotNet.Options.withWorkingDirectory wd
let inline dtntSmpl arg = DotNet.Options.lift dotnetSdk.Value arg

let cleanForTests () =
    // Clean NuGet cache (because it might contain appveyor stuff)
    let cacheFolders = [ Paket.Constants.UserNuGetPackagesFolder; Paket.Constants.NuGetCacheFolder ]
    for f in cacheFolders do
        printfn "Clearing FAKE-NuGet packages in %s" f
        !! (f </> "Fake.*")
        |> Seq.iter (Shell.rm_rf)

    let run workingDir fileName args =
        printfn "CWD: %s" workingDir
        let fileName, args =
            if Environment.isUnix
            then fileName, args else "cmd", ("/C " + fileName + " " + args)
        let ok =
            Process.execSimple (fun info ->
            { info with
                FileName = fileName
                WorkingDirectory = workingDir
                Arguments = args }
            ) System.TimeSpan.MaxValue
        if ok <> 0 then failwith (sprintf "'%s> %s %s' task failed" workingDir fileName args)

    let rmdir dir =
        if Environment.isUnix
        then Shell.rm_rf dir
        // Use this in Windows to prevent conflicts with paths too long
        else run "." "cmd" ("/C rmdir /s /q " + Path.GetFullPath dir)
    // Clean test directories
    !! "integrationtests/*/temp"
    |> Seq.iter rmdir

// Targets
Target.create "Clean" (fun _ ->
    !! "src/*/*/bin"
    //++ "src/*/*/obj"
    |> Shell.CleanDirs

    // Workaround https://github.com/fsprojects/Paket/issues/2830
    // https://github.com/fsprojects/Paket/issues/2689
    // Basically paket fails if there is already an existing nuspec in obj/ dir because then MSBuild will call paket with multiple nuspec file arguments separated by ';'
    !! "src/*/*/obj/**/*.nuspec"
    -- (sprintf "src/*/*/obj/**/*%s.nuspec" release.NugetVersion)
    //-- "src/*/*/obj/*.references"
    //-- "src/*/*/obj/*.props"
    //-- "src/*/*/obj/*.paket.references.cached"
    //-- "src/*/*/obj/*.NuGet.Config"
    |> File.deleteAll

    Shell.CleanDirs [buildDir; testDir; docsDir; apidocsDir; nugetDncDir; nugetLegacyDir; reportDir]

    // Clean Data for tests
    cleanForTests()
)

Target.create "RenameFSharpCompilerService" (fun _ ->
  for packDir in ["FSharp.Compiler.Service";"netcore"</>"FSharp.Compiler.Service"] do
    // for framework in ["net40"; "net45"] do
    for framework in ["netstandard2.0"; "net45"] do
      let dir = __SOURCE_DIRECTORY__ </> "packages"</>packDir</>"lib"</>framework
      let targetFile = dir </>  "FAKE.FSharp.Compiler.Service.dll"
      File.delete targetFile

#if DOTNETCORE
      let reader =
          let searchpaths =
              [ dir; __SOURCE_DIRECTORY__ </> "packages/FSharp.Core/lib/net45" ]
          let resolve name =
              let n = AssemblyName(name)
              match searchpaths
                      |> Seq.collect (fun p -> Directory.GetFiles(p, "*.dll"))
                      |> Seq.tryFind (fun f -> f.ToLowerInvariant().Contains(n.Name.ToLowerInvariant())) with
              | Some f -> f
              | None ->
                  failwithf "Could not resolve '%s'" name
          let readAssemblyE (name:string) (parms: Mono.Cecil.ReaderParameters) =
              Mono.Cecil.AssemblyDefinition.ReadAssembly(
                  resolve name,
                  parms)
          let readAssembly (name:string) (x:Mono.Cecil.IAssemblyResolver) =
              readAssemblyE name (new Mono.Cecil.ReaderParameters(AssemblyResolver = x))
          { new Mono.Cecil.IAssemblyResolver with
              member x.Dispose () = ()
              //member x.Resolve (name : string) = readAssembly name x
              //member x.Resolve (name : string, parms : Mono.Cecil.ReaderParameters) = readAssemblyE name parms
              member x.Resolve (name : Mono.Cecil.AssemblyNameReference) = readAssembly name.FullName x
              member x.Resolve (name : Mono.Cecil.AssemblyNameReference, parms : Mono.Cecil.ReaderParameters) = readAssemblyE name.FullName parms
               }
#else
      let reader = new Mono.Cecil.DefaultAssemblyResolver()
      reader.AddSearchDirectory(dir)
      reader.AddSearchDirectory(__SOURCE_DIRECTORY__ </> "packages/FSharp.Core/lib/net45")
#endif
      let readerParams = Mono.Cecil.ReaderParameters(AssemblyResolver = reader)
      let asem = Mono.Cecil.AssemblyDefinition.ReadAssembly(dir </>"FSharp.Compiler.Service.dll", readerParams)
      asem.Name <- Mono.Cecil.AssemblyNameDefinition("FAKE.FSharp.Compiler.Service", System.Version(1,0,0,0))
      asem.Write(dir</>"FAKE.FSharp.Compiler.Service.dll")
)


let common = [
    AssemblyInfo.Product "FAKE - F# Make"
    AssemblyInfo.Version release.AssemblyVersion
    AssemblyInfo.InformationalVersion release.NugetVersion
    AssemblyInfo.FileVersion release.NugetVersion]

// New FAKE libraries
let dotnetAssemblyInfos =
    [ "dotnet-fake", "Fake dotnet-cli command line tool"
      "Fake.Api.Slack", "Slack Integration Support"
      "Fake.Api.GitHub", "GitHub Client API Support via Octokit"
      "Fake.Azure.CloudServices", "Azure Cloud Services Support"
      "Fake.Azure.Emulators", "Azure Emulators Support"
      "Fake.Azure.Kudu", "Azure Kudu Support"
      "Fake.Azure.WebJobs", "Azure Web Jobs Support"
      "Fake.BuildServer.TeamCity", "Integration into TeamCity buildserver"
      "Fake.BuildServer.AppVeyor", "Integration into AppVeyor buildserver"
      "Fake.BuildServer.Travis", "Integration into Travis buildserver"
      "Fake.BuildServer.TeamFoundation", "Integration into TeamFoundation buildserver"
      "Fake.Core.Context", "Core Context Infrastructure"
      "Fake.Core.CommandLineParsing", "Core commandline parsing support via docopt like syntax"
      "Fake.Core.Environment", "Environment Detection"
      "Fake.Core.Process", "Starting and managing Processes"
      "Fake.Core.ReleaseNotes", "Parsing ReleaseNotes"
      "Fake.Core.SemVer", "Parsing and working with SemVer"
      "Fake.Core.String", "Core String manipulations"
      "Fake.Core.Target", "Defining and running Targets"
      "Fake.Core.Tasks", "Repeating and managing Tasks"
      "Fake.Core.Trace", "Core Logging functionality"
      "Fake.Core.Xml", "Core Xml functionality"
      "Fake.DotNet.AssemblyInfoFile", "Writing AssemblyInfo files"
      "Fake.DotNet.Cli", "Running the dotnet cli"
      "Fake.DotNet.MSBuild", "Running msbuild"
      "Fake.DotNet.NuGet", "Running NuGet Client and interacting with NuGet Feeds"
      "Fake.DotNet.Paket", "Running Paket and publishing packages"
      "Fake.DotNet.FSFormatting", "Running fsformatting.exe and generating documentation"
      "Fake.DotNet.Testing.MSpec", "Running mspec test runner"
      "Fake.DotNet.Testing.NUnit", "Running nunit test runner"
      "Fake.DotNet.Testing.XUnit2", "Running xunit test runner"
      "Fake.DotNet.Testing.MSTest", "Running mstest test runner"
      "Fake.DotNet.Xamarin", "Running Xamarin builds"
      "Fake.JavaScript.Npm", "Running npm commands"
      "Fake.IO.FileSystem", "Core Filesystem utilities and globbing support"
      "Fake.IO.Zip", "Core Zip functionality"
      "Fake.Net.Http", "HTTP Client"
      "Fake.netcore", "Command line tool"
      "Fake.Runtime", "Core runtime features"
      "Fake.Tools.Git", "Running git commands"
      "Fake.Testing.Common", "Common testing data types"
      "Fake.Tracing.NAntXml", "NAntXml"
      "Fake.Windows.Chocolatey", "Running and packaging with Chocolatey"
      "Fake.Testing.SonarQube", "Analyzing your project with SonarQube"
      "Fake.DotNet.Testing.OpenCover", "Code coverage with OpenCover" ]

let assemblyInfos =
  [ legacyDir </> "FAKE/AssemblyInfo.fs",
      [ AssemblyInfo.Title "FAKE - F# Make Command line tool"
        AssemblyInfo.Guid "fb2b540f-d97a-4660-972f-5eeff8120fba"] @ common
    legacyDir </> "Fake.Deploy/AssemblyInfo.fs",
      [ AssemblyInfo.Title "FAKE - F# Make Deploy tool"
        AssemblyInfo.Guid "413E2050-BECC-4FA6-87AA-5A74ACE9B8E1"] @ common
    legacyDir </> "deploy.web/Fake.Deploy.Web/AssemblyInfo.fs",
      [ AssemblyInfo.Title "FAKE - F# Make Deploy Web"
        AssemblyInfo.Guid "27BA7705-3F57-47BE-B607-8A46B27AE876"] @ common
    legacyDir </> "Fake.Deploy.Lib/AssemblyInfo.fs",
      [ AssemblyInfo.Title "FAKE - F# Make Deploy Lib"
        AssemblyInfo.Guid "AA284C42-1396-42CB-BCAC-D27F18D14AC7"] @ common
    legacyDir </> "FakeLib/AssemblyInfo.fs",
      [ AssemblyInfo.Title "FAKE - F# Make Lib"
        AssemblyInfo.InternalsVisibleTo "Test.FAKECore"
        AssemblyInfo.Guid "d6dd5aec-636d-4354-88d6-d66e094dadb5"] @ common
    legacyDir </> "Fake.SQL/AssemblyInfo.fs",
      [ AssemblyInfo.Title "FAKE - F# Make SQL Lib"
        AssemblyInfo.Guid "A161EAAF-EFDA-4EF2-BD5A-4AD97439F1BE"] @ common
    legacyDir </> "Fake.Experimental/AssemblyInfo.fs",
      [ AssemblyInfo.Title "FAKE - F# Make Experimental Lib"
        AssemblyInfo.Guid "5AA28AED-B9D8-4158-A594-32FE5ABC5713"] @ common
    legacyDir </> "Fake.FluentMigrator/AssemblyInfo.fs",
      [ AssemblyInfo.Title "FAKE - F# Make FluentMigrator Lib"
        AssemblyInfo.Guid "E18BDD6F-1AF8-42BB-AEB6-31CD1AC7E56D"] @ common ] @
   (dotnetAssemblyInfos
    |> List.map (fun (project, description) ->
        appDir </> sprintf "%s/AssemblyInfo.fs" project, [AssemblyInfo.Title (sprintf "FAKE - F# Make %s" description) ] @ common))

Target.create "SetAssemblyInfo" (fun _ ->
    for assemblyFile, attributes in assemblyInfos do
        // Fixes merge conflicts in AssemblyInfo.fs files, while at the same time leaving the repository in a compilable state.
        // http://stackoverflow.com/questions/32251037/ignore-changes-to-a-tracked-file
        // Quick-fix: git ls-files -v . | grep ^S | cut -c3- | xargs git update-index --no-skip-worktree
        Git.CommandHelper.directRunGitCommandAndFail "." (sprintf "update-index --skip-worktree %s" assemblyFile)
        attributes |> AssemblyInfoFile.createFSharp assemblyFile
        ()
)

Target.create "DownloadPaket" (fun _ ->
    if 0 <> Process.execSimple (fun info ->
            { info with
                FileName = ".paket/paket.exe"
                Arguments = "--version" }
            |> Process.withFramework
            ) (System.TimeSpan.FromMinutes 5.0) then
        failwith "paket failed to start"
)

Target.create "UnskipAndRevertAssemblyInfo" (fun _ ->
    for assemblyFile, _ in assemblyInfos do
        // While the files are skipped in can be hard to switch between branches
        // Therefore we unskip and revert here.
        Git.CommandHelper.directRunGitCommandAndFail "." (sprintf "update-index --no-skip-worktree %s" assemblyFile)
        Git.CommandHelper.directRunGitCommandAndFail "." (sprintf "checkout HEAD %s" assemblyFile)
        ()
)

Target.create "BuildSolution_" (fun _ ->
#if BOOTSTRAP
    MSBuild.runWithDefaults "Build" ["./src/Legacy-FAKE.sln"; "./src/Legacy-FAKE.Deploy.Web.sln"]
#else
    MsBuild.runWithDefaults "Build" ["./src/Legacy-FAKE.sln"; "./src/Legacy-FAKE.Deploy.Web.sln"]
#endif
    |> Trace.logItems "AppBuild-Output: "
)

Target.create "GenerateDocs" (fun _ ->
    Shell.CleanDir docsDir
    let source = "./help"
    let docsTemplate = "docpage.cshtml"
    let indexTemplate = "indexpage.cshtml"
    let githubLink = "https://github.com/fsharp/FAKE"
    let projInfo =
      [ "page-description", "FAKE - F# Make"
        "page-author", String.separated ", " authors
        "project-author", String.separated ", " authors
        "github-link", githubLink
        "version", release.NugetVersion
        "project-github", "http://github.com/fsharp/fake"
        "project-nuget", "https://www.nuget.org/packages/FAKE"
        "root", "http://fsharp.github.io/FAKE"
        "project-name", "FAKE - F# Make" ]
    let layoutroots = [ "./help/templates"; "./help/templates/reference" ]

    Shell.CopyDir (docsDir) "help/content" FileFilter.allFiles
    File.writeString false "./docs/.nojekyll" ""
    File.writeString false "./docs/CNAME" "fake.build"
    //CopyDir (docsDir @@ "pics") "help/pics" FileFilter.allFiles

    Shell.Copy (source @@ "markdown") ["RELEASE_NOTES.md"]
    FSFormatting.createDocs (fun s ->
        { s with
            Source = source @@ "markdown"
            OutputDirectory = docsDir
            Template = docsTemplate
            ProjectParameters = ("CurrentPage", "Modules") :: projInfo
            LayoutRoots = layoutroots })
    FSFormatting.createDocs (fun s ->
        { s with
            Source = source @@ "redirects"
            OutputDirectory = docsDir
            Template = docsTemplate
            ProjectParameters = ("CurrentPage", "FAKE-4") :: projInfo
            LayoutRoots = layoutroots })
    FSFormatting.createDocs (fun s ->
        { s with
            Source = source @@ "startpage"
            OutputDirectory = docsDir
            Template = indexTemplate
            // TODO: CurrentPage shouldn't be required as it's written in the template, but it is -> investigate
            ProjectParameters = ("CurrentPage", "Home") :: projInfo
            LayoutRoots = layoutroots })

    let dllFiles =
        !! "./build/**/Fake.*.dll"
          ++ "./build/FakeLib.dll"
          -- "./build/**/Fake.Experimental.dll"
          -- "./build/**/FSharp.Compiler.Service.dll"
          -- "./build/**/netcore/FAKE.FSharp.Compiler.Service.dll"
          -- "./build/**/FAKE.FSharp.Compiler.Service.dll"
          -- "./build/**/Fake.IIS.dll"
          -- "./build/**/Fake.Deploy.Lib.dll"

    Directory.ensure apidocsDir
    dllFiles
    |> FSFormatting.createDocsForDlls (fun s ->
        { s with
            OutputDirectory = apidocsDir
            LayoutRoots = layoutroots
            LibDirs = [ "./build" ]
            // TODO: CurrentPage shouldn't be required as it's written in the template, but it is -> investigate
            ProjectParameters = ("CurrentPage", "APIReference") :: projInfo
            SourceRepository = githubLink + "/blob/master" })

)

Target.create "CopyLicense" (fun _ ->
    Shell.CopyTo buildDir additionalFiles
)

Target.create "Test" (fun _ ->
    !! (testDir @@ "Test.*.dll")
    |> Seq.filter (fun fileName -> if Environment.isMono then fileName.ToLower().Contains "deploy" |> not else true)
    |> MSpec.exec (fun p ->
            {p with
                ToolPath = Globbing.Tools.findToolInSubPath "mspec-x86-clr4.exe" (Shell.pwd() @@ "tools" @@ "MSpec")
                ExcludeTags = if Environment.isWindows then ["HTTP"] else ["HTTP"; "WindowsOnly"]
                TimeOut = System.TimeSpan.FromMinutes 5.
                HtmlOutputDir = reportDir})
    try
        !! (testDir @@ "Test.*.dll")
          ++ (testDir @@ "FsCheck.Fake.dll")
        |> XUnit2.run id
    with e when e.Message.Contains "timed out" && Environment.isUnix ->
        Trace.traceFAKE "Ignoring xUnit timeout for now, there seems to be something funny going on ..."
)

Target.create "DotNetCoreIntegrationTests" (fun _ ->
    cleanForTests()

    !! (testDir @@ "*.IntegrationTests.dll")
    |> NUnit3.run id
)


Target.create "DotNetCoreUnitTests" (fun _ ->
    // dotnet run -p src/test/Fake.Core.UnitTests/Fake.Core.UnitTests.fsproj
    let processResult =
        DotNet.exec (dtntWorkDir root) "src/test/Fake.Core.UnitTests/bin/Release/netcoreapp2.0/Fake.Core.UnitTests.dll" "--summary"

    if processResult.ExitCode <> 0 then failwithf "Unit-Tests failed."

    // dotnet run --project src/test/Fake.Core.CommandLine.UnitTests/Fake.Core.CommandLine.UnitTests.fsproj
    let processResult =
        DotNet.exec (dtntWorkDir root) "src/test/Fake.Core.CommandLine.UnitTests/bin/Release/netcoreapp2.0/Fake.Core.CommandLine.UnitTests.dll" "--summary"

    if processResult.ExitCode <> 0 then failwithf "Unit-Tests for Fake.Core.CommandLine failed."
)

Target.create "BootstrapTest" (fun _ ->
    let buildScript = "build.fsx"
    let testScript = "testbuild.fsx"
    // Check if we can build ourself with the new binaries.
    let test clearCache (script:string) =
        let clear () =
            // Will make sure the test call actually compiles the script.
            // Note: We cannot just clean .fake here as it might be locked by the currently executing code :)
            if Directory.Exists ".fake" then
                Directory.EnumerateFiles(".fake")
                  |> Seq.filter (fun s -> (Path.GetFileName s).StartsWith script)
                  |> Seq.iter File.Delete
        let executeTarget span target =
            if clearCache then clear ()
            if Environment.isUnix then
                let result =
                    Process.execSimple (fun info ->
                    { info with
                        FileName = "chmod"
                        WorkingDirectory = "."
                        Arguments = "+x build/FAKE.exe" }
                    |> Process.withFramework
                    ) span
                if result <> 0 then failwith "'chmod +x build/FAKE.exe' failed on unix"
            Process.execSimple (fun info ->
            { info with
                FileName = "build/FAKE.exe"
                WorkingDirectory = "."
                Arguments = sprintf "%s %s --fsiargs \"--define:BOOTSTRAP\"" script target }
            |> Process.withFramework
            |> Process.setEnvironmentVariable "FAKE_DETAILED_ERRORS" "true"
                ) span

        let result = executeTarget (System.TimeSpan.FromMinutes 10.0) "PrintColors"
        if result <> 0 then failwith "Bootstrapping failed"

        let result = executeTarget (System.TimeSpan.FromMinutes 1.0) "FailFast"
        if result = 0 then failwith "Bootstrapping failed"

    // Replace the include line to use the newly build FakeLib, otherwise things will be weird.
    File.ReadAllText buildScript
    |> fun s -> s.Replace("#I \"packages/build/FAKE/tools/\"", "#I \"build/\"")
    |> fun text -> File.WriteAllText(testScript, text)

    try
      // Will compile the script.
      test true testScript
      // Will use the compiled/cached version.
      test false testScript
    finally File.Delete(testScript)
)


Target.create "BootstrapTestDotNetCore" (fun _ ->
    let buildScript = "build.fsx"
    let testScript = "testbuild.fsx"
    // Check if we can build ourself with the new binaries.
    let test timeout clearCache script =
        let clear () =
            // Will make sure the test call actually compiles the script.
            // Note: We cannot just clean .fake here as it might be locked by the currently executing code :)
            if Directory.Exists ".fake/testbuild.fsx/packages" then
              Directory.Delete (".fake/testbuild.fsx/packages", true)
            if File.Exists ".fake/testbuild.fsx/paket.depedencies.sha1" then
              File.Delete ".fake/testbuild.fsx/paket.depedencies.sha1"
            if File.Exists ".fake/testbuild.fsx/paket.lock" then
              File.Delete ".fake/testbuild.fsx/paket.lock"
            // TODO: Clean a potentially cached dll as well.

        let executeTarget target =
            if clearCache then clear ()
            let fileName =
                if Environment.isUnix then "nuget/dotnetcore/Fake.netcore/current/fake"
                else "nuget/dotnetcore/Fake.netcore/current/fake.exe"
            Process.execSimple (fun info ->
                { info with
                    FileName = fileName
                    WorkingDirectory = "."
                    Arguments = sprintf "run --fsiargs \"--define:BOOTSTRAP\" %s --target %s" script target }
                |> Process.setEnvironmentVariable "FAKE_DETAILED_ERRORS" "true"
                )
                timeout
                //true (Trace.traceFAKE "%s") Trace.trace


        let result = executeTarget "PrintColors"
        if result <> 0 then failwithf "Bootstrapping failed (because of exitcode %d)" result

        let result = executeTarget "FailFast"
        if result = 0 then failwithf "Bootstrapping failed (because of exitcode %d)" result

    // Replace the include line to use the newly build FakeLib, otherwise things will be weird.
    // TODO: We might need another way, because currently we reference the same paket group?
    File.ReadAllText buildScript
    |> fun text -> File.WriteAllText(testScript, text)

    try
      // Will compile the script.
      test (System.TimeSpan.FromMinutes 15.0) true testScript
      // Will use the compiled/cached version.
      test (System.TimeSpan.FromMinutes 3.0) false testScript
    finally File.Delete(testScript)
)

Target.create "SourceLink" (fun _ ->
//#if !DOTNETCORE
//    !! "src/app/**/*.fsproj"
//    |> Seq.iter (fun f ->
//        let proj = VsProj.LoadRelease f
//        let url = sprintf "%s/%s/{0}/%%var2%%" gitRaw projectName
//        SourceLink.Index proj.CompilesNotLinked proj.OutputFilePdb __SOURCE_DIRECTORY__ url )
//    let pdbFakeLib = "./build/FakeLib.pdb"
//    Shell.CopyFile "./build/FAKE.Deploy" pdbFakeLib
//    Shell.CopyFile "./build/FAKE.Deploy.Lib" pdbFakeLib
//#else
    printfn "We don't currently have VsProj.LoadRelease on dotnetcore."
//#endif
)

Target.create "ILRepack" (fun _ ->
    Directory.ensure buildMergedDir

    let internalizeIn filename =
        let toPack =
            [filename; "FSharp.Compiler.Service.dll"]
            |> List.map (fun l -> buildDir </> l)
            |> String.separated " "
        let targetFile = buildMergedDir </> filename

        let result =
            Process.execSimple (fun info ->
            { info with
                FileName = Directory.GetCurrentDirectory() </> "packages" </> "build" </> "ILRepack" </> "tools" </> "ILRepack.exe"
                Arguments = sprintf "/verbose /lib:%s /ver:%s /out:%s %s" buildDir release.AssemblyVersion targetFile toPack }
            ) (System.TimeSpan.FromMinutes 5.)

        if result <> 0 then failwithf "Error during ILRepack execution."

        Shell.CopyFile (buildDir </> filename) targetFile

    internalizeIn "FAKE.exe"

    !! (buildDir </> "FSharp.Compiler.Service.**")
    |> Seq.iter File.delete

    Shell.DeleteDir buildMergedDir
)

Target.create "CreateNuGet" (fun _ ->
    let set64BitCorFlags files =
        files
        |> Seq.iter (fun file ->
            let args =
                { Program = "lib" @@ "corflags.exe"
                  WorkingDir = Path.GetDirectoryName file
                  CommandLine = "/32BIT- /32BITPREF- " + Process.quoteIfNeeded file
                  Args = [] }
            printfn "%A" args
            Process.shellExec args |> ignore)

    let x64ify (package:NuGet.NuGet.NuGetParams) =
        { package with
            Dependencies = package.Dependencies |> List.map (fun (pkg, ver) -> pkg + ".x64", ver)
            Project = package.Project + ".x64" }

    for package,description in packages do
        let nugetDocsDir = nugetLegacyDir @@ "docs"
        let nugetToolsDir = nugetLegacyDir @@ "tools"
        let nugetLibDir = nugetLegacyDir @@ "lib"
        let nugetLib451Dir = nugetLibDir @@ "net451"

        Shell.CleanDir nugetDocsDir
        Shell.CleanDir nugetToolsDir
        Shell.CleanDir nugetLibDir
        Shell.DeleteDir nugetLibDir

        File.delete "./build/FAKE.Gallio/Gallio.dll"

        let deleteFCS _ =
          //!! (dir </> "FSharp.Compiler.Service.**")
          //|> Seq.iter DeleteFile
          ()

        match package with
        | p when p = projectName ->
            !! (buildDir @@ "**/*.*") |> Shell.Copy nugetToolsDir
            Shell.CopyDir nugetDocsDir docsDir FileFilter.allFiles
            deleteFCS nugetToolsDir
        | p when p = "FAKE.Core" ->
            !! (buildDir @@ "*.*") |> Shell.Copy nugetToolsDir
            Shell.CopyDir nugetDocsDir docsDir FileFilter.allFiles
            deleteFCS nugetToolsDir
        | p when p = "FAKE.Lib" ->
            Shell.CleanDir nugetLib451Dir
            {
                Globbing.BaseDirectory = buildDir
                Globbing.Includes = [ "FakeLib.dll"; "FakeLib.XML" ]
                Globbing.Excludes = []
            }
            |> Shell.Copy nugetLib451Dir
            deleteFCS nugetLib451Dir
        | _ ->
            Shell.CopyDir nugetToolsDir (buildDir @@ package) FileFilter.allFiles
            Shell.CopyTo nugetToolsDir additionalFiles
        !! (nugetToolsDir @@ "*.srcsv") |> File.deleteAll

        let setParams (p:NuGet.NuGet.NuGetParams) =
            {p with
                NuGet.NuGet.NuGetParams.Authors = authors
                NuGet.NuGet.NuGetParams.Project = package
                NuGet.NuGet.NuGetParams.Description = description
                NuGet.NuGet.NuGetParams.Version = release.NugetVersion
                NuGet.NuGet.NuGetParams.OutputPath = nugetLegacyDir
                NuGet.NuGet.NuGetParams.WorkingDir = nugetLegacyDir
                NuGet.NuGet.NuGetParams.Summary = projectSummary
                NuGet.NuGet.NuGetParams.ReleaseNotes = release.Notes |> String.toLines
                NuGet.NuGet.NuGetParams.Dependencies =
                    (if package <> "FAKE.Core" && package <> projectName && package <> "FAKE.Lib" then
                       ["FAKE.Core", NuGet.NuGet.RequireExactly (String.NormalizeVersion release.AssemblyVersion)]
                     else p.Dependencies )
                NuGet.NuGet.NuGetParams.Publish = false }

        NuGet.NuGet.NuGet setParams "fake.nuspec"
        !! (nugetToolsDir @@ "FAKE.exe") |> set64BitCorFlags
        NuGet.NuGet.NuGet (setParams >> x64ify) "fake.nuspec"
)

let netCoreProjs =
    !! (appDir </> "*/*.fsproj")

let runtimes =
  [ "win7-x86"; "win7-x64"; "osx.10.11-x64"; "ubuntu.14.04-x64"; "ubuntu.16.04-x64" ]

module CircleCi =
    let isCircleCi = Environment.environVarAsBool "CIRCLECI"

Target.create "DotNetPackage_" (fun _ ->
    // This line actually ensures we get the correct version checked in
    // instead of the one previously bundled with 'fake`
    Git.CommandHelper.gitCommand "" "checkout .paket/Paket.Restore.targets"

    let nugetDir = System.IO.Path.GetFullPath nugetDncDir

    //Environment.setEnvironVar "IncludeSource" "true"
    //Environment.setEnvironVar "IncludeSymbols" "false"
    Environment.setEnvironVar "GenerateDocumentationFile" "true"
    Environment.setEnvironVar "PackageVersion" release.NugetVersion
    Environment.setEnvironVar "Version" release.NugetVersion
    Environment.setEnvironVar "Authors" (String.separated ";" authors)
    Environment.setEnvironVar "Description" projectDescription
    Environment.setEnvironVar "PackageReleaseNotes" (release.Notes |> String.toLines)
    Environment.setEnvironVar "PackageTags" "build;fake;f#"
    Environment.setEnvironVar "PackageIconUrl" "https://raw.githubusercontent.com/fsharp/FAKE/fee4f05a2ee3c646979bf753f3b1f02d927bfde9/help/content/pics/logo.png"
    Environment.setEnvironVar "PackageProjectUrl" "https://github.com/fsharp/Fake"
    Environment.setEnvironVar "PackageLicenseUrl" "https://github.com/fsharp/FAKE/blob/d86e9b5b8e7ebbb5a3d81c08d2e59518cf9d6da9/License.txt"


    // dotnet pack
    DotNet.pack (fun c ->
        { c with
            Configuration = DotNet.Release
            OutputPath = Some nugetDir
            Common =
                if CircleCi.isCircleCi then
                    { c.Common with CustomParams = Some "/m:1" }
                else c.Common
        } |> dtntSmpl) "Fake.sln"

    let info = DotNet.info dtntSmpl

    // dotnet publish
    runtimes
    |> List.map Some
    |> (fun rs -> None :: rs)
    |> Seq.iter (fun runtime ->
        !! (appDir </> "Fake.netcore/Fake.netcore.fsproj")
        |> Seq.iter(fun proj ->
            let projName = Path.GetFileName(Path.GetDirectoryName proj)
            let runtimeName, runtime =
                match runtime with
                | Some r -> r, r
                | None -> "current", info.RID

            //DotNetRestore (fun c -> {c with Runtime = Some runtime}) proj
            let outDir = nugetDir @@ projName @@ runtimeName
            DotNet.publish (fun c ->
                { c with
                    Runtime = Some runtime
                    Configuration = DotNet.Release
                    OutputPath = Some outDir
                } |> dtntSmpl) proj
            let source = outDir </> "dotnet"
            if File.Exists source then
                failwithf "Workaround no longer required?" //TODO: If this is not triggered delete this block
                Trace.traceFAKE "Workaround https://github.com/dotnet/cli/issues/6465"
                let target = outDir </> "fake"
                if File.Exists target then File.Delete target
                File.Move(source, target)
        )
    )

    // Publish portable as well (see https://docs.microsoft.com/en-us/dotnet/articles/core/app-types)
    let netcoreFsproj = appDir </> "Fake.netcore/Fake.netcore.fsproj"
    let outDir = nugetDir @@ "Fake.netcore" @@ "portable"
    DotNet.publish (fun c ->
        { c with
            Framework = Some "netcoreapp2.0"
            OutputPath = Some outDir
        } |> dtntSmpl) netcoreFsproj
)

Target.create "DotNetCoreCreateZipPackages" (fun _ ->
    Environment.setEnvironVar "Version" release.NugetVersion

    // build zip packages
    !! "nuget/dotnetcore/*.nupkg"
    -- "nuget/dotnetcore/*.symbols.nupkg"
    |> Zip.zip "nuget/dotnetcore" "nuget/dotnetcore/Fake.netcore/fake-dotnetcore-packages.zip"

    ("portable" :: runtimes)
    |> Seq.iter (fun runtime ->
        let runtimeDir = sprintf "nuget/dotnetcore/Fake.netcore/%s" runtime
        !! (sprintf "%s/**" runtimeDir)
        |> Zip.zip runtimeDir (sprintf "nuget/dotnetcore/Fake.netcore/fake-dotnetcore-%s.zip" runtime)
    )
)

Target.create "DotNetCoreCreateChocolateyPackage" (fun _ ->
    // !! ""
    Directory.ensure "nuget/dotnetcore/chocolatey"
    Choco.packFromTemplate (fun p ->
        { p with
            PackageId = "fake"
            ReleaseNotes = release.Notes |> String.toLines
            InstallerType = Choco.ChocolateyInstallerType.SelfContained
            Version = release.NugetVersion
            Files =
                [ (System.IO.Path.GetFullPath @"nuget\dotnetcore\Fake.netcore\win7-x86") + @"\**", Some "bin", None
                  (System.IO.Path.GetFullPath @"src\VERIFICATION.txt"), Some "VERIFICATION.txt", None
                  (System.IO.Path.GetFullPath @"License.txt"), Some "LICENSE.txt", None ]
            OutputDir = "nuget/dotnetcore/chocolatey" }) "src/Fake-choco-template.nuspec"
    ()
)
Target.create "DotNetCorePushChocolateyPackage" (fun _ ->
    let path = sprintf "nuget/dotnetcore/chocolatey/%s.%s.nupkg" "fake" release.NugetVersion
    path |> Choco.push (fun p ->
        { p with
            Source = "https://push.chocolatey.org/"
            ApiKey = Environment.environVarOrFail "CHOCOLATEY_API_KEY" })
)

Target.create "CheckReleaseSecrets" (fun _ ->
    Environment.environVarOrFail "CHOCOLATEY_API_KEY" |> ignore
    Environment.environVarOrFail "nugetkey" |> ignore
    Environment.environVarOrFail "github_user" |> ignore
    Environment.environVarOrFail "github_token" |> ignore
)
let executeFPM args =
    printfn "%s %s" "fpm" args
    Shell.Exec("fpm", args=args, dir="bin")

type SourceType =
    | Dir of source:string * target:string
type DebPackageManifest =
    {
        SourceType : SourceType
        Name : string
        Version : string
        Dependencies : (string * string option) list
        BeforeInstall : string option
        AfterInstall : string option
        ConfigFile : string option
        AdditionalOptions: string list
        AdditionalArgs : string list
    }
(*
See https://www.debian.org/doc/debian-policy/ch-maintainerscripts.html
Ask @theangrybyrd (slack)

{
    SourceType = Dir("./MyCoolApp", "/opt/")
    Name = "mycoolapp"
    Version = originalVersion
    Dependencies = [("mono-devel", None)]
    BeforeInstall = "../deploy/preinst" |> Some
    AfterInstall = "../deploy/postinst" |> Some
    ConfigFile = "/etc/mycoolapp/default.conf" |> Some
    AdditionalOptions = []
    AdditionalArgs =
        [ "../deplo/mycoolapp.service=/lib/systemd/system/" ]
}
23:08
so thats stuff i you want to setup like users or what not
23:09
adding to your path would be in the after script postinst
23:10
setting permissions also, its just a shell script
23:10
might also want a prerm and postrm if you want to play nice on cleanup
*)

Target.create "DotNetCoreCreateDebianPackage" (fun _ ->
    let createDebianPackage (manifest : DebPackageManifest) =
        let argsList = ResizeArray<string>()
        argsList.Add <| match manifest.SourceType with
                        | Dir (_) -> "-s dir"
        argsList.Add <| "-t deb"
        argsList.Add <| "-f"
        argsList.Add <| (sprintf "-n %s" manifest.Name)
        argsList.Add <| (sprintf "-v %s" (manifest.Version.Replace("-","~")))
        let dependency name version =
            match version with
            | Some v -> sprintf "-d '%s %s'" name v
            | None  -> sprintf "-d '%s'" name
        argsList.AddRange <| (Seq.map(fun (a,b) -> dependency a b) manifest.Dependencies)
        manifest.BeforeInstall |> Option.iter(sprintf "--before-install %s" >> argsList.Add)
        manifest.AfterInstall |> Option.iter(sprintf "--after-install %s" >> argsList.Add)
        manifest.ConfigFile |> Option.iter(sprintf "--config-files %s" >> argsList.Add)
        argsList.AddRange <| manifest.AdditionalOptions
        argsList.Add <| match manifest.SourceType with
                        | Dir (source,target) -> sprintf "%s=%s" source target
        argsList.AddRange <| manifest.AdditionalArgs
        if argsList |> String.concat " " |> executeFPM <> 0 then
            failwith "Failed creating deb package"
    ignore createDebianPackage
    ()

)

let nuget_exe = Directory.GetCurrentDirectory() </> "packages" </> "build" </> "NuGet.CommandLine" </> "tools" </> "NuGet.exe"
let apikey = Environment.environVarOrDefault "nugetkey" ""
let nugetsource = Environment.environVarOrDefault "nugetsource" "https://www.nuget.org/api/v2/package"
let rec nugetPush tries nugetpackage =
    try
        if not <| System.String.IsNullOrEmpty apikey then
            Process.execSimple (fun info ->
            { info with
                FileName = nuget_exe
                Arguments = sprintf "push %s %s -Source %s" (Process.toParam nugetpackage) (Process.toParam apikey) (Process.toParam nugetsource) }
            )
                (System.TimeSpan.FromMinutes 10.)
            |> (fun r -> if r <> 0 then failwithf "failed to push package %s" nugetpackage)
        else Trace.traceFAKE "could not push '%s', because api key was not set" nugetpackage
    with exn when tries > 1 ->
        Trace.traceFAKE "Error while pushing NuGet package: %s" exn.Message
        nugetPush (tries - 1) nugetpackage

Target.create "DotNetCorePushNuGet" (fun _ ->
    // dotnet pack
    netCoreProjs
    -- (appDir </> "Fake.netcore/*.fsproj")
    |> Seq.iter(fun proj ->
        let projName = Path.GetFileName(Path.GetDirectoryName proj)
        !! (sprintf "nuget/dotnetcore/%s.*.nupkg" projName)
        -- (sprintf "nuget/dotnetcore/%s.*.symbols.nupkg" projName)
        |> Seq.iter (nugetPush 4))
)

Target.create "PublishNuget" (fun _ ->
    // uses NugetKey environment variable.
    // Timeout atm
    Paket.push(fun p ->
        { p with
            DegreeOfParallelism = 2
            WorkingDir = nugetLegacyDir })
    //!! (nugetLegacyDir </> "**/*.nupkg")
    //|> Seq.iter nugetPush
)

Target.create "ReleaseDocs" (fun _ ->
    Shell.CleanDir "gh-pages"
    let url = Environment.environVarOrDefault "fake_git_url" "https://github.com/fsharp/FAKE.git"
    Git.Repository.cloneSingleBranch "" url "gh-pages" "gh-pages"

    Git.Repository.fullclean "gh-pages"
    Shell.CopyRecursive "docs" "gh-pages" true |> printfn "%A"
    Shell.CopyFile "gh-pages" "./Samples/FAKE-Calculator.zip"
    Git.Staging.stageAll "gh-pages"
    Git.Commit.exec "gh-pages" (sprintf "Update generated documentation %s" release.NugetVersion)
    Git.Branches.push "gh-pages"
)

Target.create "FastRelease" (fun _ ->

    Git.Staging.stageAll ""
    Git.Commit.exec "" (sprintf "Bump version to %s" release.NugetVersion)
    let branch = Git.Information.getBranchName ""
    Git.Branches.pushBranch "" "origin" branch

    Git.Branches.tag "" release.NugetVersion
    Git.Branches.pushTag "" "origin" release.NugetVersion

    let token =
        match Environment.environVarOrDefault "github_token" "" with
        | s when not (System.String.IsNullOrWhiteSpace s) -> s
        | _ -> failwith "please set the github_token environment variable to a github personal access token with repro access."

    let files =
        runtimes @ [ "portable"; "packages" ]
        |> List.map (fun n -> sprintf "nuget/dotnetcore/Fake.netcore/fake-dotnetcore-%s.zip" n)

    GitHub.createClientWithToken token
    |> GitHub.draftNewRelease gitOwner gitName release.NugetVersion (release.SemVer.PreRelease <> None) release.Notes
    |> GitHub.uploadFiles files
    |> GitHub.publishDraft
    |> Async.RunSynchronously
)

open System
Target.create "PrintColors" (fun _ ->
  let color (color: ConsoleColor) (code : unit -> _) =
      let before = Console.ForegroundColor
      try
        Console.ForegroundColor <- color
        code ()
      finally
        Console.ForegroundColor <- before
  color ConsoleColor.Magenta (fun _ -> printfn "TestMagenta")
)
Target.create "FailFast" (fun _ -> failwith "fail fast")
Target.create "EnsureTestsRun" (fun _ ->
//#if !DOTNETCORE
//  if Environment.hasEnvironVar "SkipIntegrationTests" || Environment.hasEnvironVar "SkipTests" then
//      let res = getUserInput "Are you really sure to continue without running tests (yes/no)?"
//      if res <> "yes" then
//          failwith "cannot continue without tests"
//#endif
  ()
)
Target.create "Default" ignore
Target.create "StartDnc" ignore
Target.create "Release" ignore
Target.create "BuildSolution" ignore
Target.create "DotNetPackage" ignore
Target.create "AfterBuild" ignore
Target.create "FullDotNetCore" ignore

open Fake.Core.TargetOperators

"CheckReleaseSecrets"
    ?=> "Clean"

// DotNet Core Build
"Clean"
    ?=> "StartDnc"
    ?=> "DownloadPaket"
    ?=> "SetAssemblyInfo"
    ==> "DotNetPackage_"
    ?=> "UnskipAndRevertAssemblyInfo"
    ==> "DotNetPackage"
"StartDnc"
    ==> "DotNetPackage_"
"DownloadPaket"
    ==> "DotNetPackage_"
"DotNetPackage_"
    ==> "DotNetPackage"
// Full framework build
"Clean"
    ?=> "RenameFSharpCompilerService"
    ?=> "SetAssemblyInfo"
    ==> "BuildSolution_"
    ?=> "UnskipAndRevertAssemblyInfo"
    ==> "BuildSolution"
"RenameFSharpCompilerService"
    ==> "BuildSolution_"
"BuildSolution_"
    ==> "BuildSolution"
// AfterBuild -> Both Builds completed
"BuildSolution"
    ==> "AfterBuild"
"DotNetPackage"
    ==> "AfterBuild"

// Create artifacts when build is finished
"AfterBuild"
    =?> ("CreateNuGet", Environment.isWindows)
    ==> "CopyLicense"
    =?> ("DotNetCoreCreateChocolateyPackage", Environment.isWindows)
    =?> ("GenerateDocs", BuildServer.isLocalBuild && Environment.isWindows)
    ==> "Default"

// Test the full framework build
"BuildSolution"
    =?> ("Test", not <| Environment.hasEnvironVar "SkipTests")
    =?> ("BootstrapTest", not disableBootstrap && not <| Environment.hasEnvironVar "SkipTests")
    ==> "Default"

// Test the dotnetcore build
"DotNetPackage"
    =?> ("DotNetCoreUnitTests",not <| Environment.hasEnvironVar "SkipTests")
    ==> "DotNetCoreCreateZipPackages"
    =?> ("DotNetCoreIntegrationTests", not <| Environment.hasEnvironVar "SkipIntegrationTests" && not <| Environment.hasEnvironVar "SkipTests")
    =?> ("BootstrapTestDotNetCore", not disableBootstrap && not <| Environment.hasEnvironVar "SkipTests")
    ==> "FullDotNetCore"
    ==> "Default"

// Release stuff ('FastRelease' is to release after running 'Default')
"EnsureTestsRun"
    =?> ("DotNetCorePushChocolateyPackage", Environment.isWindows)
    =?> ("ReleaseDocs", BuildServer.isLocalBuild && Environment.isWindows)
    ==> "DotNetCorePushNuGet"
    ==> "PublishNuget"
    ==> "FastRelease"

// If 'Default' happens it needs to happen before 'EnsureTestsRun'
"Default"
    ?=> "EnsureTestsRun"

// A 'Default' includes a 'Clean'
"Clean"
    ==> "Default"

// A 'Release' includes a 'Default'
"Default"
    ==> "Release"
// A 'Release' includes a 'FastRelease'
"FastRelease"
    ==> "Release"
// A 'Release' includes a 'CheckReleaseSecrets'
"CheckReleaseSecrets"
    ==> "Release"

// start build
Target.runOrDefault "Default"
