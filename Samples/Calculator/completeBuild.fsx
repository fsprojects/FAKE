#light
// include Fake libs
#I "tools\FAKE"
#r "FakeLib.dll"

// include CustomTask
#r "MyCustomTask.dll"
open Fake 

// open CustomNamespace
open MyCustomTask

// use custom functionality
let x = RandomNumberTask.RandomNumber(2,13)
sprintf "RandomNumber: %d" x |> trace

// properties
let buildDir  = @".\build\"
let testDir   = @".\test\"
let deployDir = @".\deploy\"
let appReferences  = !+ @"src\app\**\*.csproj" |> Scan
let testReferences = !+ @"src\test\**\*.csproj" |> Scan

// tools
let nunitPath = @".\Tools\NUnit\bin"
let fxCopRoot =
  let r = System.Environment.GetEnvironmentVariable("FXCOPROOT")
  if r <> "" && r <> null then r else 
  findFile [
    @"c:\Programme\Microsoft FxCop 1.36\"; 
    @"c:\Program Files\Microsoft FxCop 1.36\";
    @"c:\Program Files (x86)\Microsoft FxCop 1.36\"] "FxCopCmd.exe"
let version = "0.2"

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
      
        let target = "Build"
        
        // compile all projects below src\app\
        let apps = MSBuildRelease buildDir target appReferences
        
        // log the output files
        Log "AppBuild-Output: " apps

Target? BuildTest <-
    fun _ -> 
        let testApps = MSBuildDebug testDir "Build" testReferences
        Log "TestBuild-Output: " testApps

Target? NUnitTest <-
    fun _ ->  
        let testAssemblies = 
          !+ (testDir + @"\NUnit.Test.*.dll") |> Scan
            
        let output = testDir + @"TestResults.xml"
        NUnit (fun p -> 
            {p with 
               ToolPath = nunitPath; 
               DisableShadowCopy = true; 
               OutputFile = output}) 
          testAssemblies

Target? xUnitTest <-
    fun () ->  
        let testAssemblies = 
          !+ (testDir + @"\xUnit.Test.*.dll") 
            |> Scan
      
        xUnit 
          (fun p -> 
             {p with 
                 ShadowCopy = false;
                 HtmlOutput = true;
                 XmlOutput = true;
                 OutputDir = testDir }) 
          testAssemblies

Target? FxCop <-
    fun () ->
        let assemblies = 
          !+ (buildDir + @"\**\*.dll") 
            ++ (buildDir + @"\**\*.exe") 
            |> Scan  
            
        FxCop 
          (fun p -> 
            {p with 
              // override default parameters
              ReportFileName = testDir + "FXCopResults.xml";
              ToolPath = fxCopRoot})
          assemblies

Target? Deploy <-
    fun () ->
        let artifacts = !+ (buildDir + "\**\*.*") -- "*.zip" |> Scan
        let zipFileName = deployDir + "Calculator.zip" 
        Zip buildDir zipFileName artifacts

Target? Default <- DoNothing
Target? Test <- DoNothing

// Dependencies
For? BuildApp <-
    Dependency? Clean
    
For? BuildTest <-
    Dependency? Clean

For? NUnitTest <-
    Dependency? BuildApp
      |> And? BuildTest
      |> And? FxCop
      
For? xUnitTest <- 
    Dependency? BuildApp
      |> And? BuildTest
      |> And? FxCop
      
For? Test <-
    Dependency? xUnitTest
      |> And? NUnitTest
      
For? Deploy <-
    Dependency? Test
      
For? Default <- 
    Dependency? Deploy
 
// start build
Run? Default