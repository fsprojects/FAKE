#r @"tools/FAKE/tools/FakeLib.dll"

open Fake
 
// properties 
let projectName = "FAKE"
let projectSummary = "FAKE - F# Make - Get rid of the noise in your build scripts."
let projectDescription = "FAKE - F# Make - is a build automation tool for .NET. Tasks and dependencies are specified in a DSL which is integrated in F#."
let authors = ["Steffen Forkmann"; "Mauricio Scheffer"; "Colin Bull"]
let mail = "forkmann@gmx.de"
let homepage = "http://github.com/forki/fake"
  
let buildDir = "./build"
let testDir = "./test"
let metricsDir = "./BuildMetrics"
let deployDir = "./Publish"
let docsDir = "./docs" 
let nugetDir = "./nuget" 
let reportDir = "./report" 
let deployZip = deployDir @@ sprintf "%s-%s.zip" projectName buildVersion
let packagesDir = "./packages"


// Targets
Target "Clean" (fun _ -> CleanDirs [buildDir; testDir; deployDir; docsDir; metricsDir; nugetDir; reportDir])

Target "RestorePackages" RestorePackages

Target "CopyFSharpFiles" (fun _ ->
    ["./tools/FSharp/FSharp.Core.optdata"
     "./tools/FSharp/FSharp.Core.sigdata"]
      |> CopyTo buildDir
)


open Fake.AssemblyInfoFile

Target "SetAssemblyInfo" (fun _ ->
    CreateFSharpAssemblyInfo "./src/app/FAKE/AssemblyInfo.fs"
        [Attribute.Title "FAKE - F# Make Command line tool"
         Attribute.Guid "fb2b540f-d97a-4660-972f-5eeff8120fba"
         Attribute.Product "FAKE - F# Make"
         Attribute.Version buildVersion
         Attribute.FileVersion buildVersion]

    CreateFSharpAssemblyInfo "./src/app/Fake.Deploy/AssemblyInfo.fs"
        [Attribute.Title "FAKE - F# Make Deploy tool"
         Attribute.Guid "413E2050-BECC-4FA6-87AA-5A74ACE9B8E1"
         Attribute.Product "FAKE - F# Make"
         Attribute.Version buildVersion
         Attribute.FileVersion buildVersion]

    CreateFSharpAssemblyInfo "./src/deploy.web/Fake.Deploy.Web.App/AssemblyInfo.fs"
        [Attribute.Title "FAKE - F# Make Deploy Web App"
         Attribute.Guid "2B684E7B-572B-41C1-86C9-F6A11355570E"
         Attribute.Product "FAKE - F# Make"
         Attribute.Version buildVersion
         Attribute.FileVersion buildVersion]

    CreateFSharpAssemblyInfo "./src/deploy.web/Fake.Deploy.Web/AssemblyInfo.cs"
        [Attribute.Title "FAKE - F# Make Deploy Web"
         Attribute.Guid "27BA7705-3F57-47BE-B607-8A46B27AE876"
         Attribute.Product "FAKE - F# Make"
         Attribute.Version buildVersion
         Attribute.FileVersion buildVersion]

    CreateFSharpAssemblyInfo "./src/app/FakeLib/AssemblyInfo.fs"
        [Attribute.Title "FAKE - F# Make Lib"
         Attribute.Guid "d6dd5aec-636d-4354-88d6-d66e094dadb5"
         Attribute.Product "FAKE - F# Make"
         Attribute.Version buildVersion
         Attribute.FileVersion buildVersion]

    CreateFSharpAssemblyInfo "./src/app/Fake.SQL/AssemblyInfo.fs"
        [Attribute.Title "FAKE - F# Make SQL Lib"
         Attribute.Guid "A161EAAF-EFDA-4EF2-BD5A-4AD97439F1BE"
         Attribute.Product "FAKE - F# Make"
         Attribute.Version buildVersion
         Attribute.FileVersion buildVersion]
)

Target "BuildApp" (fun _ ->        
    !! @"src/app/**/*.*sproj"             
    |> MSBuildRelease buildDir "Build"
    |> Log "AppBuild-Output: "
)

Target "GenerateDocumentation" (fun _ ->
    !! (buildDir @@ "Fake*.dll")
    |> Docu (fun p ->
        {p with
            ToolPath = buildDir @@ "docu.exe"
            TemplatesPath = @".\tools\Docu\templates\"
            OutputPath = docsDir })
)

Target "CopyDocu" (fun _ -> 
    ["./tools/Docu/docu.exe"
     "./tools/Docu/DocuLicense.txt"]
       |> CopyTo buildDir
)

Target "CopyLicense" (fun _ -> 
    ["License.txt"
     "readme.markdown"
     "changelog.markdown"]
       |> CopyTo buildDir
)

Target "BuildZip" (fun _ ->     
    !+ (buildDir @@ @"**/*.*") 
    -- "*.zip" 
    -- "**/*.pdb"
      |> Scan
      |> Zip buildDir deployZip
)

Target "BuildTest" (fun _ -> 
   !! @"src/test/**/*.*sproj"
   |> MSBuildDebug testDir "Build"
   |> Log "TestBuild-Output: "
)

Target "Test" (fun _ ->
    let MSpecVersion = GetPackageVersion packagesDir "Machine.Specifications"
    let mspecTool = sprintf @"%s/Machine.Specifications.%s/tools/mspec-clr4.exe" packagesDir MSpecVersion

    !! (testDir @@ "Test.*.dll") 
      |> MSpec (fun p -> 
            {p with
                ToolPath = mspecTool
                ExcludeTags = ["HTTP"]
                HtmlOutputDir = reportDir}) 
)

Target "ZipDocumentation" (fun _ ->    
    !! (docsDir @@ @"**/*.*")  
      |> Zip docsDir (deployDir @@ sprintf "Documentation-%s.zip" buildVersion)
)

Target "CreateNuGet" (fun _ -> 
    let nugetDocsDir = nugetDir @@ "docs"
    let nugetToolsDir = nugetDir @@ "tools"

    CopyDir nugetDocsDir docsDir allFiles  
    CopyDir nugetToolsDir buildDir allFiles
    CopyDir nugetToolsDir @"./lib/fsi" allFiles
    DeleteFile (nugetToolsDir @@ "Gallio.dll")

    NuGet (fun p -> 
        {p with
            Authors = authors
            Project = projectName
            Description = projectDescription                               
            OutputPath = nugetDir
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey" }) "fake.nuspec"
)

Target "Default" DoNothing

// Dependencies
"Clean"
    ==> "RestorePackages"
    ==> "CopyFSharpFiles"
    =?> ("SetAssemblyInfo",not isLocalBuild ) 
    ==> "BuildApp" <=> "BuildTest"
    ==> "Test"
    ==> "CopyLicense" <=> "CopyDocu"
    ==> "BuildZip"
    ==> "GenerateDocumentation"
    ==> "ZipDocumentation"
    ==> "CreateNuGet"
    ==> "Default"

// start build
RunTargetOrDefault "Default"
