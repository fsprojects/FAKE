module Fake.DotNet.SdkAssemblyResolver.Tests

open System
open System.IO
open System.Runtime.InteropServices
open System.Text
open Fake.DotNet
open Fake.IO
open Expecto
open Fake.Runtime.SdkAssemblyResolver
open Fake.Runtime.Trace
open Fake.SystemHelper

module TestData =
    let testFilePath file =
        Path.Combine(__SOURCE_DIRECTORY__, "TestFiles", "Fake.DotNet.Xdt.Files", file)

    let replaceNewLines text =
        RegularExpressions.Regex.Replace(text, @"\r\n?|\n", Environment.NewLine)

    let exists file =
        File.Exists(file)

    let require file =
        if not (exists file) then
            invalidArg "file" (sprintf "Unable to read test data from %s"
                                       (Path.GetFullPath(file)))

    let read file =
        require file
        File.ReadAllText(file, Encoding.UTF8)
        |> replaceNewLines

    let copy source dest =
        require source
        File.Copy(source, dest, true)

    let delete file =
        File.Delete(file)

    let withTestDir f =
        let tempFolder = Path.GetTempFileName()
        File.Delete(tempFolder)
        Directory.CreateDirectory(tempFolder)
            |> ignore
        try
            f tempFolder
        finally
            try
                Directory.Delete(tempFolder, true)
            with
            | :? DirectoryNotFoundException -> ()

open Fake.IO.FileSystemOperators

[<Tests>]
let tests =
    testList "Fake.DotNet.SdkAssemblyResolver.Tests" [
        test "follows symlinks when dotnet is symlinked" {
            TestData.withTestDir (fun dir ->
                let corelib =
                    // System.Private.CoreLib.dll
                    System.Reflection.Assembly.GetAssembly(typeof<int>).Location
                let dotnetContainingPath =
                    corelib
                    // 6.0.3 (for example)
                    |> Path.getDirectory
                    // Microsoft.NETCore.App
                    |> Path.getDirectory
                    // shared
                    |> Path.getDirectory
                    // dotnet
                    |> Path.getDirectory

                let exeExtension = if RuntimeInformation.IsOSPlatform OSPlatform.Windows then ".exe" else ""
                let dotnetExeName = sprintf "dotnet%s" exeExtension
                let dotnetExe = Path.Combine(dotnetContainingPath, dotnetExeName)

                let customDotnet = dir </> dotnetExeName
                let symlinkedDotnet = File.CreateSymbolicLink (customDotnet, dotnetExe)

                Expect.isTrue symlinkedDotnet.Exists "how do symbolic links work :("

                let resolverEnvVar = "FAKE_SDK_RESOLVER_CUSTOM_DOTNET_PATH"
                let dotnetHostPathEnvVar = "DOTNET_HOST_PATH"
                let oldDotnetHostPath = Environment.GetEnvironmentVariable dotnetHostPathEnvVar
                let oldResolver = Environment.GetEnvironmentVariable resolverEnvVar

                try
                    Environment.SetEnvironmentVariable(dotnetHostPathEnvVar, customDotnet)

                    let resolver = SdkAssemblyResolver VerboseLevel.Silent
                    Environment.SetEnvironmentVariable(resolverEnvVar, "")

                    // Observe that this does not throw
                    resolver.SdkReferenceAssemblies ()
                    |> ignore
                finally
                    Environment.SetEnvironmentVariable(dotnetHostPathEnvVar, oldDotnetHostPath)
                    Environment.SetEnvironmentVariable(resolverEnvVar, oldResolver)
            )
        }
    ]
