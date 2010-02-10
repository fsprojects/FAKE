#I @"tools\FAKE"
#r "FakeLib.dll"

open Fake 
 
// properties 
let projectName = "FAKE"

TraceEnvironmentVariables()  
  
let buildDir = @".\build\"
let testDir = @".\test\"
let deployDir = @".\deploy\"
let docsDir = @".\docs\" 

let deployZip = deployDir + sprintf "%s-%s.zip" projectName buildVersion

// files
let appReferences  = !+ @"src\app\**\*.*proj"  |> Scan
let testReferences = !+ @"src\test\**\*.csproj" |> Scan

// tools
let nunitPath = @".\Tools\NUnit\bin"

// Targets
Target? Clean <-
    fun _ ->  CleanDirs [buildDir; testDir; deployDir; docsDir]


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
        let assemblies = 
          !+ (buildDir + @"\**\*.dll")
            ++ (buildDir + @"\**\*.exe")
            -- (@"\**\*SharpZipLib*")
            -- (@"\**\*SharpSvn*")
              |> Scan
              |> Seq.map FullName

        let tool = 
          findFile [
            @"c:\Program Files (x86)\FSharp-1.9.7.8\bin\"; 
            @"c:\Program Files\FSharp-1.9.7.8\bin\";
            @"c:\Programme\FSharp-1.9.7.8\bin\"] "fshtmldoc.exe"

        Copy docsDir [@".\HelpInput\msdn.css"]

        let commandLineBuilder =
          new System.Text.StringBuilder()
            |> appendFileNamesIfNotNull  assemblies
            |> append (sprintf "--outdir\" \"%s" (docsDir |> FullName |> trimSlash)) 
            |> append "--cssfile\" \"msdn.css" 
            |> append "--namespacefile\" \"namespaces.html" 
         
        trace (commandLineBuilder.ToString())
        if not (execProcess3 (fun info ->  
            info.FileName <- tool
            info.WorkingDirectory <- docsDir
            info.Arguments <- commandLineBuilder.ToString()))
        then
            failwith "Documentation generation failed."

Target? CopyLicense <-
    fun _ -> Copy buildDir [@"License.txt"]

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
        !+ (testDir + @"\Test.*.dll") 
          |> Scan
          |> NUnit (fun p -> 
                {p with 
                   ToolPath = nunitPath; 
                   DisableShadowCopy = true; 
                   OutputFile = testDir + @"TestResults.xml"}) 

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
            |> Zip @".\Samples\Calculator" (deployDir + sprintf "CalculatorSample-%s.zip" buildVersion)

Target? ZipDocumentation <-
    fun _ ->    
        !+ (docsDir + @"\**\*.*")  
          |> Scan
          |> Zip docsDir (deployDir + sprintf "Documentation-%s.zip" buildVersion)

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

//For? ZipDocumentation <-
//    Dependency? GenerateDocumentation

For? Default <- Dependency? Deploy

// start build
Run? Default