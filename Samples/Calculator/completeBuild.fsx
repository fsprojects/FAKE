// include Fake libs
#I "tools\FAKE"
#r "FakeLib.dll"

open Fake

// Directories
let buildDir  = @".\build\"
let testDir   = @".\test\"
let deployDir = @".\deploy\"

// tools
let nunitPath = @".\Tools\NUnit"
let fxCopRoot = @".\Tools\FxCop\FxCopCmd.exe"

// Filesets
let appReferences  = 
    !+ @"src\app\**\*.csproj" 
      ++ @"src\app\**\*.fsproj" 
        |> Scan

let testReferences = 
    !+ @"src\test\**\*.csproj" 
      |> Scan

// version info
let version = "0.2"  // or retrieve from CI server

// Targets
Target "Clean" (fun _ -> 
    CleanDirs [buildDir; testDir; deployDir]
)

Target "BuildApp" (fun _ ->
    AssemblyInfo 
        (fun p -> 
        {p with
            CodeLanguage = CSharp;
            AssemblyVersion = version;
            AssemblyTitle = "Calculator Command line tool";
            AssemblyDescription = "Sample project for FAKE - F# MAKE";
            Guid = "A539B42C-CB9F-4a23-8E57-AF4E7CEE5BAA";
            OutputFileName = @".\src\app\Calculator\Properties\AssemblyInfo.cs"})
              
    AssemblyInfo 
        (fun p -> 
        {p with
            CodeLanguage = CSharp;
            AssemblyVersion = version;
            AssemblyTitle = "Calculator library";
            AssemblyDescription = "Sample project for FAKE - F# MAKE";
            Guid = "EE5621DB-B86B-44eb-987F-9C94BCC98441";
            OutputFileName = @".\src\app\CalculatorLib\Properties\AssemblyInfo.cs"})          
      
    // compile all projects below src\app\
    MSBuildRelease buildDir "Build" appReferences
        |> Log "AppBuild-Output: "
)

Target "BuildTest" (fun _ ->
    MSBuildDebug testDir "Build" testReferences
        |> Log "TestBuild-Output: "
)

Target "NUnitTest" (fun _ ->  
    !+ (testDir + @"\NUnit.Test.*.dll") 
        |> Scan
        |> NUnit (fun p -> 
            {p with 
                ToolPath = nunitPath; 
                DisableShadowCopy = true; 
                OutputFile = testDir + @"TestResults.xml"})
)

Target "xUnitTest" (fun _ ->  
    !+ (testDir + @"\xUnit.Test.*.dll") 
        |> Scan
        |> xUnit (fun p -> 
            {p with 
                ShadowCopy = false;
                HtmlOutput = true;
                XmlOutput = true;
                OutputDir = testDir })
)

Target "FxCop" (fun _ ->
    !+ (buildDir + @"\**\*.dll") 
        ++ (buildDir + @"\**\*.exe") 
        |> Scan  
        |> FxCop (fun p -> 
            {p with                     
                ReportFileName = testDir + "FXCopResults.xml";
                ToolPath = fxCopRoot})
)

Target "Deploy" (fun _ ->
    !+ (buildDir + "\**\*.*") 
        -- "*.zip" 
        |> Scan
        |> Zip buildDir (deployDir + "Calculator." + version + ".zip")
)

Target "Test" DoNothing

// Dependencies
AllTargetsDependOn "Clean"
"NUnitTest" <== ["BuildApp"; "BuildTest"; "FxCop"]
"xUnitTest" <== ["BuildApp"; "BuildTest"; "FxCop"]
"Test" <== ["xUnitTest"; "NUnitTest"]
"Deploy" <== ["Test"]
 
// start build
Run "Deploy"