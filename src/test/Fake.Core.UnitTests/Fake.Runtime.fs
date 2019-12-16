module Fake.RuntimeTests

open System.IO
open System.Reflection
open Fake.Runtime
open Fake.IO.FileSystemOperators
open Expecto
open Fake.IO
open Fake.Runtime.Runners

[<Tests>]
let tests = 
  testList "Fake.Runtime.Tests" [
    testCase "Test that cache helpers work" <| fun _ ->
      Path.fixPathForCache "build.fsx" "build.fsx"
      |> Flip.Expect.equal "should detect script itself" "scriptpath:///build.fsx"
      Path.readPathFromCache "build.fsx" "scriptpath:///build.fsx"
      |> Flip.Expect.equal "should detect script itself" (Path.GetFullPath "build.fsx")

    testCase "CoreCache.findInAssemblyList works with null token" <| fun _ ->
        let toFind = AssemblyName "Microsoft.ServiceFabric.Client.Http, Culture=neutral, PublicKeyToken=null"
        let res = { FullName = "Microsoft.ServiceFabric.Client.Http, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"; Version = "3.0.0.0"; Location = "C:\\invalid.dll" }
        let available : AssemblyInfo list = [ res ]
        let result = CoreCache.findInAssemblyList toFind available
        Expect.equal result (Some (true, res)) "Expected to retrieve Microsoft.ServiceFabric.Common"
        
    testCase "CoreCache.findInAssemblyList works with different version" <| fun _ ->
        let toFind = AssemblyName "Microsoft.ServiceFabric.Client.Http, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
        let res = { FullName = "Microsoft.ServiceFabric.Client.Http, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"; Version = "3.0.0.0"; Location = "C:\\invalid.dll" }
        let available : AssemblyInfo list = [ res ]
        let result = CoreCache.findInAssemblyList toFind available
        Expect.equal result (Some (false, res)) "Expected to retrieve Microsoft.ServiceFabric.Common"

    testCase "CoreCache.findInAssemblyList doesn't return different token" <| fun _ ->
        let toFind = AssemblyName "Microsoft.ServiceFabric.Client.Http, Version=2.0.0.0, Culture=neutral, PublicKeyToken=41bf3856ad364e35"
        let res = { FullName = "Microsoft.ServiceFabric.Client.Http, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"; Version = "3.0.0.0"; Location = "C:\\invalid.dll" }
        let available : AssemblyInfo list = [ res ]
        let result = CoreCache.findInAssemblyList toFind available
        Expect.equal result None "Expected to not retrieve Microsoft.ServiceFabric.Common"

    testCase "CoreCache.findInAssemblyList doesn't return different name" <| fun _ ->
        let toFind = AssemblyName "Microsoft.ServiceFabric.Client, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
        let res = { FullName = "Microsoft.ServiceFabric.Client.Http, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"; Version = "3.0.0.0"; Location = "C:\\invalid.dll" }
        let available : AssemblyInfo list = [ res ]
        let result = CoreCache.findInAssemblyList toFind available
        Expect.equal result None "Expected to not retrieve Microsoft.ServiceFabric.Common"

    testCase "Test that cache helpers work for nuget cache" <| fun _ ->
      let nugetLib = Paket.Constants.UserNuGetPackagesFolder </> "MyLib" </> "lib" </> "mylib.dll"
      Path.fixPathForCache "build.fsx" nugetLib
      |> Flip.Expect.equal "should detect script itself" "nugetcache:///MyLib/lib/mylib.dll"
      Path.readPathFromCache "build.fsx" "nugetcache:///MyLib/lib/mylib.dll"
      |> Flip.Expect.equal "should detect script itself" nugetLib

    testCase "Test that we can properly find type names when the file name contains '.'" <| fun _ ->
      // Add test if everything works with __SOURCE_FILE__
      let name, parser =
          CompileRunner.nameParser
             "build.test1.test2_E294A5A65B9A06E0358F991A589AC7246FA6677BA99829862925EF343588E50D"
             "build.test1.test2.fsx"

      Expect.equal
        "<StartupCode$build-test1-test2_E294A5A65B9A06E0358F991A589AC7246FA6677BA99829862925EF343588E50D>.$Build.test1.test2$fsx"
        name
        "Expected to have correct full type name"
    testCase "Test that we can tokenize __SOURCE_FILE__" <| fun _ ->
      // Add test if everything works with __SOURCE_FILE__
      
      Expect.equal "" "" "."

    // Add test if everything works with #ifdefed #r "paket: line"
    testCase "Test that we find the correct references" <| fun _ ->
      let scriptText = """
#if BOOTSTRAP && DOTNETCORE
#r "paket:
nuget Fake.Core.SemVer prerelease //"
#endif
      """
      let interesting =
          Fake.Runtime.FSharpParser.getTokenized "testfile.fsx" ["BOOTSTRAP"; "DOTNETCORE"; "FAKE"] (scriptText.Split([|'\r';'\n'|]))
          |> Fake.Runtime.FSharpParser.findInterestingItems
      let expected =
        [Fake.Runtime.FSharpParser.InterestingItem.Reference (sprintf "paket:\nnuget Fake.Core.SemVer prerelease //") ]
      Expect.equal expected interesting "Expected to find reference."
      
    // Add test if everything works with #ifdefed #r "paket: line"
    testCase "Test that we find the correct references without defines" <| fun _ ->
      let scriptText = """
#if BOOTSTRAP && DOTNETCORE
#r "paket:
nuget Fake.Core.SemVer prerelease //"
#endif
      """
      let interesting =
          Fake.Runtime.FSharpParser.getTokenized "testfile.fsx" ["DOTNETCORE"; "FAKE"] (scriptText.Split([|'\r';'\n'|]))
          |> Fake.Runtime.FSharpParser.findInterestingItems
      let expected = []
      Expect.equal expected interesting "Expected to find reference."

    // TODO: Add test if everything works with #ifdefed #r "paket: line"

    // Tests that we handle #I and #load properly
    testCase "Test #1947 - non-existing folders" <| fun _ ->
      let tmpDir = Path.GetTempFileName()
      File.Delete tmpDir
      Directory.CreateDirectory tmpDir |> ignore
      try
        Directory.CreateDirectory (tmpDir </> "packages" </> "Octokit" </> "lib" </> "net45") |> ignore
        Directory.CreateDirectory (tmpDir </> "paket-files" </> "test") |> ignore
        let scriptText = """
#load "paket-files/test/octokit.fsx"
      """
        let octokit = """
#I __SOURCE_DIRECTORY__
#I @"../../../../../packages/Octokit/lib/net45"
#I @"../../packages/Octokit/lib/net45"
#I @"../../../../../../packages/build/Octokit/lib/net45"
#r "Octokit.dll"
"""    
        File.WriteAllText(tmpDir </> "paket-files" </> "test" </> "octokit.fsx", octokit)
        let tokens =
            Fake.Runtime.FSharpParser.getTokenized "build.fsx" ["DOTNETCORE"; "FAKE"] (scriptText.Split([|'\r';'\n'|]))
        let scripts = HashGeneration.getAllScripts true [] tokens (tmpDir </> "build.fsx")
        let expected = [ 
            tmpDir </> "build.fsx"
            tmpDir </> "paket-files" </> "test" </> "octokit.fsx"
          ]
        let actual = scripts |> List.map (fun s -> s.Location)
        Expect.equal expected actual "Expected to find script."
      finally
        Directory.Delete(tmpDir, true)

    testCase "Test #1947 - test #I with ." <| fun _ ->
      let tmpDir = Path.GetTempFileName()
      File.Delete tmpDir
      Directory.CreateDirectory tmpDir |> ignore
      try
        let testScriptPath = tmpDir </> "test.fsx"
        let testScript = """
#I "test"
#I "."
#load "file.fsx"
"""      
        let fileScriptPath = tmpDir </> "test" </> "file.fsx"
        let fileScript = """
printfn "Test"
#load "other.fsx"
"""      
        let otherScriptPath = tmpDir </> "other.fsx"
        let otherScript = """
printfn "other.fsx"
"""      
        Directory.CreateDirectory (tmpDir </> "test") |> ignore
        File.WriteAllText(fileScriptPath, fileScript)
        File.WriteAllText(otherScriptPath, otherScript)
        let tokens =
            Fake.Runtime.FSharpParser.getTokenized "test.fsx" ["DOTNETCORE"; "FAKE"] (testScript.Split([|'\r';'\n'|]))
        let scripts = HashGeneration.getAllScripts true [] tokens testScriptPath
        let expected = [ 
            testScriptPath
            fileScriptPath
            otherScriptPath
          ]
        let actual = scripts |> List.map (fun s -> s.Location)
        Expect.equal expected actual "Expected to find script."
      finally
        Directory.Delete(tmpDir, true)
        
    testCase "Test #1947 - good error message" <| fun _ ->
      let tmpDir = Path.GetTempFileName()
      File.Delete tmpDir
      Directory.CreateDirectory tmpDir |> ignore
      try
        let testScriptPath = tmpDir </> "test.fsx"
        let testScript = """
#cd "asdas"
"""
        let tokens =
            Fake.Runtime.FSharpParser.getTokenized "test.fsx" ["DOTNETCORE"; "FAKE"] (testScript.Split([|'\r';'\n'|]))
        try
          let scripts = HashGeneration.getAllScripts true [] tokens testScriptPath
          Expect.isTrue false "Expected an exception"
        with e ->
          (e.Message.Contains "test.fsx(2,1): error FS2302: Directory '" && e.Message.Contains "' doesn't exist")
          |> Flip.Expect.isTrue (sprintf "Expected a good error message, but got: %s" e.Message)
      finally
        Directory.Delete(tmpDir, true)
  ]    
