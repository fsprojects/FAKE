#load ".fake/build.fsx/intellisense.fsx"

open System.Reflection
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

let projectName = "FAKE"
let projectSummary = "FAKE - F# Make - Get rid of the noise in your build scripts."
let projectDescription = "FAKE - F# Make - is a build automation tool for .NET. Tasks and dependencies are specified in a DSL which is integrated in F#."
let authors = ["Steffen Forkmann"; "Mauricio Scheffer"; "Colin Bull"; "Matthias Dittrich"]
let github_release_user = Environment.environVarOrDefault "github_release_user" "fsharp"

// The name of the project on GitHub
let gitName = "FAKE"

let release = ReleaseNotes.load "RELEASE_NOTES.md"

let buildDir = "./build"
let testDir = "./test"
let docsDir = "./docs"
let apidocsDir = "./docs/apidocs/"

let releaseDir = "./release"
let nugetDncDir = releaseDir </> "dotnetcore"
let chocoReleaseDir = nugetDncDir </> "chocolatey"
let nugetLegacyDir = releaseDir </> "legacy"

let reportDir = "./report"
let packagesDir = "./packages"
let buildMergedDir = buildDir </> "merged"
let root = __SOURCE_DIRECTORY__
let srcDir = root</>"src"
let appDir = srcDir</>"app"
let templateDir = srcDir</>"template"
let legacyDir = srcDir</>"legacy"

let packages =
    ["FAKE.Core",projectDescription
     "FAKE.Gallio",projectDescription + " Extensions for Gallio"
     "FAKE.IIS",projectDescription + " Extensions for IIS"
     "FAKE.FluentMigrator",projectDescription + " Extensions for FluentMigrator"
     "FAKE.SQL",projectDescription + " Extensions for SQL Server"
     "FAKE.Experimental",projectDescription + " Experimental Extensions"
     "Fake.Deploy.Lib",projectDescription + " Extensions for FAKE Deploy"
     projectName,projectDescription + " This package bundles all extensions."
     "FAKE.Lib",projectDescription + " FAKE helper functions as library"]


let additionalFiles = [
    "License.txt"
    "README.markdown"
    "RELEASE_NOTES.md"
    "./packages/FSharp.Core/lib/net45/FSharp.Core.sigdata"
    "./packages/FSharp.Core/lib/net45/FSharp.Core.optdata"]


let nugetsource = Environment.environVarOrDefault "nugetsource" "https://www.nuget.org/api/v2/package"
let version =
    let segToString = function
        | PreReleaseSegment.AlphaNumeric n -> n
        | PreReleaseSegment.Numeric n -> string n
    let source, buildMeta =
        match BuildServer.buildServer with
        | BuildServer.GitLabCI ->
            // Workaround for now
            // We get CI_COMMIT_REF_NAME=master and CI_COMMIT_SHA
            // Too long for chocolatey (limit = 20) and we don't strictly need it.
            [ yield PreReleaseSegment.AlphaNumeric GitLab.Environment.PipelineId
            ], sprintf "gitlab.%s" GitLab.Environment.CommitSha
        | BuildServer.TeamFoundation ->
            let sourceBranch = TeamFoundation.Environment.BuildSourceBranch
            let isPr = sourceBranch.StartsWith "refs/pull/"
            let firstSegment =
                if isPr then
                    let splits = sourceBranch.Split '/'
                    let prNum = bigint (int splits.[2])
                    [ PreReleaseSegment.AlphaNumeric "pr"; PreReleaseSegment.Numeric prNum ]
                else
                    []
            let buildId = bigint (int TeamFoundation.Environment.BuildId)
            [ yield! firstSegment
              yield PreReleaseSegment.Numeric buildId
            ], sprintf "vsts.%s" TeamFoundation.Environment.BuildSourceVersion
        | _ -> [], ""

    let semVer = SemVer.parse release.NugetVersion
    let prerelease =
        match semVer.PreRelease with
        | None -> None
        | Some p ->
            let toAdd = System.String.Join(".", source |> Seq.map segToString)
            let toAdd = if System.String.IsNullOrEmpty toAdd then toAdd else "." + toAdd
            Some ({p with
                        Values = p.Values @ source
                        Origin = p.Origin + toAdd })
    let fromRepository =
        match prerelease with
        | Some _ -> { semVer with PreRelease = prerelease; Original = None; BuildMetaData = buildMeta }
        | None -> semVer

    match Environment.environVarOrNone "FAKE_VERSION" with
    | Some ver -> SemVer.parse ver
    | None -> fromRepository

let simpleVersion = version.AsString

let nugetVersion =
    if System.String.IsNullOrEmpty version.BuildMetaData
    then version.AsString
    else sprintf "%s+%s" version.AsString version.BuildMetaData

Target.create "Legacy_RenameFSharpCompilerService" (fun _ ->
  for packDir in ["FSharp.Compiler.Service";"netcore"</>"FSharp.Compiler.Service"] do
    // for framework in ["net40"; "net45"] do
    for framework in ["netstandard2.0"; "net45"] do
      let dir = __SOURCE_DIRECTORY__ </> "packages"</>packDir</>"lib"</>framework
      let targetFile = dir </>  "FAKE.FSharp.Compiler.Service.dll"
      File.delete targetFile

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
              member x.Resolve (name : Mono.Cecil.AssemblyNameReference) = readAssembly name.FullName x
              member x.Resolve (name : Mono.Cecil.AssemblyNameReference, parms : Mono.Cecil.ReaderParameters) = readAssemblyE name.FullName parms
               }
      let readerParams = Mono.Cecil.ReaderParameters(AssemblyResolver = reader)
      let asem = Mono.Cecil.AssemblyDefinition.ReadAssembly(dir </>"FSharp.Compiler.Service.dll", readerParams)
      asem.Name <- Mono.Cecil.AssemblyNameDefinition("FAKE.FSharp.Compiler.Service", System.Version(1,0,0,0))
      asem.Write(dir</>"FAKE.FSharp.Compiler.Service.dll")
)

let common = [] // will be added in parent script

let legacyAssemblyInfos =
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
        AssemblyInfo.Guid "E18BDD6F-1AF8-42BB-AEB6-31CD1AC7E56D"] @ common ]

let publish f =
    Trace.publish ImportData.BuildArtifact f

Target.create "_Legacy_BuildSolution" (fun _ ->
    MSBuild.runWithDefaults "Build" ["./src/Legacy-FAKE.sln"; "./src/Legacy-FAKE.Deploy.Web.sln"]
    |> Trace.logItems "AppBuild-Output: "

    // TODO: Check if we run the test in the current build!
    Directory.ensure "temp"
    let testZip = "temp/tests-legacy.zip"
    !! "test/**"
    |> Zip.zip "." testZip
    publish testZip
)

Target.create "Legacy_CopyLicense" (fun _ ->
    Shell.copyTo buildDir additionalFiles
)


Target.create "Legacy_Test" (fun _ ->
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


Target.create "Legacy_ILRepack" (fun _ ->
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


Target.create "Legacy_CreateNuGet" (fun _ ->
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
        let prefs =
           [ "packages/build/Nuget.CommandLine/tools/NuGet.exe"
             "packages/build/NuGet.CommandLine/tools/NuGet.exe" ]
           |> List.map Path.GetFullPath
        match Seq.tryFind (File.Exists) prefs with
        | Some pref -> pref
        | None ->
            let rec printDir space d =
                for f in Directory.EnumerateFiles d do
                    Trace.tracefn "%sFile: %s" space f
                for sd in Directory.EnumerateDirectories d do
                    Trace.tracefn "%sDirectory: %s" space sd
                    printDir (space + "  ") sd
            printDir "  " (Path.GetFullPath "packages")
            match !! "packages/**/NuGet.exe" |> Seq.tryHead with
            | Some e ->
                Trace.tracefn "Found %s" e
                e
            | None ->
                prefs |> List.head

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
                NuGet.NuGet.NuGetParams.Version = nugetVersion
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

    let legacyZip = releaseDir </> "fake-legacy-packages.zip"
    !! (nugetLegacyDir </> "**/*.nupkg")
    |> Zip.zip nugetLegacyDir legacyZip
    publish legacyZip
)

Target.create "Legacy_PublishNuget" (fun _ ->
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


Target.description "Build the full-framework (legacy) solution"
Target.create "Legacy_BuildSolution" ignore

open Fake.Core.TargetOperators
let setTargetDependencies (fromArtifacts:bool) =
    // Full framework build
    "Clean"
        ?=> "Legacy_RenameFSharpCompilerService"
        ?=> "SetAssemblyInfo"
        ==> "_Legacy_BuildSolution"
        ?=> "UnskipAndRevertAssemblyInfo"
        ==> "Legacy_BuildSolution"
        |> ignore
    "Legacy_RenameFSharpCompilerService"
        ==> "_Legacy_BuildSolution"
        |> ignore
    "_Legacy_BuildSolution"
        ==> "Legacy_BuildSolution"
        |> ignore
    // AfterBuild -> Both Builds completed
    "Legacy_BuildSolution"
        ==> "_AfterBuild"
        |> ignore

    // Create artifacts when build is finished
    "_AfterBuild"
        ==> "Legacy_CreateNuGet"
        ==> "Legacy_CopyLicense"
        ==> "Default"
        |> ignore

    "Legacy_CreateNuGet"
        ==> "BuildArtifacts"
        |> ignore

    // Test the full framework build
    "_Legacy_BuildSolution"
        =?> ("Legacy_Test", not <| Environment.hasEnvironVar "SkipTests")
        ==> "Default"
        |> ignore

    "Legacy_BuildSolution"
        ==> "Default"
        |> ignore


    (if fromArtifacts then "PrepareArtifacts" else "EnsureTestsRun")
        ==> "Legacy_PublishNuget"
        ==> "FastRelease"
        |> ignore
    "EnsureTestsRun"
        ?=> "Legacy_PublishNuget"
        |> ignore

    // Gitlab staging (myget release)
    "Legacy_PublishNuget"
        ==> "Release_Staging"
        |> ignore