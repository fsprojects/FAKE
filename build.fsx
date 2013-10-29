#I @"tools/FAKE/tools/"
#r @"FakeLib.dll"

#I "tools/FSharp.Formatting/lib/net40"
#I "tools/Microsoft.AspNet.Razor/lib/net40"
#I "tools/RazorEngine/lib/net40"
#r "System.Web.dll"
#r "FSharp.Markdown.dll"
#r "FSharp.CodeFormat.dll"
#r "FSharp.Literate.dll"
#r "FSharp.MetadataFormat.dll"
#r "System.Web.Razor.dll"
#r "RazorEngine.dll"

open Fake
open FSharp.Literate
open Fake.Git
open FSharp.MetadataFormat
 
// properties 
let projectName = "FAKE"
let projectSummary = "FAKE - F# Make - Get rid of the noise in your build scripts."
let projectDescription = "FAKE - F# Make - is a build automation tool for .NET. Tasks and dependencies are specified in a DSL which is integrated in F#."
let authors = ["Steffen Forkmann"; "Mauricio Scheffer"; "Colin Bull"]
let mail = "forkmann@gmx.de"

let packages = 
    ["FAKE.Core",projectDescription
     "FAKE.Gallio",projectDescription + " Extensions for Gallio"
     "FAKE.IIS",projectDescription + " Extensions for IIS"
     "FAKE.SQL",projectDescription + " Extensions for SQL Server"
     "FAKE.Deploy.Lib",projectDescription + " Extensions for FAKE Deploy"
     projectName,projectDescription + " This package bundles all extensions."]

let buildVersion = if isLocalBuild then "0.0.1" else buildVersion
  
let buildDir = "./build"
let testDir = "./test"
let deployDir = "./Publish"
let docsDir = "./docs"
let apidocsDir = "./docs/apidocs/"
let nugetDir = "./nuget" 
let reportDir = "./report" 
let deployZip = deployDir @@ sprintf "%s-%s.zip" projectName buildVersion
let packagesDir = "./packages"

let additionalFiles = [
    "License.txt"
    "README.markdown"
    "help/changelog.md"]

// Targets
Target "Clean" (fun _ -> CleanDirs [buildDir; testDir; deployDir; docsDir; apidocsDir; nugetDir; reportDir])

Target "RestorePackages" RestorePackages

Target "CopyFSharpFiles" (fun _ ->
    ["./tools/FSharp/FSharp.Core.optdata"
     "./tools/FSharp/FSharp.Core.sigdata"]
      |> CopyTo buildDir
)

open Fake.AssemblyInfoFile

Target "SetAssemblyInfo" (fun _ ->
    let common = [
         Attribute.Product "FAKE - F# Make"
         Attribute.Version buildVersion
         Attribute.FileVersion buildVersion]
    
    [Attribute.Title "FAKE - F# Make Command line tool"
     Attribute.Guid "fb2b540f-d97a-4660-972f-5eeff8120fba"] @ common
    |> CreateFSharpAssemblyInfo "./src/app/FAKE/AssemblyInfo.fs"
    
    [Attribute.Title "FAKE - F# Make Deploy tool"
     Attribute.Guid "413E2050-BECC-4FA6-87AA-5A74ACE9B8E1"] @ common
    |> CreateFSharpAssemblyInfo "./src/app/Fake.Deploy/AssemblyInfo.fs"

    [Attribute.Title "FAKE - F# Make Deploy Web App"
     Attribute.Guid "2B684E7B-572B-41C1-86C9-F6A11355570E"] @ common
    |> CreateFSharpAssemblyInfo "./src/deploy.web/Fake.Deploy.Web.App/AssemblyInfo.fs"
    
    [Attribute.Title "FAKE - F# Make Deploy Web"
     Attribute.Guid "27BA7705-3F57-47BE-B607-8A46B27AE876"] @ common
    |> CreateCSharpAssemblyInfo "./src/deploy.web/Fake.Deploy.Web/AssemblyInfo.cs"
    
    [Attribute.Title "FAKE - F# Make Deploy Lib"
     Attribute.Guid "AA284C42-1396-42CB-BCAC-D27F18D14AC7"] @ common
    |> CreateFSharpAssemblyInfo "./src/app/Fake.Deploy.Lib/AssemblyInfo.fs"
    
    [Attribute.Title "FAKE - F# Make Lib"
     Attribute.Guid "d6dd5aec-636d-4354-88d6-d66e094dadb5"] @ common
    |> CreateFSharpAssemblyInfo "./src/app/FakeLib/AssemblyInfo.fs"
    
    [Attribute.Title "FAKE - F# Make SQL Lib"
     Attribute.Guid "A161EAAF-EFDA-4EF2-BD5A-4AD97439F1BE"] @ common
    |> CreateFSharpAssemblyInfo "./src/app/Fake.SQL/AssemblyInfo.fs"
)

Target "BuildSolution" (fun _ ->        
    MSBuildWithDefaults "Build" ["./FAKE.sln"]
    |> Log "AppBuild-Output: "
)

Target "GenerateDocs" (fun _ ->
    let source = "./help"
    let template = "./help/templates/template-project.html"
    let projInfo =
      [ "page-description", "FAKE - F# Make"
        "page-author", (separated ", " authors)
        "github-link", "http://github.com/fsharp/fake"
        "project-name", "FAKE - F# Make" ]

    Literate.ProcessDirectory (source, template, docsDir, replacements = projInfo)

    if isLocalBuild then  // TODO: this needs to be fixed in FSharp.Formatting
        MetadataFormat.Generate ( "./build/FakeLib.dll", apidocsDir, ["./help/templates/reference/"])

    WriteStringToFile false "./docs/.nojekyll" ""

    CopyDir (docsDir @@ "content") "help/content" allFiles
    CopyDir (docsDir @@ "pics") "help/pics" allFiles
)

Target "CopyLicense" (fun _ -> 
    CopyTo buildDir additionalFiles
)

Target "BuildZip" (fun _ ->     
    !+ (buildDir @@ @"**/*.*") 
    -- "*.zip" 
    -- "**/*.pdb"
      |> Scan
      |> Zip buildDir deployZip
)

Target "Test" (fun _ ->
    !! (testDir @@ "Test.*.dll") 
    |> MSpec (fun p -> 
            {p with
                ExcludeTags = ["HTTP"]
                HtmlOutputDir = reportDir})
)

Target "ZipDocumentation" (fun _ -> 
    !! (docsDir @@ @"**/*.*")  
       |> Zip docsDir (deployDir @@ sprintf "Documentation-%s.zip" buildVersion)
)

Target "CreateNuGet" (fun _ -> 
    for package,description in packages do            
        let nugetDocsDir = nugetDir @@ "docs"
        let nugetToolsDir = nugetDir @@ "tools"

        CleanDir nugetDocsDir
        CleanDir nugetToolsDir
        
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
                OutputPath = nugetDir
                Summary = projectSummary
                Dependencies =
                    if package <> "FAKE.Core" && package <> projectName then
                      ["FAKE.Core", RequireExactly (NormalizeVersion buildVersion)]
                    else p.Dependencies
                AccessKey = getBuildParamOrDefault "nugetkey" ""
                Publish = hasBuildParam "nugetkey" }) "fake.nuspec"
)

Target "ReleaseDocs" (fun _ ->
    CleanDir "gh-pages"
    CommandHelper.runSimpleGitCommand "" "clone -b gh-pages --single-branch git@github.com:fsharp/FAKE.git gh-pages" |> printfn "%s"
    
    fullclean "gh-pages"
    CopyRecursive "docs" "gh-pages" true |> printfn "%A"
    CommandHelper.runSimpleGitCommand "gh-pages" "add . --all" |> printfn "%s"
    CommandHelper.runSimpleGitCommand "gh-pages" (sprintf "commit -m \"Update generated documentation %s\"" buildVersion) |> printfn "%s"
    Branches.push "gh-pages"    
)

Target "Default" DoNothing

// Dependencies
"Clean"
    ==> "RestorePackages"
    ==> "CopyFSharpFiles"
    =?> ("SetAssemblyInfo",not isLocalBuild ) 
    ==> "BuildSolution"
    =?> ("Test",not isLinux )
    ==> "CopyLicense"
    ==> "BuildZip"
    =?> ("GenerateDocs",    not isLinux )
    =?> ("ZipDocumentation",not isLinux )
    =?> ("CreateNuGet",     not isLinux )
    ==> "Default"
    ==> "ReleaseDocs"

// start build
RunTargetOrDefault "Default"
