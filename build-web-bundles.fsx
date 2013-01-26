#r @"tools\FAKE\tools\FakeLib.dll"

open System
open Fake

let buildDir = "./build-bundles"
let targetDir = "./src/deploy.web/Fake.Deploy.Web/Bundles"
let bundleProjects  = !! @"src\deploy.web\Fake.Deploy.Web.DataProviders\**\*.*sproj"

Target "Clean" (fun _ -> 
    CleanDirs [buildDir; targetDir]
)

Target "SetAssemblyInfo" (fun _ ->
    AssemblyInfo 
        (fun p -> 
        {p with
            CodeLanguage = FSharp;
            AssemblyVersion = buildVersion;
            AssemblyTitle = "FAKE - F# Deploy Web RavenDB Provider";
            Guid = "A96DF3AB-BF56-4252-9C5F-9F2F6DAD5E8B";
            OutputFileName = @".\src\deploy.web\Fake.Deploy.Web.DataProviders\Fake.Deploy.Web.RavenDb\AssemblyInfo.fs"})
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
        let files = !! dir.FullName
        Zip dir.FullName name files
)

Target "Default" DoNothing

// Dependencies
"Clean"
    ==> "BuildBundles" <=> "ZipBundles"
    ==> "Default"
  
if not isLocalBuild then
    "Clean" ==> "SetAssemblyInfo" ==> "BuildBundles" |> ignore

// start build
RunParameterTargetOrDefault "target" "Default"