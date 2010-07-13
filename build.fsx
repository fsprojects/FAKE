#I @"tools\FAKE"
#r "FakeLib.dll"

open Fake
 
// properties 
let projectName = "FAKE"

TraceEnvironmentVariables()  
  
let buildDir = @".\build\"
let testDir = @".\test\"
let metricsDir = @".\BuildMetrics\"
let deployDir = @".\Publish\"
let docsDir = @".\docs\" 
let templatesSrcDir = @".\docu\src\Docu.Console\templates\" 

let deployZip = deployDir + sprintf "%s-%s.zip" projectName buildVersion

// files
let appReferences  = !+ @"src\app\**\*.*sproj"  |> Scan
let testReferences = !+ @"src\test\**\*.*sproj" |> Scan

// tools
let nunitPath = @".\Tools\NUnit"

// Targets
Target? Clean <-
    fun _ ->  CleanDirs [buildDir; testDir; deployDir; docsDir; metricsDir]


Target? BuildApp <-
    fun _ ->   
        if not isLocalBuild then
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
                      
            AssemblyInfo 
             (fun p -> 
                {p with
                   CodeLanguage = FSharp;
                   AssemblyVersion = buildVersion;
                   AssemblyTitle = "FAKE - F# Make Git Lib";
                   Guid = "2101B852-0B08-4EAA-A343-85E399327A98";
                   OutputFileName = @".\src\app\Fake.Git\AssemblyInfo.fs"})                                                                  
                     
        MSBuildRelease buildDir "Build" appReferences
            |> Log "AppBuild-Output: "

Target? BuildDocu <-
    fun _ ->                                               
        MSBuildRelease null "Build" [@".\docu\Build.proj"]
            |> Log "DocuBuild-Output: "
        Copy buildDir [@".\docu\artifacts\docu.exe"; @".\docu\License.txt"]        
        Rename (buildDir + @"DocuLicense.txt") (buildDir + @"License.txt")


Target? GenerateDocumentation <-
    fun _ ->
        !+ (buildDir + "Fake*.dll")
          |> Scan
          |> Docu (fun p ->
                {p with
                    ToolPath = buildDir @@ "docu.exe"
                    TemplatesPath = templatesSrcDir
                    OutputPath = docsDir })
            

Target? CopyLicense <-
    fun _ -> Copy buildDir [@"License.txt"; @"readme.txt"]

Target? BuildZip <-
    fun _ ->     
      !+ (buildDir + @"\**\*.*") 
        -- "*.zip" 
        -- "**\*.pdb"
          |> Scan
          |> Zip buildDir deployZip

Target? BuildTest <-
    fun _ -> 
        MSBuildDebug testDir "Build" testReferences
          |> Log "TestBuild-Output: "

Target? Test <-
    fun _ ->  
        !+ (testDir + @"\*.dll") 
          |> Scan
          |> NUnit (fun p -> 
                {p with 
                   ToolPath = nunitPath; 
                   DisableShadowCopy = true; 
                   Framework = "net-2.0.50727";
                   OutputFile = metricsDir @@ "nunit-result.xml"}) 

Target? ZipCalculatorSample <-
    fun _ ->
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

Target? ZipDocumentation <-
    fun _ ->    
        !+ (docsDir + @"\**\*.*")  
          |> Scan
          |> Zip docsDir (deployDir @@ sprintf "Documentation-%s.zip" buildVersion)

Target? Deploy <- DoNothing
Target? Default <- DoNothing

// Dependencies

For? BuildApp <- Dependency? Clean
For? Test <- Dependency? Clean
For? BuildZip <- Dependency? BuildApp |> And? CopyLicense
For? ZipCalculatorSample <- Dependency? Clean
For? Test <- Dependency? BuildApp |> And? BuildTest

For? Deploy <- 
    Dependency? Test 
      |> And? BuildDocu 
      |> And? BuildZip 
      |> And? ZipCalculatorSample
      |> And? ZipDocumentation

For? GenerateDocumentation <- Dependency? BuildApp
For? ZipDocumentation <- Dependency? GenerateDocumentation
For? Default <- Dependency? Deploy

// start build
Run? Default