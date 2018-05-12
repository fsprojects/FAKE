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
nuget Suave
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
#I "packages/build/FAKE/tools/"
#r "FakeLib.dll"
#r "Paket.Core.dll"
#r "packages/build/System.Net.Http/lib/net46/System.Net.Http.dll"
#r "packages/build/Octokit/lib/net45/Octokit.dll"
#I "packages/build/SourceLink.Fake/tools/"

#r "System.IO.Compression"
//#load "packages/build/SourceLink.Fake/tools/SourceLink.fsx"

#endif

//#if !FAKE
//let execContext = Fake.Core.Context.FakeExecutionContext.Create false "build.fsx" []
//Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
//#endif
// #load "src/app/Fake.DotNet.FSFormatting/FSFormatting.fs"
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
(*
let version =
    let semVer = SemVer.parse release.NugetVersion
    match semVer.PreRelease with
    | None -> ()
    | _ -> ()*)

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

let nuget_exe = Directory.GetCurrentDirectory() </> "packages" </> "build" </> "NuGet.CommandLine" </> "tools" </> "NuGet.exe"
let apikey = Environment.environVarOrDefault "nugetkey" ""
let nugetsource = Environment.environVarOrDefault "nugetsource" "https://www.nuget.org/api/v2/package"
let artifactsDir = Environment.environVarOrDefault "artifactsdirectory" ""
let fromArtifacts = not <| String.isNullOrEmpty artifactsDir

module MyGitLab =
    let isGitLabCi = Environment.environVar "GITLAB_CI" = "true"
    /// Implements a TraceListener for TeamCity build servers.
    /// ## Parameters
    ///  - `importantMessagesToStdErr` - Defines whether to trace important messages to StdErr.
    ///  - `colorMap` - A function which maps TracePriorities to ConsoleColors.
    type internal GitLabTraceListener() =

        interface ITraceListener with
            /// Writes the given message to the Console.
            member __.Write msg = 
                let color = ConsoleWriter.colorMap msg
                let importantMessagesToStdErr = true
                let write = ConsoleWriter.writeAnsiColor //else ConsoleWriter.write
                match msg with
                | TraceData.ImportantMessage text | TraceData.ErrorMessage text ->
                    write importantMessagesToStdErr color true text
                | TraceData.LogMessage(text, newLine) | TraceData.TraceMessage(text, newLine) ->
                    write false color newLine text
                | TraceData.OpenTag (tag, descr) ->
                    write false color true (sprintf "Starting %s '%s': %s" tag.Type tag.Name descr)
                | TraceData.CloseTag (tag, time) ->
                    write false color true (sprintf "Finished '%s' in %O" tag.Name time)
                | TraceData.ImportData (typ, path) ->
                    let name = Path.GetFileName path
                    let target = Path.Combine("artifacts", name)
                    let targetDir = Path.GetDirectoryName target
                    Directory.ensure targetDir
                    Shell.cp_r path target
                    write false color true (sprintf "Import data '%O': %s -> %s" typ path target)
                | TraceData.TestOutput (test, out, err) ->
                    write false color true (sprintf "Test '%s' output:\n\tOutput: %s\n\tError: %s" test out err)
                | TraceData.BuildNumber number ->
                    write false color true (sprintf "Build Number: %s" number)
                | TraceData.TestStatus (test, status) ->
                    write false color true (sprintf "Test '%s' status: %A" test status)

    let defaultTraceListener =
      GitLabTraceListener() :> ITraceListener
    let detect () = isGitLabCi
    let install(force:bool) =
        if not (detect()) then failwithf "Cannot run 'install()' on a non-AppVeyor environment"
        if force || not (CoreTracing.areListenersSet()) then
            CoreTracing.setTraceListeners [defaultTraceListener]
        () 
    let Installer =
        { new BuildServerInstaller() with
            member __.Install () = install (false)
            member __.Detect () = detect() }

BuildServer.install [
    AppVeyor.Installer
    TeamCity.Installer
    Travis.Installer
    TeamFoundation.Installer
    MyGitLab.Installer
]

//let current = CoreTracing.getListeners()
//if current |> Seq.contains CoreTracing.defaultConsoleTraceListener |> not then
//    CoreTracing.setTraceListeners (CoreTracing.defaultConsoleTraceListener :: current)

let dotnetSdk = lazy DotNet.install DotNet.Release_2_1_4
let inline dtntWorkDir wd =
    DotNet.Options.lift dotnetSdk.Value
    >> DotNet.Options.withWorkingDirectory wd
let inline dtntSmpl arg = DotNet.Options.lift dotnetSdk.Value arg

let publish f =
    Trace.publish ImportData.BuildArtifact (Path.GetFullPath f)

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

Target.create "WorkaroundPaketNuspecBug" (fun _ ->
    // Workaround https://github.com/fsprojects/Paket/issues/2830
    // https://github.com/fsprojects/Paket/issues/2689
    // Basically paket fails if there is already an existing nuspec in obj/ dir because then MSBuild will call paket with multiple nuspec file arguments separated by ';'
    !! "src/*/*/obj/**/*.nuspec"
    -- (sprintf "src/*/*/obj/**/*%s.nuspec" release.NugetVersion)
    |> File.deleteAll
)

// Targets
Target.create "Clean" (fun _ ->
    !! "src/*/*/bin"
    //++ "src/*/*/obj"
    |> Shell.cleanDirs

    Shell.cleanDirs [buildDir; testDir; docsDir; apidocsDir; nugetDncDir; nugetLegacyDir; reportDir]

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
      "Fake.Api.GitHub", "GitHub Client API Support via Octokit"
      "Fake.Api.HockeyApp", "HockeyApp Integration Support"
      "Fake.Api.Slack", "Slack Integration Support"
      "Fake.Azure.CloudServices", "Azure Cloud Services Support"
      "Fake.Azure.Emulators", "Azure Emulators Support"
      "Fake.Azure.Kudu", "Azure Kudu Support"
      "Fake.Azure.WebJobs", "Azure Web Jobs Support"
      "Fake.BuildServer.AppVeyor", "Integration into AppVeyor buildserver"
      "Fake.BuildServer.GitLab", "Integration into GitLab-CI buildserver"
      "Fake.BuildServer.TeamCity", "Integration into TeamCity buildserver"
      "Fake.BuildServer.TeamFoundation", "Integration into TeamFoundation buildserver"
      "Fake.BuildServer.Travis", "Integration into Travis buildserver"
      "Fake.Core.CommandLineParsing", "Core commandline parsing support via docopt like syntax"
      "Fake.Core.Context", "Core Context Infrastructure"
      "Fake.Core.Environment", "Environment Detection"
      "Fake.Core.Process", "Starting and managing Processes"
      "Fake.Core.ReleaseNotes", "Parsing ReleaseNotes"
      "Fake.Core.SemVer", "Parsing and working with SemVer"
      "Fake.Core.String", "Core String manipulations"
      "Fake.Core.Target", "Defining and running Targets"
      "Fake.Core.Tasks", "Repeating and managing Tasks"
      "Fake.Core.Trace", "Core Logging functionality"
      "Fake.Core.Xml", "Core Xml functionality"
      "Fake.Documentation.DocFx", "Documentation with DocFx"
      "Fake.DotNet.AssemblyInfoFile", "Writing AssemblyInfo files"
      "Fake.DotNet.Cli", "Running the dotnet cli"
      "Fake.DotNet.Fsc", "Running the f# compiler - fsc"
      "Fake.DotNet.FSFormatting", "Running fsformatting.exe and generating documentation"
      "Fake.DotNet.Mage", "Manifest Generation and Editing Tool"
      "Fake.DotNet.MSBuild", "Running msbuild"
      "Fake.DotNet.NuGet", "Running NuGet Client and interacting with NuGet Feeds"
      "Fake.DotNet.Paket", "Running Paket and publishing packages"
      "Fake.DotNet.Testing.Expecto", "Running expecto test runner"
      "Fake.DotNet.Testing.MSpec", "Running mspec test runner"
      "Fake.DotNet.Testing.MSTest", "Running mstest test runner"
      "Fake.DotNet.Testing.NUnit", "Running nunit test runner"
      "Fake.DotNet.Testing.OpenCover", "Code coverage with OpenCover"
      "Fake.DotNet.Testing.SpecFlow", "BDD with Gherkin and SpecFlow"
      "Fake.DotNet.Testing.XUnit2", "Running xunit test runner"
      "Fake.DotNet.Xamarin", "Running Xamarin builds"
      "Fake.Installer.InnoSetup", "Creating installers with InnoSetup"
      "Fake.IO.FileSystem", "Core Filesystem utilities and globbing support"
      "Fake.IO.Zip", "Core Zip functionality"
      "Fake.JavaScript.Npm", "Running npm commands"
      "Fake.JavaScript.Yarn", "Running Yarn commands"
      "Fake.Net.Http", "HTTP Client"
      "Fake.netcore", "Command line tool"
      "Fake.Runtime", "Core runtime features"
      "Fake.Sql.DacPac", "Sql Server Data Tools DacPac operations"
      "Fake.Testing.Common", "Common testing data types"
      "Fake.Testing.ReportGenerator", "Convert XML coverage output to various formats"
      "Fake.Testing.SonarQube", "Analyzing your project with SonarQube"
      "Fake.Tools.Git", "Running git commands"
      "Fake.Tools.Pickles", "Convert Gherkin to HTML"
      "Fake.Tracing.NAntXml", "NAntXml"
      "Fake.Windows.Chocolatey", "Running and packaging with Chocolatey"
      "Fake.Windows.Registry", "CRUD functionality for Windows registry" ]

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

Target.create "_BuildSolution" (fun _ ->
    MSBuild.runWithDefaults "Build" ["./src/Legacy-FAKE.sln"; "./src/Legacy-FAKE.Deploy.Web.sln"]
    |> Trace.logItems "AppBuild-Output: "
    
    // TODO: Check if we run the test in the current build!
    Directory.ensure "temp"
    let testZip = "temp/tests-legacy.zip"
    !! "test/**"
    |> Zip.zip "." testZip
    publish testZip
)

Target.create "GenerateDocs" (fun _ ->
    Shell.cleanDir docsDir
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

    let layoutRoots = [ "./help/templates"; "./help/templates/reference"]
    let fake5LayoutRoots = "./help/templates/fake5" :: layoutRoots
    let legacyLayoutRoots = "./help/templates/legacy" :: layoutRoots
    let fake4LayoutRoots = "./help/templates/fake4" :: layoutRoots

    Shell.copyDir (docsDir) "help/content" FileFilter.allFiles
    // to skip circleci builds
    let docsCircleCi = docsDir + "/.circleci"
    Directory.ensure docsCircleCi
    Shell.copyDir docsCircleCi ".circleci" FileFilter.allFiles
    File.writeString false "./docs/.nojekyll" ""
    File.writeString false "./docs/CNAME" "fake.build"
    //CopyDir (docsDir @@ "pics") "help/pics" FileFilter.allFiles

    Shell.copy (source @@ "markdown") ["RELEASE_NOTES.md"]
    FSFormatting.createDocs (fun s ->
        { s with
            Source = source @@ "markdown"
            OutputDirectory = docsDir
            Template = docsTemplate
            ProjectParameters = ("CurrentPage", "Modules") :: projInfo
            LayoutRoots = layoutRoots })
    FSFormatting.createDocs (fun s ->
        { s with
            Source = source @@ "redirects"
            OutputDirectory = docsDir
            Template = docsTemplate
            ProjectParameters = ("CurrentPage", "FAKE-4") :: projInfo
            LayoutRoots = layoutRoots })
    FSFormatting.createDocs (fun s ->
        { s with
            Source = source @@ "startpage"
            OutputDirectory = docsDir
            Template = indexTemplate
            // TODO: CurrentPage shouldn't be required as it's written in the template, but it is -> investigate
            ProjectParameters = ("CurrentPage", "Home") :: projInfo
            LayoutRoots = layoutRoots })

    Directory.ensure apidocsDir

    let dllsAndLibDirs (dllPattern:IGlobbingPattern) = 
        let dlls = 
            dllPattern
            |> Seq.distinctBy Path.GetFileName
            |> List.ofSeq
        let libDirs = 
            dlls
            |> Seq.map Path.GetDirectoryName
            |> Seq.distinct
            |> List.ofSeq
        (dlls,libDirs)         
    // FAKE 5 module documentation
    let fake5ApidocsDir = apidocsDir @@ "v5"
    Directory.ensure fake5ApidocsDir
    
    let fake5Dlls, fake5LibDirs = 
        !! "./src/app/Fake.*/bin/Release/**/Fake.*.dll" 
        |> dllsAndLibDirs
 
    fake5Dlls
    |> FSFormatting.createDocsForDlls (fun s ->
        { s with
            OutputDirectory = fake5ApidocsDir
            LayoutRoots =  fake5LayoutRoots
            LibDirs = fake5LibDirs
            // TODO: CurrentPage shouldn't be required as it's written in the template, but it is -> investigate
            ProjectParameters = ("api-docs-prefix", "/apidocs/v5/") :: ("CurrentPage", "APIReference") :: projInfo
            SourceRepository = githubLink + "/blob/master" })

    // Compat urls
    let redirectPage newPage =
        sprintf """
<html>
	<head>
		<title>Redirecting</title>
		<meta charset="utf-8" />
		<meta name="viewport" content="width=device-width, initial-scale=1" />
	</head>
    <body>
        <p><a href="%s">This page has moved here...</a></p>
        <script type="text/javascript">
            var url = "%s";
            window.location.replace(url);
        </script>
    </body>
</html>"""  newPage newPage

    !! (fake5ApidocsDir + "/*.html")
    |> Seq.iter (fun v5File ->
        // ./docs/apidocs/v5/blub.html
        let name = Path.GetFileName v5File
        let v4Name = Path.GetDirectoryName (Path.GetDirectoryName v5File) @@ name
        // ./docs/apidocs/blub.html
        let link = sprintf "/apidocs/v5/%s" name
        File.WriteAllText(v4Name, redirectPage link)
    )

    // FAKE 5 legacy documentation
    let fake5LegacyApidocsDir = apidocsDir @@ "v5/legacy"
    Directory.ensure fake5LegacyApidocsDir
    let fake5LegacyDlls, fake5LegacyLibDirs = 
        !! "./build/**/Fake.*.dll"
          ++ "./build/FakeLib.dll"
          -- "./build/**/Fake.Experimental.dll"
          -- "./build/**/FSharp.Compiler.Service.dll"
          -- "./build/**/netcore/FAKE.FSharp.Compiler.Service.dll"
          -- "./build/**/FAKE.FSharp.Compiler.Service.dll"
          -- "./build/**/Fake.IIS.dll"
          -- "./build/**/Fake.Deploy.Lib.dll"
        |> dllsAndLibDirs

    fake5LegacyDlls
    |> FSFormatting.createDocsForDlls (fun s ->
        { s with
            OutputDirectory = fake5LegacyApidocsDir
            LayoutRoots = legacyLayoutRoots
            LibDirs = fake5LegacyLibDirs
            // TODO: CurrentPage shouldn't be required as it's written in the template, but it is -> investigate
            ProjectParameters = ("api-docs-prefix", "/apidocs/v5/legacy/") :: ("CurrentPage", "APIReference") :: projInfo
            SourceRepository = githubLink + "/blob/master" })

    // FAKE 4 legacy documentation
    let fake4LegacyApidocsDir = apidocsDir @@ "v4"
    Directory.ensure fake4LegacyApidocsDir
    let fake4LegacyDlls, fake4LegacyLibDirs =
        !! "./packages/docs/FAKE/tools/Fake.*.dll"
          ++ "./packages/docs/FAKE/tools/FakeLib.dll"
          -- "./packages/docs/FAKE/tools/Fake.Experimental.dll"
          -- "./packages/docs/FAKE/tools/FSharp.Compiler.Service.dll"
          -- "./packages/docs/FAKE/tools/FAKE.FSharp.Compiler.Service.dll"
          -- "./packages/docs/FAKE/tools/Fake.IIS.dll"
          -- "./packages/docs/FAKE/tools/Fake.Deploy.Lib.dll"
        |> dllsAndLibDirs

    fake4LegacyDlls
    |> FSFormatting.createDocsForDlls (fun s ->
        { s with
            OutputDirectory = fake4LegacyApidocsDir
            LayoutRoots = fake4LayoutRoots
            LibDirs = fake4LegacyLibDirs
            // TODO: CurrentPage shouldn't be required as it's written in the template, but it is -> investigate
            ProjectParameters = ("api-docs-prefix", "/apidocs/v4/") ::("CurrentPage", "APIReference") :: projInfo
            SourceRepository = githubLink + "/blob/hotfix_fake4" })
)

#if DOTNETCORE
let startWebServer () =
    let rec findPort port =
        let portIsTaken = false
            //if Environment.isMono then false else
            //System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
            //|> Seq.exists (fun x -> x.Port = port)

        if portIsTaken then findPort (port + 1) else port

    let port = findPort 8083
    let serverConfig = 
        { Suave.Web.defaultConfig with
           homeFolder = Some (Path.GetFullPath docsDir)
           bindings = [ Suave.Http.HttpBinding.createSimple Suave.Http.Protocol.HTTP "127.0.0.1" port ]
        }
    let (>=>) = Suave.Operators.(>=>)    
    let app =
      Suave.WebPart.choose [
        //Filters.path "/websocket" >=> handShake socketHandler
        Suave.Writers.setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
        >=> Suave.Writers.setHeader "Pragma" "no-cache"
        >=> Suave.Writers.setHeader "Expires" "0"
        >=> Suave.Files.browseHome ]
    Suave.Web.startWebServerAsync serverConfig app |> snd |> Async.Start
    let psi = System.Diagnostics.ProcessStartInfo(sprintf "http://localhost:%d/index.html" port)
    psi.UseShellExecute <- true
    System.Diagnostics.Process.Start (psi) |> ignore

Target.create "HostDocs" (fun _ -> 
    startWebServer()
    Trace.traceImportant "Press any key to stop."
    System.Console.ReadKey() |> ignore
)
#endif

Target.create "CopyLicense" (fun _ ->
    Shell.copyTo buildDir additionalFiles
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

    let processResult =
        DotNet.exec (dtntWorkDir root) "src/test/Fake.Core.IntegrationTests/bin/Release/netcoreapp2.0/Fake.Core.IntegrationTests.dll" "--summary"

    if processResult.ExitCode <> 0 then failwithf "DotNet Core Integration tests failed."
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
            [ ".fake/testbuild.fsx/packages"
              ".fake/testbuild.fsx/paket.depedencies.sha1"
              ".fake/testbuild.fsx/paket.lock"
              "testbuild.fsx.lock" ]
            |> List.iter Shell.rm_rf
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

        Shell.copyFile (buildDir </> filename) targetFile

    internalizeIn "FAKE.exe"

    !! (buildDir </> "FSharp.Compiler.Service.**")
    |> Seq.iter File.delete

    Shell.deleteDir buildMergedDir
)

Target.create "CreateNuGet" (fun _ ->
    let path =
        if Environment.isWindows 
        then "lib" @@ "corflags.exe"
        else "lib" @@ "xCorFlags.exe"
    let set64BitCorFlags files =
        files
        |> Seq.iter (fun file ->
            let exitCode =
                Process.execSimple (fun proc -> 
                { proc with
                    FileName = Path.GetFullPath path
                    WorkingDirectory = Path.GetDirectoryName file
                    Arguments = "/32BIT- /32BITPREF- " + Process.quoteIfNeeded file
                    }
                |> Process.withFramework) (System.TimeSpan.FromMinutes 1.)
            if exitCode <> 0 then failwithf "corflags.exe failed with %d" exitCode)

    let x64ify (package:NuGet.NuGet.NuGetParams) =
        { package with
            Dependencies = package.Dependencies |> List.map (fun (pkg, ver) -> pkg + ".x64", ver)
            Project = package.Project + ".x64" }

    let nugetExe =
        let pref = Path.GetFullPath "packages/build/NuGet.CommandLine/tools/NuGet.exe"
        if File.Exists pref then pref
        else
            let rec printDir space d =
                for f in Directory.EnumerateFiles d do
                    Trace.tracefn "%sFile: %s" space f
                for sd in Directory.EnumerateDirectories d do
                    Trace.tracefn "%sDirectory: %s" space sd
                    printDir (space + "  ") d
            printDir "  " (Path.GetFullPath "packages")
            match !! "packages/**/NuGet.exe" |> Seq.tryHead with
            | Some e ->
                Trace.tracefn "Found %s" e
                e
            | None ->
                pref
        
    for package,description in packages do
        let nugetDocsDir = nugetLegacyDir @@ "docs"
        let nugetToolsDir = nugetLegacyDir @@ "tools"
        let nugetLibDir = nugetLegacyDir @@ "lib"
        let nugetLib451Dir = nugetLibDir @@ "net451"

        Shell.cleanDir nugetDocsDir
        Shell.cleanDir nugetToolsDir
        Shell.cleanDir nugetLibDir
        Shell.deleteDir nugetLibDir

        File.delete "./build/FAKE.Gallio/Gallio.dll"

        let deleteFCS _ =
          //!! (dir </> "FSharp.Compiler.Service.**")
          //|> Seq.iter DeleteFile
          ()

        Directory.ensure docsDir
        match package with
        | p when p = projectName ->
            !! (buildDir @@ "**/*.*") |> Shell.copy nugetToolsDir
            Shell.copyDir nugetDocsDir docsDir FileFilter.allFiles
            deleteFCS nugetToolsDir
        | p when p = "FAKE.Core" ->
            !! (buildDir @@ "*.*") |> Shell.copy nugetToolsDir
            Shell.copyDir nugetDocsDir docsDir FileFilter.allFiles
            deleteFCS nugetToolsDir
        | p when p = "FAKE.Lib" ->
            Shell.cleanDir nugetLib451Dir
            {
                Globbing.BaseDirectory = buildDir
                Globbing.Includes = [ "FakeLib.dll"; "FakeLib.XML" ]
                Globbing.Excludes = []
            }
            |> Shell.copy nugetLib451Dir
            deleteFCS nugetLib451Dir
        | _ ->
            Shell.copyDir nugetToolsDir (buildDir @@ package) FileFilter.allFiles
            Shell.copyTo nugetToolsDir additionalFiles
        !! (nugetToolsDir @@ "*.srcsv") |> File.deleteAll


        let setParams (p:NuGet.NuGet.NuGetParams) =
            {p with
                NuGet.NuGet.NuGetParams.ToolPath = nugetExe
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

    let legacyZip = "nuget/fake-legacy-packages.zip"
    !! (nugetLegacyDir </> "**/*.nupkg")
    |> Zip.zip nugetLegacyDir legacyZip
    publish legacyZip
)

let netCoreProjs =
    !! (appDir </> "*/*.fsproj")

let runtimes =
  [ "win7-x86"; "win7-x64"; "osx.10.11-x64"; "ubuntu.14.04-x64"; "ubuntu.16.04-x64" ]

module CircleCi =
    let isCircleCi = Environment.environVarAsBool "CIRCLECI"


// Create target for each runtime
let info = lazy DotNet.info dtntSmpl
runtimes
|> List.map Some
|> (fun rs -> None :: rs)
|> Seq.iter (fun runtime ->
    let runtimeName, runtime =
        match runtime with
        | Some r -> r, lazy r
        | None -> "current", lazy info.Value.RID
    let targetName = sprintf "_DotNetPublish_%s" runtimeName
    Target.create targetName (fun _ ->
        !! (appDir </> "Fake.netcore/Fake.netcore.fsproj")
        |> Seq.iter(fun proj ->
            let nugetDir = System.IO.Path.GetFullPath nugetDncDir
            let projName = Path.GetFileName(Path.GetDirectoryName proj)

            //DotNetRestore (fun c -> {c with Runtime = Some runtime}) proj
            let outDir = nugetDir @@ projName @@ runtimeName
            DotNet.publish (fun c ->
                { c with
                    Runtime = Some runtime.Value
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
)

Target.create "_DotNetPublish_portable" (fun _ ->
    let nugetDir = System.IO.Path.GetFullPath nugetDncDir
    
    // Publish portable as well (see https://docs.microsoft.com/en-us/dotnet/articles/core/app-types)
    let netcoreFsproj = appDir </> "Fake.netcore/Fake.netcore.fsproj"
    let outDir = nugetDir @@ "Fake.netcore" @@ "portable"
    DotNet.publish (fun c ->
        { c with
            Framework = Some "netcoreapp2.0"
            OutputPath = Some outDir
        } |> dtntSmpl) netcoreFsproj
)

Target.create "_DotNetPackage" (fun _ ->
    let nugetDir = System.IO.Path.GetFullPath nugetDncDir
    // This line actually ensures we get the correct version checked in
    // instead of the one previously bundled with 'fake`
    Git.CommandHelper.gitCommand "" "checkout .paket/Paket.Restore.targets"


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

    // TODO: Check if we run the test in the current build!
    Directory.ensure "temp"
    let testZip = "temp/tests.zip"
    !! "src/test/*/bin/Release/netcoreapp2.0/**"
    |> Zip.zip "src/test" testZip
    publish testZip
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
    
    runtimes @ [ "portable"; "packages" ]
    |> List.map (fun n -> sprintf "nuget/dotnetcore/Fake.netcore/fake-dotnetcore-%s.zip" n)
    |> List.iter publish
)

let getChocoWrapper () =
    let altToolPath = Path.GetFullPath "temp/choco.sh"
    if not Environment.isWindows then
        Directory.ensure "temp"
        File.WriteAllText(altToolPath, """#!/bin/bash
docker run --rm -v $PWD:$PWD -w $PWD linuturk/mono-choco $@
"""          )
        let result = Shell.Exec("chmod", sprintf "+x %s" altToolPath)
        if result <> 0 then failwithf "'chmod +x %s' failed on unix" altToolPath
    altToolPath

Target.create "DotNetCoreCreateChocolateyPackage" (fun _ ->
    // !! ""
    let altToolPath = getChocoWrapper()
    let changeToolPath (p: Choco.ChocoPackParams) =
        if Environment.isWindows
        then p
        else { p with ToolPath = altToolPath }
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
            OutputDir = "nuget/dotnetcore/chocolatey" }
        |> changeToolPath) "src/Fake-choco-template.nuspec"

    let chocoPackage = sprintf "nuget/dotnetcore/chocolatey/%s.%s.nupkg" "fake" release.NugetVersion
    let chocoTargetPackage = sprintf "nuget/dotnetcore/chocolatey/chocolatey-%s.%s.nupkg" "fake" release.NugetVersion
    File.Copy(chocoPackage, chocoTargetPackage, true)
    publish chocoTargetPackage
)
Target.create "DotNetCorePushChocolateyPackage" (fun _ ->
    let name = sprintf "%s.%s.nupkg" "fake" release.NugetVersion
    let path = sprintf "nuget/dotnetcore/chocolatey/%s" name
    if not Environment.isWindows && not (File.exists path) && fromArtifacts then
        Directory.ensure "nuget/dotnetcore/chocolatey"
        Shell.copyFile path (artifactsDir </> sprintf "chocolatey-%s" name)

    let altToolPath = getChocoWrapper()
    let changeToolPath (p: Choco.ChocoPushParams) =
        if Environment.isWindows then p else { p with ToolPath = altToolPath }
    path |> Choco.push (fun p ->
        { p with
            Source = "https://push.chocolatey.org/"
            ApiKey = Environment.environVarOrFail "CHOCOLATEY_API_KEY" }
        |> changeToolPath)
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
            PublishUrl = nugetsource
            DegreeOfParallelism = 2
            WorkingDir = nugetLegacyDir })
    //!! (nugetLegacyDir </> "**/*.nupkg")
    //|> Seq.iter nugetPush
)

Target.create "ReleaseDocs" (fun _ ->
    Shell.cleanDir "gh-pages"
    let url = Environment.environVarOrDefault "fake_git_url" "https://github.com/fsharp/FAKE.git"
    Git.Repository.cloneSingleBranch "" url "gh-pages" "gh-pages"

    Git.Repository.fullclean "gh-pages"
    Shell.copyRecursive "docs" "gh-pages" true |> printfn "%A"
    Shell.copyFile "gh-pages" "./Samples/FAKE-Calculator.zip"
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

Target.create "Release_Staging" (fun _ -> ())

open System.IO.Compression
let unzip target (fileName : string) =
    use stream = new FileStream(fileName, FileMode.Open)
    use zipFile = new ZipArchive(stream)
    for zipEntry in zipFile.Entries do
        let unzipPath = Path.Combine(target, zipEntry.FullName)
        let directoryPath = Path.GetDirectoryName(unzipPath)
        if unzipPath.EndsWith "/" then
            Directory.CreateDirectory(unzipPath) |> ignore
        else
            // unzip the file
            Directory.ensure directoryPath
            let zipStream = zipEntry.Open()
            if unzipPath.EndsWith "/" |> not then 
                use unzippedFileStream = File.Create(unzipPath)
                zipStream.CopyTo(unzippedFileStream)

Target.create "PrepareArtifacts" (fun _ ->
    if not fromArtifacts then
        Trace.trace "empty artifactsDir."
    else
        !! (artifactsDir </> "fake-dotnetcore*")
        |> Shell.copy "nuget/dotnetcore/Fake.netcore"

        unzip "nuget/dotnetcore" "nuget/dotnetcore/Fake.netcore/fake-dotnetcore-packages.zip"

        if Environment.isWindows then
            Directory.ensure "nuget/dotnetcore/chocolatey"
            let name = sprintf "%s.%s.nupkg" "fake" release.NugetVersion
            Shell.copyFile (sprintf "nuget/dotnetcore/chocolatey/%s" name) (artifactsDir </> sprintf "chocolatey-%s" name)
        else
            unzip "." (artifactsDir </> "chocolatey-requirements.zip")

        Directory.ensure "nuget/legacy"
        unzip "nuget/legacy" (artifactsDir </> "fake-legacy-packages.zip")

        Directory.ensure "temp/build"
        !! ("nuget" </> "legacy" </> "*.nupkg")
        |> Seq.iter (fun pack ->
            unzip "temp/build" pack
        )
        Shell.copyDir "build" "temp/build" (fun _ -> true)
        
        Directory.ensure "help"
        unzip "help" (artifactsDir </> "help-markdown.zip")

        unzip "src/test" (artifactsDir </> "tests.zip")
)

Target.create "BuildArtifacts" (fun args ->
    Directory.ensure "temp"

    if not Environment.isWindows then
        // Chocolatey package is done in a separate step...
        let chocoReq = "temp/chocolatey-requirements.zip"
        //!! @"nuget\dotnetcore\Fake.netcore\win7-x86\**" already part of fake-dotnetcore-win7-x86
        !! @"src\VERIFICATION.txt"
        ++ @"License.txt"
        ++ "src/Fake-choco-template.nuspec"
        |> Zip.zip "." chocoReq
        publish chocoReq

    let buildCache = "temp/build-cache.zip"
    !! (".fake" </> "build.fsx" </> "*.dll")
    ++ (".fake" </> "build.fsx" </> "*.pdb")
    ++ "build.fsx"
    ++ "paket.dependencies"
    ++ "paket.lock"
    ++ "RELEASE_NOTES.md"
    |> Zip.zip "." buildCache
    publish buildCache

    if args.Context.TryFindPrevious "Release_GenerateDocs" |> Option.isNone then
        // When Release_GenerateDocs is missing upload markdown (for later processing)
        let helpZip = "temp/help-markdown.zip"
        !! ("help" </> "**")
        |> Zip.zip "help" helpZip
        publish helpZip
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
Target.Description "Default Build all artifacts and documentation"
Target.create "Default" ignore
Target.create "_StartDnc" ignore
Target.Description "Simple local command line release"
Target.create "Release" ignore
Target.Description "Build the full-framework (legacy) solution"
Target.create "BuildSolution" ignore
Target.Description "dotnet pack pack to build all nuget packages"
Target.create "DotNetPackage" ignore
Target.create "_AfterBuild" ignore
Target.Description "Build and test the dotnet sdk part (fake 5 - no legacy)"
Target.create "FullDotNetCore" ignore
Target.Description "publish fake 5 runner for various platforms"
Target.create "DotNetPublish" ignore
Target.Description "Run the tests - if artifacts are available via 'artifactsdirectory' those are used."
Target.create "RunTests" ignore
Target.Description "Generate the docs (potentially from artifacts) and publish as artifact."
Target.create "Release_GenerateDocs" (fun _ ->
    let testZip = "temp/docs.zip"
    !! "docs/**"
    |> Zip.zip "docs" testZip
    publish testZip
)

Target.Description "Full Build & Test and publish results as artifacts."
Target.create "Release_BuildAndTest" ignore

open Fake.Core.TargetOperators

"CheckReleaseSecrets"
    ?=> "Clean"
"WorkaroundPaketNuspecBug"
    ==> "Clean"
"WorkaroundPaketNuspecBug"
    ==> "_DotNetPackage"
    
// DotNet Core Build
"Clean"
    ?=> "_StartDnc"
    ?=> "DownloadPaket"
    ?=> "SetAssemblyInfo"
    ==> "_DotNetPackage"
    ?=> "UnskipAndRevertAssemblyInfo"
    ==> "DotNetPackage"
"_StartDnc"
    ==> "_DotNetPackage"
"DownloadPaket"
    ==> "_DotNetPackage"
"_DotNetPackage"
    ==> "DotNetPackage"

let mutable prev = None
for runtime in "current" :: "portable" :: runtimes do
    let rawTargetName = sprintf "_DotNetPublish_%s" runtime
    let targetName = sprintf "DotNetPublish_%s" runtime
    Target.Description (sprintf "publish fake 5 runner for %s" runtime)
    Target.create targetName ignore
    "SetAssemblyInfo"
        ==> rawTargetName
        ?=> "UnskipAndRevertAssemblyInfo"
        ==> targetName
        |> ignore
    rawTargetName
        ==> targetName
        |> ignore
    "_StartDnc"
        ==> targetName
        |> ignore
    "DownloadPaket"
        ==> targetName
        |> ignore
    targetName
        ==> "DotNetPublish"
        |> ignore

    // Make sure we order then (when building parallel!)
    match prev with
    | Some prev -> prev ?=> rawTargetName |> ignore
    | None -> "_DotNetPackage" ?=> rawTargetName |> ignore
    prev <- Some rawTargetName


// Full framework build
"Clean"
    ?=> "RenameFSharpCompilerService"
    ?=> "SetAssemblyInfo"
    ==> "_BuildSolution"
    ?=> "UnskipAndRevertAssemblyInfo"
    ==> "BuildSolution"
"RenameFSharpCompilerService"
    ==> "_BuildSolution"
"_BuildSolution"
    ==> "BuildSolution"
// AfterBuild -> Both Builds completed
"BuildSolution"
    ==> "_AfterBuild"
"DotNetPackage"
    ==> "_AfterBuild"
"DotNetPublish"
    ==> "_AfterBuild"


// Create artifacts when build is finished
let prevDocs =
    "_AfterBuild"
    ==> "CreateNuGet"
    ==> "CopyLicense"
    =?> ("DotNetCoreCreateChocolateyPackage", Environment.isWindows)
(if fromArtifacts then "PrepareArtifacts" else prevDocs)
    =?> ("GenerateDocs", not <| Environment.hasEnvironVar "SkipDocs")
    ==> "Default"
prevDocs ?=> "GenerateDocs"

"GenerateDocs"
    ==> "Release_GenerateDocs"

// Build artifacts only (no testing)
"CreateNuGet"
    ==> "BuildArtifacts"
"DotNetCoreCreateChocolateyPackage"
    =?> ("BuildArtifacts", Environment.isWindows)
"DotNetCoreCreateZipPackages"
    ==> "BuildArtifacts"

// Test the full framework build
"_BuildSolution"
    =?> ("Test", not <| Environment.hasEnvironVar "SkipTests")
    ==> "Default"

"BuildSolution"
    ==> "Default"
    
(if fromArtifacts then "PrepareArtifacts" else "_BuildSolution")
    =?> ("BootstrapTest", not disableBootstrap && not <| Environment.hasEnvironVar "SkipTests")
    ==> "Default"
"_BuildSolution" ?=> "BootstrapTest"

"BootstrapTest"
    ==> "RunTests"

// Test the dotnetcore build
(if fromArtifacts then "PrepareArtifacts" else "_DotNetPackage")
    =?> ("DotNetCoreUnitTests",not <| Environment.hasEnvironVar "SkipTests")
    ==> "FullDotNetCore"
"_DotNetPackage" ?=> "DotNetCoreUnitTests"

"DotNetCoreUnitTests"
    ==> "RunTests"

(if fromArtifacts then "PrepareArtifacts" else "_DotNetPublish_current")
    =?> ("DotNetCoreIntegrationTests", not <| Environment.hasEnvironVar "SkipIntegrationTests" && not <| Environment.hasEnvironVar "SkipTests")
    ==> "FullDotNetCore"
"_DotNetPublish_current" ?=> "DotNetCoreIntegrationTests"

"DotNetCoreIntegrationTests"
    ==> "RunTests"

(if fromArtifacts then "PrepareArtifacts" else "_DotNetPackage")
    =?> ("DotNetCoreIntegrationTests", not <| Environment.hasEnvironVar "SkipIntegrationTests" && not <| Environment.hasEnvironVar "SkipTests")
"_DotNetPackage" ?=> "DotNetCoreIntegrationTests"

(if fromArtifacts then "PrepareArtifacts" else "_DotNetPublish_current")
    =?> ("BootstrapTestDotNetCore", not disableBootstrap && not <| Environment.hasEnvironVar "SkipTests")
    ==> "FullDotNetCore"
"_DotNetPublish_current" ?=> "BootstrapTestDotNetCore"

"BootstrapTestDotNetCore"
    ==> "RunTests"

"DotNetPackage"
    ==> "DotNetCoreCreateZipPackages"
    ==> "FullDotNetCore"
    ==> "Default"

// Artifacts & Tests
"Default" ==> "Release_BuildAndTest"
"Release_GenerateDocs" ?=> "BuildArtifacts"
"BuildArtifacts" ==> "Release_BuildAndTest"
"Release_GenerateDocs" ==> "Release_BuildAndTest"


// Release stuff ('FastRelease' is to release after running 'Default')
(if fromArtifacts then "PrepareArtifacts" else "EnsureTestsRun")
    =?> ("DotNetCorePushChocolateyPackage", Environment.isWindows)
    ==> "FastRelease"
"EnsureTestsRun" ?=> "DotNetCorePushChocolateyPackage"

(if fromArtifacts then "PrepareArtifacts" else "EnsureTestsRun")
    =?> ("ReleaseDocs", not <| Environment.hasEnvironVar "SkipDocs")
    ==> "FastRelease"
"EnsureTestsRun" ?=> "ReleaseDocs"

(if fromArtifacts then "PrepareArtifacts" else "EnsureTestsRun")
    ==> "DotNetCorePushNuGet"
    ==> "FastRelease"
"EnsureTestsRun" ?=> "DotNetCorePushNuGet"

(if fromArtifacts then "PrepareArtifacts" else "EnsureTestsRun")
    ==> "PublishNuget"
    ==> "FastRelease"
"EnsureTestsRun" ?=> "PublishNuget"

// Gitlab staging (myget release)
"PublishNuget"
    ==> "Release_Staging"
"DotNetCorePushNuGet"
    ==> "Release_Staging"

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
