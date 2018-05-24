module Fake.DotNet.AssemblyInfoFileTests

open Fake.DotNet
open Expecto
open System.Reflection
open System.IO

let currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
let testFile fileName = Path.Combine(currentDir, "TestFiles", "Fake.DotNet.AssemblyInfoFile", fileName)

let attributeByName (info:seq<AssemblyInfo.Attribute>) name = info |> Seq.filter(fun a -> a.Name = name) |> Seq.head    

[<Tests>]
let tests =
    testList "Fake.DotNet.AssemblyInfoFile.Tests" [
        Fake.ContextHelper.fakeContextTestCase "Test that we can read cs assembly info" <| fun _ ->     
            let info = AssemblyInfoFile.getAttributes (testFile "AssemblyInfo.cs")     

            Expect.equal "\"MyTitle\"" (attributeByName info "AssemblyTitle").Value "AssemblyTitle value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyTitle").Namespace "AssemblyTitle namespace is not correct"

            Expect.equal "\"MyDescription\"" (attributeByName info "AssemblyDescription").Value "AssemblyDescription value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyDescription").Namespace "AssemblyDescription namespace is not correct"

            Expect.equal "\"\"" (attributeByName info "AssemblyConfiguration").Value "AssemblyConfiguration value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyConfiguration").Namespace "AssemblyConfiguration namespace is not correct"

            Expect.equal "\"MyCompany\"" (attributeByName info "AssemblyCompany").Value "AssemblyCompany value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyCompany").Namespace "AssemblyCompany namespace is not correct"

            Expect.equal "\"MyProduct\"" (attributeByName info "AssemblyProduct").Value "AssemblyProduct value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyProduct").Namespace "AssemblyProduct namespace is not correct"

            Expect.equal "\"\"" (attributeByName info "AssemblyCopyright").Value "AssemblyCopyright value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyCopyright").Namespace "AssemblyCopyright namespace is not correct"

            Expect.equal "\"\"" (attributeByName info "AssemblyTrademark").Value "AssemblyTrademark value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyTrademark").Namespace "AssemblyTrademark namespace is not correct"

            Expect.equal "\"\"" (attributeByName info "AssemblyCulture").Value "AssemblyCulture value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyCulture").Namespace "AssemblyCulture namespace is not correct"

            Expect.equal "false" (attributeByName info "ComVisible").Value "ComVisible value is not correct"
            Expect.equal "System.Runtime.InteropServices" (attributeByName info "ComVisible").Namespace "ComVisible namespace is not correct"

            Expect.equal "\"da5f2252-0fe8-47df-af83-0d6ee1d64c9b\"" (attributeByName info "Guid").Value "Guid value is not correct"
            Expect.equal "System.Runtime.InteropServices" (attributeByName info "Guid").Namespace "Guid namespace is not correct"

            Expect.equal "\"0.0.0.0\"" (attributeByName info "AssemblyVersion").Value "AssemblyVersion value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyVersion").Namespace "AssemblyVersion namespace is not correct"

            Expect.equal "\"0.0.0.0\"" (attributeByName info "AssemblyFileVersion").Value "AssemblyFileVersion value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyFileVersion").Namespace "AssemblyFileVersion namespace is not correct"

        Fake.ContextHelper.fakeContextTestCase "Test that we can read fs assembly info" <| fun _ ->     
            let info = AssemblyInfoFile.getAttributes (testFile "AssemblyInfo.fs")

            Expect.equal "\"MyTitle\"" (attributeByName info "AssemblyTitle").Value "AssemblyTitle value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyTitle").Namespace "AssemblyTitle namespace is not correct"

            Expect.equal "\"MyProduct\"" (attributeByName info "AssemblyProduct").Value "AssemblyProduct value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyProduct").Namespace "AssemblyProduct namespace is not correct"

            Expect.equal "\"0.0.0.0\"" (attributeByName info "AssemblyVersion").Value "AssemblyVersion value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyVersion").Namespace "AssemblyVersion namespace is not correct"

            Expect.equal "\"0.0.0.0\"" (attributeByName info "AssemblyFileVersion").Value "AssemblyFileVersion value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyFileVersion").Namespace "AssemblyFileVersion namespace is not correct"

            Expect.equal "\"0.0.0.0\"" (attributeByName info "AssemblyInformationalVersion").Value "AssemblyInformationalVersion value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyInformationalVersion").Namespace "AssemblyInformationalVersion namespace is not correct"

        Fake.ContextHelper.fakeContextTestCase "Test that we can read vb assembly info" <| fun _ ->     
            let info = AssemblyInfoFile.getAttributes (testFile "AssemblyInfo.vb")

            Expect.equal "\"MyTitle\"" (attributeByName info "AssemblyTitle").Value "AssemblyTitle value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyTitle").Namespace "AssemblyTitle namespace is not correct"

            Expect.equal "\"MyDescription\"" (attributeByName info "AssemblyDescription").Value "AssemblyDescription value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyDescription").Namespace "AssemblyDescription namespace is not correct"

            Expect.equal "\"\"" (attributeByName info "AssemblyConfiguration").Value "AssemblyConfiguration value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyConfiguration").Namespace "AssemblyConfiguration namespace is not correct"

            Expect.equal "\"MyCompany\"" (attributeByName info "AssemblyCompany").Value "AssemblyCompany value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyCompany").Namespace "AssemblyCompany namespace is not correct"

            Expect.equal "\"MyProduct\"" (attributeByName info "AssemblyProduct").Value "AssemblyProduct value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyProduct").Namespace "AssemblyProduct namespace is not correct"

            Expect.equal "\"\"" (attributeByName info "AssemblyCopyright").Value "AssemblyCopyright value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyCopyright").Namespace "AssemblyCopyright namespace is not correct"

            Expect.equal "\"\"" (attributeByName info "AssemblyTrademark").Value "AssemblyTrademark value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyTrademark").Namespace "AssemblyTrademark namespace is not correct"

            Expect.equal "\"\"" (attributeByName info "AssemblyCulture").Value "AssemblyCulture value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyCulture").Namespace "AssemblyCulture namespace is not correct"

            Expect.equal "false" (attributeByName info "ComVisible").Value "ComVisible value is not correct"
            Expect.equal "System.Runtime.InteropServices" (attributeByName info "ComVisible").Namespace "ComVisible namespace is not correct"

            Expect.equal "\"da5f2252-0fe8-47df-af83-0d6ee1d64c9b\"" (attributeByName info "Guid").Value "Guid value is not correct"
            Expect.equal "System.Runtime.InteropServices" (attributeByName info "Guid").Namespace "Guid namespace is not correct"

            Expect.equal "\"0.0.0.0\"" (attributeByName info "AssemblyVersion").Value "AssemblyVersion value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyVersion").Namespace "AssemblyVersion namespace is not correct"

            Expect.equal "\"0.0.0.0\"" (attributeByName info "AssemblyFileVersion").Value "AssemblyFileVersion value is not correct"
            Expect.equal "System.Reflection" (attributeByName info "AssemblyFileVersion").Namespace "AssemblyFileVersion namespace is not correct"

        Fake.ContextHelper.fakeContextTestCase "Test that we can read cs assembly info and re-write assembly info" <| fun _ -> 
            let originalFilePath = (testFile "AssemblyInfo.cs")
            let newFilePath = (testFile "NewAssemblyInfo.cs")           

            AssemblyInfoFile.create newFilePath (AssemblyInfoFile.getAttributes originalFilePath) (Some(AssemblyInfoFileConfig(false, true, "System")))

            let originalContent = (File.ReadAllText originalFilePath).Replace("\r", "")
            let newContent = (File.ReadAllText newFilePath).Replace("\r", "")

            Expect.equal originalContent newContent "File content differs when read and write"

        Fake.ContextHelper.fakeContextTestCase "Test that we can read fs assembly info and re-write assembly info" <| fun _ -> 
            let originalFilePath = (testFile "AssemblyInfo.fs")
            let newFilePath = (testFile "NewAssemblyInfo.fs")           

            AssemblyInfoFile.create newFilePath (AssemblyInfoFile.getAttributes originalFilePath) (Some(AssemblyInfoFileConfig(false, true, "System")))

            let originalContent = (File.ReadAllText originalFilePath).Replace("\r", "")
            let newContent = (File.ReadAllText newFilePath).Replace("\r", "")

            Expect.equal originalContent newContent "File content differs when read and write"

        Fake.ContextHelper.fakeContextTestCase "Test that we can read vb assembly info and re-write assembly info" <| fun _ -> 
            let originalFilePath = (testFile "AssemblyInfo.vb")
            let newFilePath = (testFile "NewAssemblyInfo.vb")           

            AssemblyInfoFile.create newFilePath (AssemblyInfoFile.getAttributes originalFilePath) (Some(AssemblyInfoFileConfig(false, true, "System")))

            let originalContent = (File.ReadAllText originalFilePath).Replace("\r", "")
            let newContent = (File.ReadAllText newFilePath).Replace("\r", "")

            Expect.equal originalContent newContent "File content differs when read and write"      
    ]
