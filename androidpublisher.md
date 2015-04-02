# Publish Android apk

This module helps android developers to automatically publish their APKs

## Prerequisite

Before using this module, you will need an android keystore for apk signing.

Next, you will need a Google service account described here: 
https://developers.google.com/accounts/docs/OAuth2ServiceAccount#creatinganaccount
and here: 
https://developers.google.com/android-publisher/getting_started


## Usage example:

    #r "packages/FAKE/tools/FakeLib.dll"

    let androidBuildDir = "./build/"
    let androidProdDir = "./pack/"

    androidProdDir |> ensureDirectory

    //Clean old apk
    Target "Clean" (fun _ ->
        CleanDir androidBuildDir
        CleanDir androidProdDir
    )

    Target "Android-Package" (fun () ->
        AndroidPackage(fun defaults ->
                        { defaults with 
                            ProjectPath = "Path to my project Droid.csproj"
                            Configuration = "Release"
                            OutputPath = androidBuildDir
                        })

        |> AndroidSignAndAlign (fun defaults ->
            { defaults with 
                KeystorePath = @"path to my file.keystore"
                KeystorePassword = "my password"
                KeystoreAlias = "my key alias"
            })
        |> fun file -> file.CopyTo(Path.Combine(androidProdDir, file.Name)) |> ignore

    )

    Target "Publish" (fun _ -> 
        // I like verbose script
        trace "publishing Android App"
        let apk = androidProdDir |> directoryInfo |> filesInDir |> Seq.filter(fun f -> f.Name.EndsWith(".apk")) |> Seq.exactlyOne
        let apkPath = apk.FullName
        tracefn "Apk found: %s" apkPath
        let mail = "my service account mail@developer.gserviceaccount.com"
        let certificate = new X509Certificate2(@"Path to my certificate file probably named 'Google Play Android Developer-xxxxxxxxxxxx.p12'", "notasecret", X509KeyStorageFlags.Exportable)
        let packageName = "my Android package name"

        // to publish an alpha version: 
        PublishApk { AlphaSettings with Config = { Certificate = certificate; PackageName = packageName; AccountId = mail; Apk = apkPath; } }

        // to publish a beta version: 
        // PublishApk { BetaSettings with Config = { Certificate = certificate; PackageName = packageName; AccountId = mail; Apk = apkPath; } }

        // to publish a production version: 
        // PublishApk { ProductionSettings with Config = { Certificate = certificate; PackageName = packageName; AccountId = mail; Apk = apkPath; } }
    )

    Target "Android-Build" (fun _ ->
        !! "**/my project Droid.csproj"
            |> MSBuildRelease androidBuildDir "Build"
            |> Log "BuildAndroidLib-Output: "
    )

    Target "Default" (fun _ ->
        trace "Building default target"
        RestorePackages()
    )

    "Clean"
        ==> "Android-Package"
        ==> "Default"

    RunTargetOrDefault "Default"

Default target will not start "Publish" target because apps do not need to be updated too frequently (as explained here: https://developers.google.com/android-publisher/api_usage)

To publish your app, you can run

    PS> Fake.exe .\build.fsx "target=publish"

