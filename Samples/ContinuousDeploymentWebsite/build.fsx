// include Fake lib
#r @"tools\FAKE\tools\FakeLib.dll"

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

// version info
let version = ReadFileAsString "version.txt" // or retrieve from CI server

// Targets
Target "Clean" (fun _ -> 
    CleanDirs [buildDir; testDir; deployDir; reportDir; nugetDir]
    RestorePackages()
)

Target "AssemblyInfo" (fun _ ->
    AssemblyInfo 
        (fun p -> 
        {p with
            CodeLanguage = CSharp;
            AssemblyVersion = version;
            AssemblyTitle = "Fake_Website";
            AssemblyDescription = "Sample website for FAKE - F# MAKE";
            Guid = "78B65159-BCE7-413A-A7A7-3B7BB8277E72";
            OutputFileName = @".\src\app\Fake_Website\Properties\AssemblyInfo.cs"})             
)

Target "BuildApp" (fun _ ->
    !! @"src\app\**\*.csproj" 
      ++ @"src\app\**\*.fsproj" 
        |> MSBuildRelease buildDir "Build"
        |> Log "Build-Output: "
)

Target "BuildTest" (fun _ ->
    !! @"src\test\**\*.csproj"
      |> MSBuildDebug testDir "Build"
      |> Log "TestBuild-Output: "
)

Target "Test" (fun _ ->
    let MSpecVersion = GetPackageVersion packagesDir "Machine.Specifications"
    let mspecTool = sprintf @"%sMachine.Specifications.%s\tools\mspec-clr4.exe" packagesDir MSpecVersion

    !! (testDir @@ "*.Tests.dll") 
      |> MSpec (fun p -> 
            {p with
                ToolPath = mspecTool
                HtmlOutputDir = reportDir}) 
)

Target "CreateNuget" (fun _ ->     
    XCopy @".\deployment\" nugetDir
    XCopy @".\build\_publishedWebsites\Fake_WebSite\" (nugetDir @@ "website")
    XCopy @".\tools\FAKE\" (nugetDir @@ "tools\FAKE")

    "Fake_Website.nuspec"
      |> NuGet (fun p -> 
            {p with               
                Authors = authors
                Project = projectName
                Version = version
                NoPackageAnalysis = true
                Description = projectDescription                               
                ToolPath = @".\tools\Nuget\Nuget.exe"                             
                OutputPath = nugetDir })
)

Target "Publish" (fun _ ->     
    !! (nugetDir + "*.nupkg") 
      |> Copy deployDir
)

// Dependencies
"Clean"
  ==> "AssemblyInfo"
  ==> "BuildApp"
  ==> "BuildTest"
  ==> "Test"
  ==> "CreateNuget"
  ==> "Publish"
 
// start build
RunParameterTargetOrDefault "target" "Publish"