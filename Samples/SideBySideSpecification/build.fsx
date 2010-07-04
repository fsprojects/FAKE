// include Fake libs
#I @"tools\FAKE"
#r "FakeLib.dll"

open Fake
open Fake.MSBuild

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

// version info
let version = "0.2"  // or retrieve from CI server

// Targets
Target? Clean <-
    fun _ -> CleanDirs [buildDir; testDir; deployDir]

Target? BuildApp <-
    fun _ -> 
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
        appReferences 
          |> Seq.map (fun project -> 
                let target = project + "_Spliced"
                RemoveTestsFromProject 
                    (fun s -> s.StartsWith("nunit"))
                    (fun s -> s.EndsWith("Specs.cs"))
                    target
                    project
                target)
          |> MSBuildRelease buildDir "Build"
          |> Log "AppBuild-Output: "

Target? BuildTest <-
    fun _ -> 
        MSBuildDebug testDir "Build" appReferences
          |> Log "TestBuild-Output: "

Target? NUnitTest <-
    fun _ ->  
        !+ (testDir + @"\*.dll") 
          |> Scan
          |> NUnit (fun p -> 
                {p with 
                    ToolPath = nunitPath; 
                    DisableShadowCopy = true; 
                    OutputFile = testDir + @"TestResults.xml"})

Target? Deploy <-
    fun _ ->
        !+ (buildDir + "\**\*.*") 
          -- "*.zip" 
          |> Scan
          |> Zip buildDir (deployDir + "Calculator." + version + ".zip")

Target? Default <- DoNothing
Target? Test <- DoNothing

// Dependencies
For? BuildApp <- Dependency? Clean    
For? BuildTest <- Dependency? Clean
For? NUnitTest <- Dependency? BuildApp |> And? BuildTest    
For? Test <- Dependency? NUnitTest      
For? Deploy <- Dependency? Test      
For? Default <- Dependency? Deploy
 
// start build
Run? Default