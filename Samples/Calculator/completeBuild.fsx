// include Fake libs
#r @"tools\FAKE\tools\FakeLib.dll"

open Fake

// Directories
let buildDir  = @".\build\"
let testDir   = @".\test\"
let deployDir = @".\deploy\"

// tools
let nunitPath = @".\Tools\NUnit"
let fxCopRoot = @".\Tools\FxCop\FxCopCmd.exe"
    
// version info
let version = "0.2"  // or retrieve from CI server

// Targets
Target "Clean" (fun _ -> 
    CleanDirs [buildDir; testDir; deployDir]
)

Target "SetVersions" (fun _ ->
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
)


Target "CompileApp" (fun _ ->    
    !+ @"src\app\**\*.csproj" 
      ++ @"src\app\**\*.fsproj" 
        |> Scan
        |> MSBuildRelease buildDir "Build" 
        |> Log "AppBuild-Output: "
)

Target "CompileTest" (fun _ ->
    !! @"src\test\**\*.csproj"
      |> MSBuildDebug testDir "Build"
      |> Log "TestBuild-Output: "
)

Target "NUnitTest" (fun _ ->  
    !! (testDir + @"\NUnit.Test.*.dll") 
        |> NUnit (fun p -> 
            {p with 
                ToolPath = nunitPath; 
                DisableShadowCopy = true; 
                OutputFile = testDir + @"TestResults.xml"})
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

Target "Zip" (fun _ ->
    !+ (buildDir + "\**\*.*") 
        -- "*.zip" 
        |> Scan
        |> Zip buildDir (deployDir + "Calculator." + version + ".zip")
)

// Dependencies
"Clean"
  ==> "SetVersions" 
  ==> "CompileApp" 
  ==> "CompileTest"
  ==> "FxCop"
  ==> "NUnitTest"  
  ==> "Zip"
 
// start build
Run "Zip"