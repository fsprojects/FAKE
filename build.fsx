#if DOTNETCORE
// We need to use this for now as "regular" Fake breaks when its caching logic cannot find "loadDependencies.fsx".
// This is the reason why we need to checkin the "loadDependencies.fsx" file for now...
#load ".fake/build.fsx/loadDependencies.fsx"

open System
open System.IO
open System.Reflection
open Fake.Core
open Fake.Core.BuildServer
open Fake.Core.Environment
open Fake.Core.Trace
open Fake.Core.Targets
open Fake.Core.TargetOperators
open Fake.Core.String
open Fake.Core.SemVer
open Fake.Core.ReleaseNotes
open Fake.Core.Process
open Fake.Core.Globbing
open Fake.Core.Globbing.Operators
open Fake.IO.FileSystem
open Fake.IO.FileSystem.FileFilter
open Fake.IO.Zip
open Fake.IO.FileSystem.Directory
open Fake.IO.FileSystem.File
open Fake.IO.FileSystem.Operators
open Fake.IO.FileSystem.Shell
open Fake.DotNet
open Fake.DotNet.FSFormatting
open Fake.DotNet.AssemblyInfoFile
open Fake.DotNet.AssemblyInfoFile.AssemblyInfo
open Fake.DotNet.MsBuild
open Fake.DotNet.Cli
open Fake.Testing.Common
open Fake.DotNet.Testing.MSpec
open Fake.DotNet.Testing.XUnit2
open Fake.DotNet.Testing.NUnit3
open Fake.DotNet.NuGet.NuGet
open Fake.Core.Globbing.Tools
open Fake.Windows
open Fake.Tools
open Fake.Tools.Git
open Fake.Tools.Git.Repository
open Fake.Tools.Git.Staging
open Fake.Tools.Git.Commit

let currentDirectory = Shell.pwd()
#else
// Load this before FakeLib, see https://github.com/fsharp/FSharp.Compiler.Service/issues/763
#r @"packages/Mono.Cecil/lib/net40/Mono.Cecil.dll"
//#if DESIGNTIME
#I @"packages/build/FAKE/tools/"
#r @"FakeLib.dll"
#r @"Paket.Core.dll"
//#else
//#r "src/app/FakeLib/bin/Debug/FakeLib.dll"
//#endif
#I "packages/build/SourceLink.Fake/tools/"
#load "packages/build/SourceLink.Fake/tools/SourceLink.fsx"

open Fake
open Fake.Git
open Fake.FSharpFormatting
open System.IO
open SourceLink
open Fake.ReleaseNotesHelper
open Fake.AssemblyInfoFile
open Fake.Testing.XUnit2
open Fake.Testing.NUnit3
#endif


// properties
let projectName = "FAKE"
let projectSummary = "FAKE - F# Make - Get rid of the noise in your build scripts."
let projectDescription = "FAKE - F# Make - is a build automation tool for .NET. Tasks and dependencies are specified in a DSL which is integrated in F#."
let authors = ["Steffen Forkmann"; "Mauricio Scheffer"; "Colin Bull"; "Matthias Dittrich"]
let gitRaw = environVarOrDefault "gitRaw" "https://raw.github.com/fsharp"

let release = LoadReleaseNotes "RELEASE_NOTES.md"

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

let additionalFiles = [
    "License.txt"
    "README.markdown"
    "RELEASE_NOTES.md"
    "./packages/FSharp.Core/lib/net45/FSharp.Core.sigdata"
    "./packages/FSharp.Core/lib/net45/FSharp.Core.optdata"]

let cleanForTests () =
    // Clean NuGet cache (because it might contain appveyor stuff)
    let cacheFolders = [ Paket.Constants.UserNuGetPackagesFolder; Paket.Constants.NuGetCacheFolder ]
    for f in cacheFolders do
        printfn "Clearing FAKE-NuGet packages in %s" f
        !! (f </> "Fake.*")
        |> Seq.iter (Shell.rm_rf)

    // Clean test directories
    !! "integrationtests/*/temp"
    |> CleanDirs

// Targets
Target "Clean" (fun _ ->
    !! "src/*/*/bin"
    ++ "src/*/*/obj"
    |> CleanDirs

    CleanDirs [buildDir; testDir; docsDir; apidocsDir; nugetDncDir; nugetLegacyDir; reportDir]

    // Clean Data for tests
    cleanForTests()
)

Target "RenameFSharpCompilerService" (fun _ ->
  for packDir in ["FSharp.Compiler.Service";"netcore"</>"FSharp.Compiler.Service"] do
    // for framework in ["net40"; "net45"] do
    for framework in ["netstandard1.6"; "net45"] do
      let dir = __SOURCE_DIRECTORY__ </> "packages"</>packDir</>"lib"</>framework
      let targetFile = dir </>  "FAKE.FSharp.Compiler.Service.dll"
      DeleteFile targetFile

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
      let readerParams = new Mono.Cecil.ReaderParameters(AssemblyResolver = reader)
      let asem = Mono.Cecil.AssemblyDefinition.ReadAssembly(dir </>"FSharp.Compiler.Service.dll", readerParams)
      asem.Name <- new Mono.Cecil.AssemblyNameDefinition("FAKE.FSharp.Compiler.Service", new System.Version(1,0,0,0))
      asem.Write(dir</>"FAKE.FSharp.Compiler.Service.dll")
)


let common = [
    Attribute.Product "FAKE - F# Make"
    Attribute.Version release.AssemblyVersion
    Attribute.InformationalVersion release.AssemblyVersion
    Attribute.FileVersion release.AssemblyVersion]

// New FAKE libraries
let dotnetAssemblyInfos =
    [ "Fake.Core.BuildServer", "Buildserver Support"
      "Fake.Core.Context", "Core Context Infrastructure"
      "Fake.Core.Environment", "Environment Detection"
      "Fake.Core.Globbing", "Filesystem Globbing Support and Operators"
      "Fake.Core.Process", "Starting and managing Processes"
      "Fake.Core.ReleaseNotes", "Parsing ReleaseNotes"
      "Fake.Core.SemVer", "Parsing and working with SemVer"
      "Fake.Core.String", "Core String manipulations"
      "Fake.Core.Targets", "Defining and running Targets"
      "Fake.Core.Tasks", "Repeating and managing Tasks"
      "Fake.Core.Tracing", "Core Logging functionality"
      "Fake.Core.Xml", "Core Xml functionality"
      "Fake.DotNet.AssemblyInfoFile", "Writing AssemblyInfo files"
      "Fake.DotNet.Cli", "Running the dotnet cli"
      "Fake.DotNet.MsBuild", "Running msbuild"
      "Fake.DotNet.NuGet", "Running NuGet Client and interacting with NuGet Feeds"
      "Fake.DotNet.Paket", "Running Paket and publishing packages"
      "Fake.DotNet.FSFormatting", "Running fsformatting.exe and generating documentatiom"
      "Fake.DotNet.Testing.MSpec", "Running mspec test runner"
      "Fake.DotNet.Testing.NUnit", "Running nunit test runner"
      "Fake.DotNet.Testing.XUnit2", "Running xunit test runner"
      "Fake.IO.FileSystem", "Core Filesystem utilities"
      "Fake.IO.Zip", "Core Zip functionality"
      "Fake.netcore", "Command line tool"
      "Fake.Runtime", "Core runtime features"
      "Fake.Tools.Git", "Running git commands"
      "Fake.Testing.Common", "Common testing data types"
      "Fake.Tracing.NAntXml", "NAntXml"
      "Fake.Windows.Chocolatey", "Running and packaging with Chocolatey"
      "Fake.Testing.SonarQube", "Analyzing your project with SonarQube" ]
    
let assemblyInfos =
  [ "./src/app/FAKE/AssemblyInfo.fs",
      [ Attribute.Title "FAKE - F# Make Command line tool"
        Attribute.Guid "fb2b540f-d97a-4660-972f-5eeff8120fba"] @ common
    "./src/app/Fake.Deploy/AssemblyInfo.fs",
      [ Attribute.Title "FAKE - F# Make Deploy tool"
        Attribute.Guid "413E2050-BECC-4FA6-87AA-5A74ACE9B8E1"] @ common
    "./src/deploy.web/Fake.Deploy.Web/AssemblyInfo.fs",
      [ Attribute.Title "FAKE - F# Make Deploy Web"
        Attribute.Guid "27BA7705-3F57-47BE-B607-8A46B27AE876"] @ common
    "./src/app/Fake.Deploy.Lib/AssemblyInfo.fs",
      [ Attribute.Title "FAKE - F# Make Deploy Lib"
        Attribute.Guid "AA284C42-1396-42CB-BCAC-D27F18D14AC7"] @ common
    "./src/app/FakeLib/AssemblyInfo.fs",
      [ Attribute.Title "FAKE - F# Make Lib"
        Attribute.InternalsVisibleTo "Test.FAKECore"
        Attribute.Guid "d6dd5aec-636d-4354-88d6-d66e094dadb5"] @ common
    "./src/app/Fake.SQL/AssemblyInfo.fs",
      [ Attribute.Title "FAKE - F# Make SQL Lib"
        Attribute.Guid "A161EAAF-EFDA-4EF2-BD5A-4AD97439F1BE"] @ common
    "./src/app/Fake.Experimental/AssemblyInfo.fs",
      [ Attribute.Title "FAKE - F# Make Experimental Lib"
        Attribute.Guid "5AA28AED-B9D8-4158-A594-32FE5ABC5713"] @ common
    "./src/app/Fake.FluentMigrator/AssemblyInfo.fs",
      [ Attribute.Title "FAKE - F# Make FluentMigrator Lib"
        Attribute.Guid "E18BDD6F-1AF8-42BB-AEB6-31CD1AC7E56D"] @ common ] @
   (dotnetAssemblyInfos
    |> List.map (fun (project, description) ->
        sprintf "./src/app/%s/AssemblyInfo.fs" project, [Attribute.Title (sprintf "FAKE - F# Make %s" description) ] @ common))

Target "SetAssemblyInfo" (fun _ ->
    for assemblyFile, attributes in assemblyInfos do
        // Fixes merge conflicts in AssemblyInfo.fs files, while at the same time leaving the repository in a compilable state.
        // http://stackoverflow.com/questions/32251037/ignore-changes-to-a-tracked-file
        // not jet released
        Git.CommandHelper.directRunGitCommandAndFail "." (sprintf "update-index --skip-worktree %s" assemblyFile)
        attributes |> CreateFSharpAssemblyInfo assemblyFile
)

Target "DownloadPaket" (fun _ ->
    if 0 <> ExecProcess (fun info -> 
                info.FileName <- ".paket/paket.exe"
                info.Arguments <- "--version") (System.TimeSpan.FromMinutes 5.0) then
        failwith "paket failed to start"
)

Target "UnskipAssemblyInfo" (fun _ ->
    for assemblyFile, _ in assemblyInfos do
        Git.CommandHelper.directRunGitCommandAndFail "." (sprintf "update-index --no-skip-worktree %s" assemblyFile)
)

Target "BuildSolution" (fun _ ->
    MSBuildWithDefaults "Build" ["./FAKE.sln"; "./FAKE.Deploy.Web.sln"]
    |> Log "AppBuild-Output: "
)

// START _________________ MOVE TO Fake.DotNet.FSFormatting
// START _________________ MOVE TO Fake.DotNet.FSFormatting
// START _________________ MOVE TO Fake.DotNet.FSFormatting
// START _________________ MOVE TO Fake.DotNet.FSFormatting
// START _________________ MOVE TO Fake.DotNet.FSFormatting
// START _________________ MOVE TO Fake.DotNet.FSFormatting

/// Specifies the fsformatting executable
let mutable toolPath =
    findToolInSubPath "fsformatting.exe" (Directory.GetCurrentDirectory() @@ "tools" @@ "FSharp.Formatting.CommandTool" @@ "tools")

/// Runs fsformatting.exe with the given command in the given repository directory.
let private run toolPath command = 
    if 0 <> ExecProcess (fun info -> 
                info.FileName <- toolPath
                info.Arguments <- command) System.TimeSpan.MaxValue
    then failwithf "FSharp.Formatting %s failed." command

type LiterateArguments =
    { ToolPath : string
      Source : string
      OutputDirectory : string 
      Template : string
      ProjectParameters : (string * string) list
      LayoutRoots : string list }

let defaultLiterateArguments =
    { ToolPath = toolPath
      Source = ""
      OutputDirectory = ""
      Template = ""
      ProjectParameters = []
      LayoutRoots = [] }

let CreateDocs p =
    let arguments = (p:LiterateArguments->LiterateArguments) defaultLiterateArguments
    let layoutroots =
        if arguments.LayoutRoots.IsEmpty then []
        else [ "--layoutRoots" ] @ arguments.LayoutRoots
    let source = arguments.Source
    let template = arguments.Template
    let outputDir = arguments.OutputDirectory

    let command = 
        arguments.ProjectParameters
        |> Seq.map (fun (k, v) -> [ k; v ])
        |> Seq.concat
        |> Seq.append 
               (["literate"; "--processdirectory" ] @ layoutroots @ [ "--inputdirectory"; source; "--templatefile"; template; 
                  "--outputDirectory"; outputDir; "--replacements" ])
        |> Seq.map (fun s -> 
               if s.StartsWith "\"" then s
               else sprintf "\"%s\"" s)
        |> separated " "
    run arguments.ToolPath command
    printfn "Successfully generated docs for %s" source

type MetadataFormatArguments =
    { ToolPath : string
      Source : string
      SourceRepository : string
      OutputDirectory : string 
      Template : string
      ProjectParameters : (string * string) list
      LayoutRoots : string list
      LibDirs : string list }

let defaultMetadataFormatArguments =
    { ToolPath = toolPath
      Source = Directory.GetCurrentDirectory()
      SourceRepository = ""
      OutputDirectory = ""
      Template = ""
      ProjectParameters = []
      LayoutRoots = []
      LibDirs = [] }

let CreateDocsForDlls (p:MetadataFormatArguments->MetadataFormatArguments) dllFiles = 
    let arguments = p defaultMetadataFormatArguments
    let outputDir = arguments.OutputDirectory
    let projectParameters = arguments.ProjectParameters
    let sourceRepo = arguments.SourceRepository
    let libdirs = 
        if arguments.LibDirs.IsEmpty then []
        else [ "--libDirs" ] @ arguments.LibDirs

    let layoutroots =
        if arguments.LayoutRoots.IsEmpty then []
        else [ "--layoutRoots" ] @ arguments.LayoutRoots

    for file in dllFiles do
        projectParameters
        |> Seq.map (fun (k, v) -> [ k; v ])
        |> Seq.concat
        |> Seq.append 
                ([ "metadataformat"; "--generate"; "--outdir"; outputDir] @ layoutroots @ libdirs @ [ "--sourceRepo"; sourceRepo;
                   "--sourceFolder"; arguments.Source; "--parameters" ])
        |> Seq.map (fun s -> 
                if s.StartsWith "\"" then s
                else sprintf "\"%s\"" s)
        |> separated " "
        |> fun prefix -> sprintf "%s --dllfiles \"%s\"" prefix file
        |> run arguments.ToolPath

        printfn "Successfully generated docs for DLL %s" file

// END _________________ MOVE TO Fake.DotNet.FSFormatting
// END _________________ MOVE TO Fake.DotNet.FSFormatting
// END _________________ MOVE TO Fake.DotNet.FSFormatting
// END _________________ MOVE TO Fake.DotNet.FSFormatting
// END _________________ MOVE TO Fake.DotNet.FSFormatting
// END _________________ MOVE TO Fake.DotNet.FSFormatting


Target "GenerateDocs" (fun _ ->
    CleanDir docsDir
    let source = "./help"
    let docsTemplate = "docpage.cshtml"
    let indexTemplate = "indexpage.cshtml"
    let githubLink = "https://github.com/fsharp/FAKE"
    let projInfo =
      [ "page-description", "FAKE - F# Make"
        "page-author", separated ", " authors
        "project-author", separated ", " authors
        "github-link", githubLink
        "project-github", "http://github.com/fsharp/fake"
        "project-nuget", "https://www.nuget.org/packages/FAKE"
        "root", "http://fsharp.github.io/FAKE"
        "project-name", "FAKE - F# Make" ]
    let layoutroots = [ "./help/templates"; "./help/templates/reference" ]

    CopyDir (docsDir) "help/content" allFiles
    WriteStringToFile false "./docs/.nojekyll" ""
    WriteStringToFile false "./docs/CNAME" "fake.build"
    //CopyDir (docsDir @@ "pics") "help/pics" allFiles

    Copy (source @@ "markdown") ["RELEASE_NOTES.md"]
    CreateDocs (fun s ->
        { s with
            Source = source @@ "markdown"
            OutputDirectory = docsDir
            Template = docsTemplate
            ProjectParameters = ("CurrentPage", "Modules") :: projInfo
            LayoutRoots = layoutroots })
    CreateDocs (fun s ->
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

    ensureDirectory apidocsDir
    dllFiles
    |> CreateDocsForDlls (fun s -> 
        { s with 
            OutputDirectory = apidocsDir
            LayoutRoots = layoutroots 
            LibDirs = [ "./build" ]
            // TODO: CurrentPage shouldn't be required as it's written in the template, but it is -> investigate
            ProjectParameters = ("CurrentPage", "APIReference") :: projInfo
            SourceRepository = githubLink + "/blob/master" }) 

)

Target "CopyLicense" (fun _ ->
    CopyTo buildDir additionalFiles
)

Target "Test" (fun _ ->
    !! (testDir @@ "Test.*.dll")
    |> Seq.filter (fun fileName -> if isMono then fileName.ToLower().Contains "deploy" |> not else true)
    |> MSpec (fun p ->
            {p with
                ToolPath = findToolInSubPath "mspec-x86-clr4.exe" (currentDirectory @@ "tools" @@ "MSpec")
                ExcludeTags = ["HTTP"]
                TimeOut = System.TimeSpan.FromMinutes 5.
                HtmlOutputDir = reportDir})
    try
        !! (testDir @@ "Test.*.dll")
          ++ (testDir @@ "FsCheck.Fake.dll")
        |>  xUnit2 id
    with e when e.Message.Contains "timed out" && isUnix ->
        traceFAKE "Ignoring xUnit timeout for now, there seems to be something funny going on ..."
)

Target "TestDotnetCore" (fun _ ->
    cleanForTests()

    !! (testDir @@ "*.IntegrationTests.dll")
    |> NUnit3 id
)

Target "BootstrapTest" (fun _ ->
    let buildScript = "build.fsx"
    let testScript = "testbuild.fsx"
    // Check if we can build ourself with the new binaries.
    let test clearCache script =
        let clear () =
            // Will make sure the test call actually compiles the script.
            // Note: We cannot just clean .fake here as it might be locked by the currently executing code :)
            if Directory.Exists ".fake" then
                Directory.EnumerateFiles(".fake")
                  |> Seq.filter (fun s -> (Path.GetFileName s).StartsWith script)
                  |> Seq.iter File.Delete
        let executeTarget span target =
            if clearCache then clear ()
            if isUnix then
                let result =
                    ExecProcess (fun info ->
                        info.FileName <- "chmod"
                        info.WorkingDirectory <- "."
                        info.Arguments <- "+x build/FAKE.exe") span
                if result <> 0 then failwith "'chmod +x build/FAKE.exe' failed on unix"
            ExecProcess (fun info ->
                info.FileName <- "build/FAKE.exe"
                info.WorkingDirectory <- "."
                info.Arguments <- sprintf "%s %s -pd" script target) span

        let result = executeTarget (System.TimeSpan.FromMinutes 10.0) "PrintColors"
        if result <> 0 then failwith "Bootstrapping failed"

        let result = executeTarget (System.TimeSpan.FromMinutes 1.0) "FailFast"
        if result = 0 then failwith "Bootstrapping failed"

    // Replace the include line to use the newly build FakeLib, otherwise things will be weird.
    File.ReadAllText buildScript
    |> fun s -> s.Replace("#I @\"packages/build/FAKE/tools/\"", "#I @\"build/\"")
    |> fun text -> File.WriteAllText(testScript, text)

    try
      // Will compile the script.
      test true testScript
      // Will use the compiled/cached version.
      test false testScript
    finally File.Delete(testScript)
)


Target "BootstrapTestDotnetCore" (fun _ ->
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
            if isUnix then
                ExecProcess (fun info ->
                    info.FileName <- "nuget/dotnetcore/Fake.netcore/current/Fake"
                    info.WorkingDirectory <- "."
                    info.Arguments <- sprintf "-v run %s --target %s" script target) timeout
            else
                ExecProcess (fun info ->
                    info.FileName <- "nuget/dotnetcore/Fake.netcore/current/fake.exe"
                    info.WorkingDirectory <- "."
                    info.Arguments <- sprintf "run %s --target %s" script target) timeout

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

Target "SourceLink" (fun _ ->
#if !DOTNETCORE
    !! "src/app/**/*.fsproj"
    |> Seq.iter (fun f ->
        let proj = VsProj.LoadRelease f
        let url = sprintf "%s/%s/{0}/%%var2%%" gitRaw projectName
        SourceLink.Index proj.CompilesNotLinked proj.OutputFilePdb __SOURCE_DIRECTORY__ url )
    let pdbFakeLib = "./build/FakeLib.pdb"
    CopyFile "./build/FAKE.Deploy" pdbFakeLib
    CopyFile "./build/FAKE.Deploy.Lib" pdbFakeLib
#else
    printfn "We don't currently have VsProj.LoadRelease on dotnetcore."
#endif
)

Target "ILRepack" (fun _ ->
    CreateDir buildMergedDir

    let internalizeIn filename =
        let toPack =
            [filename; "FSharp.Compiler.Service.dll"]
            |> List.map (fun l -> buildDir </> l)
            |> separated " "
        let targetFile = buildMergedDir </> filename

        let result =
            ExecProcess (fun info ->
                info.FileName <- Directory.GetCurrentDirectory() </> "packages" </> "build" </> "ILRepack" </> "tools" </> "ILRepack.exe"
                info.Arguments <- sprintf "/verbose /lib:%s /ver:%s /out:%s %s" buildDir release.AssemblyVersion targetFile toPack) (System.TimeSpan.FromMinutes 5.)

        if result <> 0 then failwithf "Error during ILRepack execution."

        CopyFile (buildDir </> filename) targetFile

    internalizeIn "FAKE.exe"

    !! (buildDir </> "FSharp.Compiler.Service.**")
    |> Seq.iter DeleteFile

    DeleteDir buildMergedDir
)

Target "CreateNuGet" (fun _ ->
    let set64BitCorFlags files =
        files
        |> Seq.iter (fun file ->
            let args =
                { Program = "lib" @@ "corflags.exe"
                  WorkingDirectory = Path.GetDirectoryName file
                  CommandLine = "/32BIT- /32BITPREF- " + quoteIfNeeded file
                  Args = [] }
            printfn "%A" args
            shellExec args |> ignore)

    let x64ify package =
        { package with
            Dependencies = package.Dependencies |> List.map (fun (pkg, ver) -> pkg + ".x64", ver)
            Project = package.Project + ".x64" }

    for package,description in packages do
        let nugetDocsDir = nugetLegacyDir @@ "docs"
        let nugetToolsDir = nugetLegacyDir @@ "tools"
        let nugetLibDir = nugetLegacyDir @@ "lib"
        let nugetLib451Dir = nugetLibDir @@ "net451"

        CleanDir nugetDocsDir
        CleanDir nugetToolsDir
        CleanDir nugetLibDir
        DeleteDir nugetLibDir

        DeleteFile "./build/FAKE.Gallio/Gallio.dll"

        let deleteFCS dir =
          //!! (dir </> "FSharp.Compiler.Service.**")
          //|> Seq.iter DeleteFile
          ()

        match package with
        | p when p = projectName ->
            !! (buildDir @@ "**/*.*") |> Copy nugetToolsDir
            CopyDir nugetDocsDir docsDir allFiles
            deleteFCS nugetToolsDir
        | p when p = "FAKE.Core" ->
            !! (buildDir @@ "*.*") |> Copy nugetToolsDir
            CopyDir nugetDocsDir docsDir allFiles
            deleteFCS nugetToolsDir
        | p when p = "FAKE.Lib" ->
            CleanDir nugetLib451Dir
            !! (buildDir @@ "FakeLib.dll") |> Copy nugetLib451Dir
            deleteFCS nugetLib451Dir
        | _ ->
            CopyDir nugetToolsDir (buildDir @@ package) allFiles
            CopyTo nugetToolsDir additionalFiles
        !! (nugetToolsDir @@ "*.srcsv") |> DeleteFiles

        let setParams p =
            {p with
                Authors = authors
                Project = package
                Description = description
                Version = release.NugetVersion
                OutputPath = nugetLegacyDir
                WorkingDir = nugetLegacyDir
                Summary = projectSummary
                ReleaseNotes = release.Notes |> toLines
                Dependencies =
                    (if package <> "FAKE.Core" && package <> projectName && package <> "FAKE.Lib" then
                       ["FAKE.Core", RequireExactly (NormalizeVersion release.AssemblyVersion)]
                     else p.Dependencies )
                Publish = false }

        NuGet setParams "fake.nuspec"
        !! (nugetToolsDir @@ "FAKE.exe") |> set64BitCorFlags
        NuGet (setParams >> x64ify) "fake.nuspec"
)

#if !DOTNETCORE
#load "src/app/Fake.DotNet.Cli/Dotnet.fs"
open Fake.DotNet.Cli
#endif
let LatestTooling options =
    { options with
        InstallerOptions = (fun io ->
            { io with
                Branch = "release/2.0.0"
            })
        Channel = None
        Version = Version "1.0.4"
    }
Target "InstallDotnetCore" (fun _ ->
     DotnetCliInstall LatestTooling
)

let root = __SOURCE_DIRECTORY__
let srcDir = root</>"src"
let appDir = srcDir</>"app"


let netCoreProjs =
    !! "src/app/Fake.Core.*/*.fsproj"
    ++ "src/app/Fake.DotNet.*/*.fsproj"
    ++ "src/app/Fake.Windows.*/*.fsproj"
    ++ "src/app/Fake.IO.*/*.fsproj"
    ++ "src/app/Fake.Tools.*/*.fsproj"
    ++ "src/app/Fake.netcore/*.fsproj"
    ++ "src/app/Fake.Testing.*/*.fsproj"
    ++ "src/app/Fake.Runtime/*.fsproj"

Target "DotnetRestore" (fun _ ->

    setEnvironVar "Version" release.NugetVersion

    //dotnet root "--info"
    Dotnet { DotnetOptions.Default with WorkingDirectory = root } "--info"

    // Workaround bug where paket integration doesn't generate
    // .nuget\packages\.tools\dotnet-compile-fsc\1.0.0-preview2-020000\netcoreapp1.0\dotnet-compile-fsc.deps.json
    let t = Path.GetFullPath "workaround"
    ensureDirectory t
    Dotnet { DotnetOptions.Default with WorkingDirectory = t } "new console --language f#"
    Dotnet { DotnetOptions.Default with WorkingDirectory = t } "restore"
    Dotnet { DotnetOptions.Default with WorkingDirectory = t } "build"
    Directory.Delete(t, true)

    // Copy nupkgs to nuget/dotnetcore
    !! "lib/nupgks/**/*.nupkg"
    |> Seq.iter (fun file ->
        let dir = nugetDncDir //@@ "dotnetcore"
        ensureDirectory dir
        File.Copy(file, dir @@ Path.GetFileName file, true))

    let netcoreFsprojs = netCoreProjs |> Seq.toList
    let result = Dotnet { DotnetOptions.Default with WorkingDirectory = root } "sln src/Fake-netcore.sln list"
    let srcAbsolutePathLength = (Path.GetFullPath "./src").Length + 1
    let missingNetCoreProj =
        netCoreProjs
        |> Seq.toList
        |> List.map (fun proj ->
            let relativePath = proj.Substring srcAbsolutePathLength
            if result.Messages |> Seq.contains relativePath |> not then
                traceFAKE "Project '%s' is missing in src/Fake-netcore.sln! Run 'dotnet sln src/Fake-netcore.sln add src/%s'" proj (relativePath.Replace("\\", "/"))
                true
            else false)
        |> Seq.exists id
    if missingNetCoreProj then failwith "At least one netcore project seems to be missing from the src/Fake-netcore.sln solution!"
    // dotnet restore
    DotnetRestore id "src/Fake-netcore.sln"
)

let runtimes =
  [ "win7-x86"; "win7-x64"; "osx.10.11-x64"; "ubuntu.14.04-x64"; "ubuntu.16.04-x64" ]

Target "DotnetPackage" (fun _ ->
    let nugetDir = System.IO.Path.GetFullPath nugetDncDir

    setEnvironVar "Version" release.NugetVersion
    setEnvironVar "Authors" (separated ";" authors)
    setEnvironVar "Description" projectDescription
    setEnvironVar "PackageReleaseNotes" (release.Notes |> toLines)
    setEnvironVar "PackageTags" "build;fake;f#"
    setEnvironVar "PackageIconUrl" "https://raw.githubusercontent.com/fsharp/FAKE/fee4f05a2ee3c646979bf753f3b1f02d927bfde9/help/content/pics/logo.png"
    setEnvironVar "PackageProjectUrl" "https://github.com/fsharp/Fake"
    setEnvironVar "PackageLicenseUrl" "https://github.com/fsharp/FAKE/blob/d86e9b5b8e7ebbb5a3d81c08d2e59518cf9d6da9/License.txt"

    // dotnet pack
    DotnetPack (fun c ->
        { c with
            Configuration = Release
            OutputPath = Some nugetDir
        }) "src/Fake-netcore.sln"

    let info = DotnetInfo id

    // see https://github.com/fsharp/FSharp.Compiler.Service/issues/755
    let win32manifest = "packages/netcore/FSharp.Compiler.Tools/build/netcoreapp1.0/default.win32manifest"

    let mutable runtimeWorked = false
    // dotnet publish
    runtimes
    |> List.map Some
    |> (fun rs -> None :: rs)
    |> Seq.iter (fun runtime ->
        !! "src/app/Fake.netcore/Fake.netcore.fsproj"
        |> Seq.iter(fun proj ->
            let projName = Path.GetFileName(Path.GetDirectoryName proj)
            let runtimeName, runtime =
                match runtime with
                | Some r -> r, r
                | None -> "current", info.RID

            DotnetRestore (fun c -> {c with Runtime = Some runtime}) proj
            let outDir = nugetDir @@ projName @@ runtimeName
            DotnetPublish (fun c ->
                { c with
                    Runtime = Some runtime
                    Configuration = Release
                    OutputPath = Some outDir
                }) proj
            let source = outDir </> "dotnet"
            if File.Exists source then
                traceFAKE "Workaround https://github.com/dotnet/cli/issues/6465"
                let target = outDir </> "fake"
                if File.Exists target then File.Delete target
                File.Move(source, target)
        )
    )

    // Publish portable as well (see https://docs.microsoft.com/en-us/dotnet/articles/core/app-types)
    let netcoreFsproj = "src/app/Fake.netcore/Fake.netcore.fsproj"
    let oldContent = File.ReadAllText netcoreFsproj
    try
        // File.WriteAllText(netcoreJson, newContent)
        let outDir = nugetDir @@ "Fake.netcore" @@ "portable"
        DotnetPublish (fun c ->
            { c with
                Framework = Some "netcoreapp1.0"
                OutputPath = Some outDir
            }) netcoreFsproj

        //File.Copy(win32manifest, outDir + "/default.win32manifest")
    with e ->
        printfn "failed to publish portable!"
        // File.WriteAllText(netcoreJson, oldContent)
        ()
)

Target "DotnetCoreCreateZipPackages" (fun _ ->
    setEnvironVar "Version" release.NugetVersion

    // build zip packages
    !! "nuget/dotnetcore/*.nupkg"
    -- "nuget/dotnetcore/*.symbols.nupkg"
    |> Zip "nuget/dotnetcore" "nuget/dotnetcore/Fake.netcore/fake-dotnetcore-packages.zip"

    ("portable" :: runtimes)
    |> Seq.iter (fun runtime ->
        let runtimeDir = sprintf "nuget/dotnetcore/Fake.netcore/%s" runtime
        !! (sprintf "%s/**" runtimeDir)
        |> Zip runtimeDir (sprintf "nuget/dotnetcore/Fake.netcore/fake-dotnetcore-%s.zip" runtime)
    )
)

Target "DotnetCoreCreateChocolateyPackage" (fun _ ->
    // !! ""
    ensureDirectory "nuget/dotnetcore/chocolatey"
    Choco.PackFromTemplate (fun p ->
        { p with
            PackageId = "fake"
            ReleaseNotes = release.Notes |> toLines
            InstallerType = Choco.ChocolateyInstallerType.SelfContained
            Version = release.NugetVersion
            Files = [ (System.IO.Path.GetFullPath @"nuget\dotnetcore\Fake.netcore\win7-x86") + @"\**", Some "bin", None ]
            OutputDir = "nuget/dotnetcore/chocolatey" }) "src/Fake-choco-template.nuspec"
    ()
)
Target "DotnetCorePushChocolateyPackage" (fun _ ->
    let path = sprintf "nuget/dotnetcore/chocolatey/%s.%s.nupkg" "fake" release.NugetVersion
    path |> Choco.Push (fun p ->
        { p with
            Source = "https://push.chocolatey.org/"
            ApiKey = environVarOrFail "CHOCOLATEY_API_KEY" })
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

Target "DotnetCoreCreateDebianPackage" (fun _ ->
    let createDebianPackage (manifest : DebPackageManifest) =
        let argsList = ResizeArray<string>()
        argsList.Add <| match manifest.SourceType with
                        | Dir (source,target) -> "-s dir"
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
    ()

)

Target "DotnetCorePushNuGet" (fun _ ->
    let nuget_exe = Directory.GetCurrentDirectory() </> "packages" </> "build" </> "NuGet.CommandLine" </> "tools" </> "NuGet.exe"
    let apikey = environVarOrDefault "nugetkey" ""
    let nugetsource = environVarOrDefault "nugetsource" "https://www.nuget.org/api/v2/package"
    let nugetPush nugetpackage =
        if not <| System.String.IsNullOrEmpty apikey then
            ExecProcess (fun info ->
                info.FileName <- nuget_exe
                info.Arguments <- sprintf "push %s %s -Source %s" (toParam nugetpackage) (toParam apikey) (toParam nugetsource)) (System.TimeSpan.FromMinutes 5.)
            |> (fun r -> if r <> 0 then failwithf "failed to push package %s" nugetpackage)

    // dotnet pack
    netCoreProjs
    -- "src/app/Fake.netcore/*.fsproj"
    |> Seq.iter(fun proj ->
        let projName = Path.GetFileName(Path.GetDirectoryName proj)
        !! (sprintf "nuget/dotnetcore/%s.*.nupkg" projName)
        -- (sprintf "nuget/dotnetcore/%s.*.symbols.nupkg" projName)
        |> Seq.iter nugetPush)
)

Target "PublishNuget" (fun _ ->
    // uses NugetKey environment variable.
    Paket.Push(fun p ->
        { p with
            DegreeOfParallelism = 2
            WorkingDir = nugetLegacyDir })
    // We have some nugets in there we don't want to push
    //Paket.Push(fun p ->
    //    { p with
    //        DegreeOfParallelism = 2
    //        WorkingDir = nugetDncDir })
)

Target "ReleaseDocs" (fun _ ->
    CleanDir "gh-pages"
    cloneSingleBranch "" "https://github.com/fsharp/FAKE.git" "gh-pages" "gh-pages"

    fullclean "gh-pages"
    CopyRecursive "docs" "gh-pages" true |> printfn "%A"
    CopyFile "gh-pages" "./Samples/FAKE-Calculator.zip"
    StageAll "gh-pages"
    Commit "gh-pages" (sprintf "Update generated documentation %s" release.NugetVersion)
    Branches.push "gh-pages"
)

Target "Release" (fun _ ->
    StageAll ""
    Commit "" (sprintf "Bump version to %s" release.NugetVersion)
    Branches.push ""

    Branches.tag "" release.NugetVersion
    Branches.pushTag "" "origin" release.NugetVersion
)
open System
Target "PrintColors" (fun s ->
  let color (color: ConsoleColor) (code : unit -> _) =
      let before = Console.ForegroundColor
      try
        Console.ForegroundColor <- color
        code ()
      finally
        Console.ForegroundColor <- before
  color ConsoleColor.Magenta (fun _ -> printfn "TestMagenta")
)
Target "FailFast" (fun _ -> failwith "fail fast")
Target "EnsureTestsRun" (fun _ ->
#if !DOTNETCORE
  if hasBuildParam "SkipIntegrationTests" || hasBuildParam "SkipTests" then
      let res = getUserInput "Are you really sure to continue without running tests (yes/no)?"
      if res <> "yes" then
          failwith "cannot continue without tests"
#endif
  ()
)
Target "Default" DoNothing
Target "StartDnc" DoNothing

"Clean"
    ==> "StartDnc"
    ==> "InstallDotnetCore"
    ==> "DownloadPaket"
    ==> "DotnetRestore"
    ==> "DotnetPackage"

// Dependencies
"Clean"
    ==> "RenameFSharpCompilerService"
    ==> "SetAssemblyInfo"
    ==> "BuildSolution"
    ==> "DotnetPackage"
    ==> "DotnetCoreCreateZipPackages"
    =?> ("TestDotnetCore", not <| hasBuildParam "SkipIntegrationTests" && not <| hasBuildParam "SkipTests")
    ////==> "ILRepack"
    =?> ("Test", not <| hasBuildParam "SkipTests")
    =?> ("BootstrapTest",not <| hasBuildParam "SkipTests")
    =?> ("BootstrapTestDotnetCore",not <| hasBuildParam "SkipTests")
    =?> ("SourceLink", isLocalBuild && not isLinux)
    =?> ("CreateNuGet", not isLinux)
    ==> "CopyLicense"
    =?> ("DotnetCoreCreateChocolateyPackage", not isLinux)
    ==> "Default"
    =?> ("GenerateDocs", isLocalBuild && not isLinux)
    ==> "EnsureTestsRun"
    =?> ("DotnetCorePushChocolateyPackage", not isLinux)
    =?> ("ReleaseDocs", isLocalBuild && not isLinux)
    ==> "DotnetCorePushNuGet"
    ==> "PublishNuget"
    ==> "Release"

// start build
RunTargetOrDefault "Default"
