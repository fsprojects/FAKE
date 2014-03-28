#I @"tools/FAKE/tools/"
#r @"FakeLib.dll"
#load "tools/SourceLink.Fake/tools/SourceLink.fsx"

open Fake
open Fake.Git
open Fake.FSharpFormatting
open System.IO
open SourceLink
open Fake.ReleaseNotesHelper

// properties
let projectName = "FAKE"
let projectSummary = "FAKE - F# Make - Get rid of the noise in your build scripts."
let projectDescription = "FAKE - F# Make - is a build automation tool for .NET. Tasks and dependencies are specified in a DSL which is integrated in F#."
let authors = ["Steffen Forkmann"; "Mauricio Scheffer"; "Colin Bull"]
let mail = "forkmann@gmx.de"
let gitRaw = environVarOrDefault "gitRaw" "https://raw.github.com/fsharp"

let release = parseReleaseNotes (System.IO.File.ReadAllLines "RELEASE_NOTES.md")

let packages =
    ["FAKE.Core",projectDescription
     "FAKE.Gallio",projectDescription + " Extensions for Gallio"
     "FAKE.IIS",projectDescription + " Extensions for IIS"
     "FAKE.SQL",projectDescription + " Extensions for SQL Server"
     "FAKE.Experimental",projectDescription + " Experimental Extensions"
     "FAKE.Deploy.Lib",projectDescription + " Extensions for FAKE Deploy"
     projectName,projectDescription + " This package bundles all extensions."]

let buildDir = "./build"
let testDir = "./test"
let deployDir = "./Publish"
let docsDir = "./docs"
let apidocsDir = "./docs/apidocs/"
let nugetDir = "./nuget"
let reportDir = "./report"
let deployZip = deployDir @@ sprintf "%s-%s.zip" projectName release.AssemblyVersion
let packagesDir = "./packages"

let additionalFiles = [
    "License.txt"
    "README.markdown"
    "RELEASE_NOTES.md"
    "help/changelog.md"]

// Targets
Target "Clean" (fun _ -> CleanDirs [buildDir; testDir; deployDir; docsDir; apidocsDir; nugetDir; reportDir])

Target "RestorePackages" RestorePackages

open Fake.AssemblyInfoFile

Target "SetAssemblyInfo" (fun _ ->
    let common = [
         Attribute.Product "FAKE - F# Make"
         Attribute.Version release.AssemblyVersion
         Attribute.InformationalVersion release.AssemblyVersion
         Attribute.FileVersion release.AssemblyVersion]

    [Attribute.Title "FAKE - F# Make Command line tool"
     Attribute.Guid "fb2b540f-d97a-4660-972f-5eeff8120fba"] @ common
    |> CreateFSharpAssemblyInfo "./src/app/FAKE/AssemblyInfo.fs"

    [Attribute.Title "FAKE - F# Make Deploy tool"
     Attribute.Guid "413E2050-BECC-4FA6-87AA-5A74ACE9B8E1"] @ common
    |> CreateFSharpAssemblyInfo "./src/app/Fake.Deploy/AssemblyInfo.fs"

    [Attribute.Title "FAKE - F# Make Deploy Web"
     Attribute.Guid "27BA7705-3F57-47BE-B607-8A46B27AE876"] @ common
    |> CreateFSharpAssemblyInfo "./src/deploy.web/Fake.Deploy.Web/AssemblyInfo.fs"

    [Attribute.Title "FAKE - F# Make Deploy Lib"
     Attribute.Guid "AA284C42-1396-42CB-BCAC-D27F18D14AC7"] @ common
    |> CreateFSharpAssemblyInfo "./src/app/Fake.Deploy.Lib/AssemblyInfo.fs"

    [Attribute.Title "FAKE - F# Make Lib"
     Attribute.InternalsVisibleTo "Test.FAKECore"
     Attribute.Guid "d6dd5aec-636d-4354-88d6-d66e094dadb5"] @ common
    |> CreateFSharpAssemblyInfo "./src/app/FakeLib/AssemblyInfo.fs"

    [Attribute.Title "FAKE - F# Make SQL Lib"
     Attribute.Guid "A161EAAF-EFDA-4EF2-BD5A-4AD97439F1BE"] @ common
    |> CreateFSharpAssemblyInfo "./src/app/Fake.SQL/AssemblyInfo.fs"

    [Attribute.Title "FAKE - F# Make Experimental Lib"
     Attribute.Guid "5AA28AED-B9D8-4158-A594-32FE5ABC5713"] @ common
    |> CreateFSharpAssemblyInfo "./src/app/Fake.Experimental/AssemblyInfo.fs"
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

    CreateDocs source docsDir template projInfo

    let dllFiles =
        !! "./build/**/Fake.*.dll"
          ++ "./build/FakeLib.dll"
          -- "./build/**/Fake.Experimental.dll"
          -- "./build/**/Fake.Deploy.Lib.dll"

    CreateDocsForDlls apidocsDir templatesDir projInfo (githubLink + "/blob/develop") dllFiles

    WriteStringToFile false "./docs/.nojekyll" ""

    CopyDir (docsDir @@ "content") "help/content" allFiles
    CopyDir (docsDir @@ "pics") "help/pics" allFiles
)

Target "CopyLicense" (fun _ ->
    CopyTo buildDir additionalFiles
)

Target "Test" (fun _ ->
    let dlls = !! (testDir @@ "Test.*.dll")

    dlls
    |> MSpec (fun p ->
            {p with
                ExcludeTags = ["HTTP"]
                HtmlOutputDir = reportDir})

    dlls
    |>  xUnit (fun p -> p)
)

Target "SourceLink" (fun _ ->
    use repo = new GitRepo(__SOURCE_DIRECTORY__)
    !! "src/app/**/*.fsproj" 
    |> Seq.iter (fun f ->
        let proj = VsProj.LoadRelease f
        logfn "source linking %s" proj.OutputFilePdb
        let files = proj.Compiles -- "**/AssemblyInfo.fs"
        repo.VerifyChecksums files
        proj.VerifyPdbChecksums files
        proj.CreateSrcSrv (sprintf "%s/%s/{0}/%%var2%%" gitRaw projectName) repo.Revision (repo.Paths files)
        Pdbstr.exec proj.OutputFilePdb proj.OutputFilePdbSrcSrv
    )
)

Target "CreateNuGet" (fun _ ->
    for package,description in packages do
        let nugetDocsDir = nugetDir @@ "docs"
        let nugetToolsDir = nugetDir @@ "tools"

        CleanDir nugetDocsDir
        CleanDir nugetToolsDir

        DeleteFile "./build/FAKE.Gallio/Gallio.dll"

        match package with
        | p when p = projectName ->
            !! (buildDir @@ "**/*.*") |> Copy nugetToolsDir
            CopyDir nugetToolsDir @"./lib/fsi" allFiles
            CopyDir nugetDocsDir docsDir allFiles
        | p when p = "FAKE.Core" ->
            !! (buildDir @@ "*.*") |> Copy nugetToolsDir
            CopyDir nugetToolsDir @"./lib/fsi" allFiles
            CopyDir nugetDocsDir docsDir allFiles
        | _ ->
            CopyDir nugetToolsDir (buildDir @@ package) allFiles
            CopyTo nugetToolsDir additionalFiles
        !! (nugetToolsDir @@ "*.pdb") |> DeleteFiles

        NuGet (fun p ->
            {p with
                Authors = authors
                Project = package
                Description = description
                Version = release.NugetVersion
                OutputPath = nugetDir
                Summary = projectSummary
                ReleaseNotes = release.Notes |> toLines
                Dependencies =
                    if package <> "FAKE.Core" && package <> projectName then
                      ["FAKE.Core", RequireExactly (NormalizeVersion release.AssemblyVersion)]
                    else p.Dependencies
                AccessKey = getBuildParamOrDefault "nugetkey" ""
                Publish = hasBuildParam "nugetkey"
                ToolPath = "./tools/NuGet/nuget.exe"  }) "fake.nuspec"
)

Target "ReleaseDocs" (fun _ ->
    CleanDir "gh-pages"
    cloneSingleBranch "" "https://github.com/fsharp/FAKE.git" "gh-pages" "gh-pages"

    fullclean "gh-pages"
    CopyRecursive "docs" "gh-pages" true |> printfn "%A"
    CopyFile "gh-pages" "./Samples/FAKE-Calculator.zip"
    StageAll "gh-pages"
    Commit "gh-pages" (sprintf "Update generated documentation %s" release.AssemblyVersion)
    Branches.push "gh-pages"
)

Target "Default" DoNothing

// Dependencies
"Clean"
    ==> "RestorePackages"
    ==> "SetAssemblyInfo"
    ==> "BuildSolution"
    ==> "Test"
    ==> "CopyLicense"
    =?> ("GenerateDocs", isLocalBuild && not isLinux)
    //=?> ("SourceLink", isLocalBuild)
    =?> ("CreateNuGet", not isLinux)
    ==> "Default"
    ==> "ReleaseDocs"

// start build
RunTargetOrDefault "Default"
