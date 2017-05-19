#I @"packages/build/FAKE/tools/"
#r @"FakeLib.dll"
#r @"packages/Mono.Cecil/lib/net45/Mono.Cecil.dll"
#I "packages/build/SourceLink.Fake/tools/"
#load "packages/build/SourceLink.Fake/tools/SourceLink.fsx"

open Fake
open Fake.Git
open Fake.FSharpFormatting
open System.IO
open SourceLink
open Fake.ReleaseNotesHelper
open Fake.Testing.XUnit2

// properties
let projectName = "FAKE"
let projectSummary = "FAKE - F# Make - Get rid of the noise in your build scripts."
let projectDescription = "FAKE - F# Make - is a build automation tool for .NET. Tasks and dependencies are specified in a DSL which is integrated in F#."
let authors = ["Steffen Forkmann"; "Mauricio Scheffer"; "Colin Bull"]
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
let nugetDir = "./nuget"
let reportDir = "./report"
let packagesDir = "./packages"
let buildMergedDir = buildDir </> "merged"

let additionalFiles = [
    "License.txt"
    "README.markdown"
    "RELEASE_NOTES.md"
    "./packages/FSharp.Core/lib/net40/FSharp.Core.sigdata"
    "./packages/FSharp.Core/lib/net40/FSharp.Core.optdata"]

// Targets
Target "Clean" (fun _ -> CleanDirs [buildDir; testDir; docsDir; apidocsDir; nugetDir; reportDir])

open Fake.AssemblyInfoFile

Target "RenameFSharpCompilerService" (fun _ ->
    for framework in ["net45"] do
      let dir = __SOURCE_DIRECTORY__ </> "packages/FSharp.Compiler.Service/lib" </> framework
      let targetFile = dir </> "FAKE.FSharp.Compiler.Service.dll"
      DeleteFile targetFile

      let reader = new Mono.Cecil.DefaultAssemblyResolver()
      reader.AddSearchDirectory(dir)
      reader.AddSearchDirectory(__SOURCE_DIRECTORY__ </> "packages/FSharp.Core/lib/net40")
      let readerParams = new Mono.Cecil.ReaderParameters(AssemblyResolver = reader)
      let asem = Mono.Cecil.AssemblyDefinition.ReadAssembly(dir </> "FSharp.Compiler.Service.dll", readerParams)
      asem.Name <- new Mono.Cecil.AssemblyNameDefinition("FAKE.FSharp.Compiler.Service", new System.Version(1,0,0,0))
      asem.Write(dir </> "FAKE.FSharp.Compiler.Service.dll")
)

Target "SetAssemblyInfo" (fun _ ->
    let common = [
         Attribute.Product "FAKE - F# Make"
         Attribute.Version release.AssemblyVersion
         Attribute.InformationalVersion release.AssemblyVersion
         Attribute.FileVersion release.AssemblyVersion]

    let assemblyInfos =
      [ "./src/app/FAKE/AssemblyInfo.fs",
        [Attribute.Title "FAKE - F# Make Command line tool"
         Attribute.Guid "fb2b540f-d97a-4660-972f-5eeff8120fba"] @ common
        "./src/app/Fake.Deploy/AssemblyInfo.fs",
        [Attribute.Title "FAKE - F# Make Deploy tool"
         Attribute.Guid "413E2050-BECC-4FA6-87AA-5A74ACE9B8E1"] @ common
        "./src/deploy.web/Fake.Deploy.Web/AssemblyInfo.fs",
        [Attribute.Title "FAKE - F# Make Deploy Web"
         Attribute.Guid "27BA7705-3F57-47BE-B607-8A46B27AE876"] @ common
        "./src/app/Fake.Deploy.Lib/AssemblyInfo.fs",
        [Attribute.Title "FAKE - F# Make Deploy Lib"
         Attribute.Guid "AA284C42-1396-42CB-BCAC-D27F18D14AC7"] @ common
        "./src/app/FakeLib/AssemblyInfo.fs",
        [Attribute.Title "FAKE - F# Make Lib"
         Attribute.InternalsVisibleTo "Test.FAKECore"
         Attribute.Guid "d6dd5aec-636d-4354-88d6-d66e094dadb5"] @ common
        "./src/app/Fake.SQL/AssemblyInfo.fs",
        [Attribute.Title "FAKE - F# Make SQL Lib"
         Attribute.Guid "A161EAAF-EFDA-4EF2-BD5A-4AD97439F1BE"] @ common
        "./src/app/Fake.Experimental/AssemblyInfo.fs",
        [Attribute.Title "FAKE - F# Make Experimental Lib"
         Attribute.Guid "5AA28AED-B9D8-4158-A594-32FE5ABC5713"] @ common
        "./src/app/Fake.FluentMigrator/AssemblyInfo.fs",
        [Attribute.Title "FAKE - F# Make FluentMigrator Lib"
         Attribute.Guid "E18BDD6F-1AF8-42BB-AEB6-31CD1AC7E56D"] @ common ]

    for assemblyFile, attributes in assemblyInfos do
        attributes |> CreateFSharpAssemblyInfo assemblyFile
        // Fixes merge conflicts in AssemblyInfo.fs files, while at the same time leaving the repository in a compilable state.
        // http://stackoverflow.com/questions/32251037/ignore-changes-to-a-tracked-file
        Git.CommandHelper.directRunGitCommandAndFail "." (sprintf "update-index --skip-worktree %s" assemblyFile)
)

Target "BuildSolution" (fun _ ->
    MSBuildWithDefaults "Build" ["./FAKE.sln"; "./FAKE.Deploy.Web.sln"]
    |> Log "AppBuild-Output: "
)

Target "GenerateDocs" (fun _ ->
    let source = "./help"
    let template = "./help/literate/templates/template-project.html"
    let templatesDir = "./help/templates/reference/"
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

    Copy source ["RELEASE_NOTES.md"]

    CreateDocs source docsDir template projInfo

    let dllFiles =
        !! "./build/**/Fake.*.dll"
          ++ "./build/FakeLib.dll"
          -- "./build/**/Fake.Experimental.dll"
          -- "./build/**/FSharp.Compiler.Service.dll"
          -- "./build/**/FAKE.FSharp.Compiler.Service.dll"
          -- "./build/**/Fake.IIS.dll"
          -- "./build/**/Fake.Deploy.Lib.dll"

    CreateDocsForDlls apidocsDir templatesDir (projInfo @ ["--libDirs", "./build"]) (githubLink + "/blob/master") dllFiles

    WriteStringToFile false "./docs/.nojekyll" ""

    CopyDir (docsDir @@ "content") "help/content" allFiles
    CopyDir (docsDir @@ "pics") "help/pics" allFiles
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
                HtmlOutputDir = reportDir})

    !! (testDir @@ "Test.*.dll")
      ++ (testDir @@ "FsCheck.Fake.dll")
    |>  xUnit2 id
)

Target "Bootstrap" (fun _ ->
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
        let executeTarget target =
            if clearCache then clear ()
            ExecProcess (fun info ->
                info.FileName <- "build/FAKE.exe"
                info.WorkingDirectory <- "."
                info.Arguments <- sprintf "%s %s -pd" script target) (System.TimeSpan.FromMinutes 3.0)

        let result = executeTarget "PrintColors"
        if result <> 0 then failwith "Bootstrapping failed"

        let result = executeTarget "FailFast"
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

Target "SourceLink" (fun _ ->
    !! "src/app/**/*.fsproj"
    |> Seq.iter (fun f ->
        let proj = VsProj.LoadRelease f
        let url = sprintf "%s/%s/{0}/%%var2%%" gitRaw projectName
        SourceLink.Index proj.CompilesNotLinked proj.OutputFilePdb __SOURCE_DIRECTORY__ url )
    let pdbFakeLib = "./build/FakeLib.pdb"
    CopyFile "./build/FAKE.Deploy" pdbFakeLib
    CopyFile "./build/FAKE.Deploy.Lib" pdbFakeLib
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
                info.FileName <- currentDirectory </> "packages" </> "build" </> "ILRepack" </> "tools" </> "ILRepack.exe"
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
                  WorkingDirectory = directory file
                  CommandLine = "/32BIT- /32BITPREF- " + quoteIfNeeded file
                  Args = [] }
            printfn "%A" args
            shellExec args |> ignore)

    let x64ify package =
        { package with
            Dependencies = package.Dependencies |> List.map (fun (pkg, ver) -> pkg + ".x64", ver)
            Project = package.Project + ".x64" }

    for package,description in packages do
        let nugetDocsDir = nugetDir @@ "docs"
        let nugetToolsDir = nugetDir @@ "tools"
        let nugetLibDir = nugetDir @@ "lib"
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
                OutputPath = nugetDir
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

Target "PublishNuget" (fun _ ->
    Paket.Push(fun p ->
        { p with
            DegreeOfParallelism = 2
            WorkingDir = nugetDir })
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
Target "Default" DoNothing

// Dependencies
"Clean"
    ==> "RenameFSharpCompilerService"
    ==> "SetAssemblyInfo"
    ==> "BuildSolution"
    //==> "ILRepack"
    ==> "Test"
    ==> "Bootstrap"
    ==> "Default"
    ==> "CopyLicense"
    =?> ("GenerateDocs", isLocalBuild && not isLinux)
    =?> ("SourceLink", isLocalBuild && not isLinux)
    =?> ("CreateNuGet", not isLinux)
    =?> ("ReleaseDocs", isLocalBuild && not isLinux)
    ==> "PublishNuget"
    ==> "Release"

// start build
RunTargetOrDefault "Default"
