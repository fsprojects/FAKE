// include Fake libs
#I @"..\..\tools\FAKE\"
#r "FakeLib.dll"

open Fake

let projectName = "Fake_Website"
let projectSummary = "FAKE - F# Make - Sample website"
let projectDescription = "FAKE - F# Make - is a build automation tool for .NET"
let authors = ["Steffen Forkmann"; "Mauricio Scheffer"; "Colin Bull"]

// Directories
let buildDir  = @".\build\"
let testDir   = @".\test\"
let deployDir = @".\Publish\"
let reportDir = @".\report\"
let nugetDir = @".\nuget\" 
let packagesDir = @".\packages\"

// tools
let MSpecVersion = GetPackageVersion packagesDir "Machine.Specifications"
let mspecTool = sprintf @"%sMachine.Specifications.%s\tools\mspec-clr4.exe" packagesDir MSpecVersion

// Filesets
let appReferences  = 
    !+ @"src\app\**\*.csproj" 
      ++ @"src\app\**\*.fsproj" 
        |> Scan

let testReferences = !! @"src\test\**\*.csproj"  // !! creates a lazy fileset

// version info
let version = "0.1"  // or retrieve from CI server

// Targets
Target "Clean" (fun _ -> 
    CleanDirs [buildDir; testDir; deployDir; reportDir; nugetDir]
)

Target "BuildApp" (fun _ ->
    AssemblyInfo 
        (fun p -> 
        {p with
            CodeLanguage = CSharp;
            AssemblyVersion = version;
            AssemblyTitle = "Fake_Website";
            AssemblyDescription = "Sample website for FAKE - F# MAKE";
            Guid = "78B65159-BCE7-413A-A7A7-3B7BB8277E72";
            OutputFileName = @".\src\app\Fake_Website\Properties\AssemblyInfo.cs"})             
      
    // compile all projects below src\app\
    MSBuildRelease buildDir "Build" appReferences
        |> Log "AppBuild-Output: "
)

Target "BuildTest" (fun _ ->
    MSBuildDebug testDir "Build" testReferences
        |> Log "TestBuild-Output: "
)

Target "Test" (fun _ ->  
    !! (testDir @@ "*.Tests.dll") 
      |> MSpec (fun p -> 
            {p with
                ToolPath = mspecTool
                HtmlOutputDir = reportDir}) 
)

Target "CreateNuget" (fun _ ->     
    XCopy @".\deployment\" nugetDir
    XCopy @".\build\_publishedWebsites\Fake_WebSite\" (nugetDir @@ "website")
    XCopy @"..\..\tools\FAKE\" (nugetDir @@ "tools\FAKE")

    NuGet (fun p -> 
        {p with               
            Authors = authors
            Project = projectName
            Version = version
            Description = projectDescription                               
            ToolPath = @"..\..\tools\Nuget\Nuget.exe"                             
            OutputPath = nugetDir }) "Fake_Website.nuspec"
)

Target "Publish" (fun _ ->     
    !! (nugetDir + "*.nupkg") 
      |> Copy deployDir
)

Target "Deploy" (fun _ ->   
    !! (deployDir + "*.nupkg") 
      |> Seq.head
      |> DeploymentHelper.DeployPackageLocally
)

Target "Default" DoNothing

// Dependencies
"Clean"
  ==> "BuildApp" <=> "BuildTest"
  ==> "Test"
  ==> "CreateNuget"
  ==> "Publish"
  ==> "Deploy"
  ==> "Default"
 
// start build
RunParameterTargetOrDefault "target" "Default"