#I @"tools\FAKE"
#r "FakeLib.dll"

open Fake
 
// properties 
let projectName = "FAKE"
let projectSummary = "FAKE - F# Make - Get rid of the noise in your build scripts."
let projectDescription = "FAKE - F# Make - is a build automation tool for .NET. Tasks and dependencies are specified in a DSL which is integrated in F#."
let authors = ["Steffen Forkmann"; "Mauricio Scheffer"]
let mail = "forkmann@gmx.de"
let homepage = "http://github.com/forki/fake"

TraceEnvironmentVariables()  
  
let buildDir = @".\build\"
let testDir = @".\test\"
let metricsDir = @".\BuildMetrics\"
let deployDir = @".\Publish\"
let docsDir = @".\docs\" 
let nugetDir = @".\nuget\" 
let templatesSrcDir = @".\tools\Docu\templates\" 

let deployZip = deployDir + sprintf "%s-%s.zip" projectName buildVersion

// files
let appReferences  = !+ @"src\app\**\*.*sproj"  |> Scan
let testReferences = !+ @"src\test\**\*.*sproj" |> Scan

// tools
let nunitPath = @".\Tools\NUnit"

// Targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir; testDir; deployDir; docsDir; metricsDir; nugetDir]
    CopyFile buildDir "./tools/FSharp/FSharp.Core.sigdata"
    CopyFile buildDir "./tools/FSharp/FSharp.Core.optdata"    
)

Target "SetAssemblyInfo" (fun _ ->
    AssemblyInfo 
        (fun p -> 
        {p with
            CodeLanguage = FSharp;
            AssemblyVersion = buildVersion;
            AssemblyTitle = "FAKE - F# Make Command line tool";
            Guid = "fb2b540f-d97a-4660-972f-5eeff8120fba";
            OutputFileName = @".\src\app\FAKE\AssemblyInfo.fs"})
                   
    AssemblyInfo 
        (fun p -> 
        {p with
            CodeLanguage = FSharp;
            AssemblyVersion = buildVersion;
            AssemblyTitle = "FAKE - F# Make Lib";
            Guid = "d6dd5aec-636d-4354-88d6-d66e094dadb5";
            OutputFileName = @".\src\app\FakeLib\AssemblyInfo.fs"})

    AssemblyInfo 
        (fun p -> 
        {p with
            CodeLanguage = FSharp;
            AssemblyVersion = buildVersion;
            AssemblyTitle = "FAKE - F# Make SQL Lib";
            Guid = "A161EAAF-EFDA-4EF2-BD5A-4AD97439F1BE";
            OutputFileName = @".\src\app\Fake.SQL\AssemblyInfo.fs"})
)

Target "BuildApp" (fun _ ->                     
    MSBuildRelease buildDir "Build" appReferences
        |> Log "AppBuild-Output: "

    Copy buildDir [@".\tools\Docu\docu.exe"; @".\tools\Docu\DocuLicense.txt"]
)

Target "GenerateDocumentation" (fun _ ->
    !+ (buildDir + "Fake*.dll")
        |> Scan
        |> Docu (fun p ->
            {p with
                ToolPath = buildDir @@ "docu.exe"
                TemplatesPath = templatesSrcDir
                OutputPath = docsDir })
)

Target "CopyLicense" (fun _ -> 
    Copy buildDir [@"License.txt"; @"readme.markdown"]
)

Target "BuildZip" (fun _ ->     
    !+ (buildDir + @"\**\*.*") 
    -- "*.zip" 
    -- "**/*.pdb"
        |> Scan
        |> Zip buildDir deployZip
)

Target "BuildTest" (fun _ -> 
    MSBuildDebug testDir "Build" testReferences
        |> Log "TestBuild-Output: "
)

Target "Test" (fun _ ->  
    !+ (testDir + @"\*.dll") 
        |> Scan
        |> NUnit (fun p -> 
            {p with 
                ToolPath = nunitPath; 
                DisableShadowCopy = true; 
                Framework = "net-2.0.50727";
                OutputFile = metricsDir @@ "nunit-result.xml"}) 
)

Target "ZipCalculatorSample" (fun _ ->
    // copy fake file output to sample tools path
    !+ (buildDir + @"\**\*.*") 
        |> Scan
        |> Copy @".\Samples\Calculator\tools\FAKE\"
        
    !+ @"Samples\Calculator\**\*.*" 
        -- "**\*Resharper*\**"
        -- "**\*Resharper*"
        -- "**\bin\Debug\**"
        -- "**\obj\Debug\**"
        -- "**\bin\Release\**"
        -- "**\obj\Release\**"
        |> Scan
        |> Zip @".\Samples\Calculator" (deployDir @@ sprintf "CalculatorSample-%s.zip" buildVersion)
)

Target "ZipDocumentation" (fun _ ->    
    !+ (docsDir + @"\**\*.*")  
        |> Scan
        |> Zip docsDir (deployDir @@ sprintf "Documentation-%s.zip" buildVersion)
)

Target "DeployNuGet" (fun _ -> 
    let nugetDocsDir = nugetDir @@ "docs/"
    let nugetToolsDir = nugetDir @@ "sol/"

    XCopy docsDir nugetDocsDir
    XCopy buildDir nugetToolsDir

    NuGet (fun p -> 
        {p with               
            Authors = authors
            Project = projectName
            Description = projectDescription                
            OutputPath = nugetDir })  "fake.nuspec"
)

Target "Deploy" DoNothing

// Dependencies

AllTargetsDependOn "Clean"
if not isLocalBuild then
    "BuildApp" <== ["SetAssemblyInfo"]

["BuildZip"; "Test"; "GenerateDocumentation"] |> TargetsDependOn "BuildApp"

"BuildZip" <== ["CopyLicense"]
"Test" <== ["BuildTest"]
"DeployNuGet" <== ["Test"; "BuildZip"; "ZipCalculatorSample"; "ZipDocumentation"]
"Deploy" <== ["DeployNuGet"]
"ZipDocumentation" <== ["GenerateDocumentation"]

// start build
Run "Deploy"
