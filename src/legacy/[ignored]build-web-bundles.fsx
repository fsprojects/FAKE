#I @"tools/FAKE/tools/"
#r @"FakeLib.dll"

open System
open System.IO
open Fake

let buildDir = "./build-bundles"
let targetDir = "./src/deploy.web/Fake.Deploy.Web/Bundles"
let bundleDir = @"src\deploy.web\Fake.Deploy.Web.DataProviders"
let bundleProjects  = !! (bundleDir + @"\**\*.*sproj")

Target "Clean" (fun _ ->
    CleanDirs [buildDir; targetDir]
)

Target "SetAssemblyInfo" (fun _ ->
    Directory.EnumerateDirectories(bundleDir)
    |> Seq.map(fun d -> d, (Path.GetFileName(d)).Split([|'.'|]))
    |> Seq.map(fun d -> fst d, snd d |> List.ofArray |> List.tail)
    |> Seq.map(fun d -> fst d, String.Join(" ", snd d))
    |> List.ofSeq
    |> Seq.iter(fun d ->
        let dir, name = d
        AssemblyInfo
            (fun p ->
            {p with
                CodeLanguage = FSharp
                AssemblyVersion = buildVersion
                AssemblyTitle = "FAKE - F# " + name + " Provider"
                Guid = Guid.NewGuid().ToString()
                OutputFileName = dir + @"\AssemblyInfo.fs"})
    )
)

Target "BuildBundles" (fun _ ->
    for bundle in bundleProjects do
        let bundleBuild = buildDir @@ IO.Path.GetFileNameWithoutExtension(bundle)
        MSBuildRelease bundleBuild "Build" [bundle] |> ignore
)

Target "ZipBundles" (fun _ ->
    for dir in IO.Directory.EnumerateDirectories(buildDir) do
        let dir = IO.DirectoryInfo(dir)
        let name = targetDir @@ dir.Name + ".zip"
        let files = IO.Directory.EnumerateFiles(dir.FullName, "*.*", IO.SearchOption.AllDirectories)
        Zip dir.FullName name files
)

Target "Default" DoNothing

// Dependencies
"Clean"
    =?> ("SetAssemblyInfo",not isLocalBuild) 
    ==> "BuildBundles"
    ==> "ZipBundles"
    ==> "Default"

// start build
RunTargetOrDefault "Default"
