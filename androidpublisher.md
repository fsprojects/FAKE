# Publish Android apk

This module will help android developers to automatically publish their APKs

## Prerequisite

Before using this module, you will need an android keystore for apk signing.

Next, you will need a Google service account decribed here: 
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

	Target "android-package" (fun () ->
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

	Target "publish" (fun _ -> 
		// I like verbose script
        trace "publishing Android App"
        let apk = androidProdDir |> directoryInfo |> filesInDir |> Seq.filter(fun f -> f.Name.EndsWith(".apk")) |> Seq.exactlyOne
        let apkPath = apk.FullName
        tracefn "Apk found: %s" apkPath
        let mail = "my service account mail@developer.gserviceaccount.com"
        let certificate = new X509Certificate2(@"Path to my certificate file probably named 'Google Play Android Developer-xxxxxxxxxxxx.p12'", "notasecret", X509KeyStorageFlags.Exportable)
        let packageName = "my Android package name"

		// to publish an alpha version: 
        PublishApk { AlphaSettings with Certificate = certificate; PackageName = packageName; AccountId = mail; Apk = apkPath; }

		// to publish a beta version: 
        // PublishApk { BetaSettings with Certificate = certificate; PackageName = packageName; AccountId = mail; Apk = apkPath; }

		// to publish a production version: 
        // PublishApk { ProductionSettings with Certificate = certificate; PackageName = packageName; AccountId = mail; Apk = apkPath; }
	)

	Target "android-build" (fun _ ->
		!! "**/my project Droid.csproj"
			|> MSBuildRelease androidBuildDir "Build"
			|> Log "BuildAndroidLib-Output: "
	)

	Target "Default" (fun _ ->
		trace "Building default target"
		RestorePackages()
	)

	"Clean"
		==> "android-package"
		==> "Default"

	RunTargetOrDefault "Default"

Default target will not start "publish" target because apps do not need to be updated too frequently (as explained here: https://developers.google.com/android-publisher/api_usage)

To publish your app, you can run

	PS> Fake.exe .\build.fsx "target=publish"

