# Publish Android apk

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE.exe before version 5 (or the non-netcore version). The documentation needs te be updated, please help!</p>
</div>

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
							Properties = ["MSBuild property", "MSBuild property value"]
                        })

        |> AndroidSignAndAlign (fun defaults ->
            { defaults with 
                KeystorePath = @"path to my file.keystore"
                KeystorePassword = "my password"
                KeystoreAlias = "my key alias"
            })
        |> fun file -> file.CopyTo(Path.Combine(androidProdDir, file.Name)) |> ignore
    )

	// You can also build one APK per ABI
	Target "Android-MultiPackages" (fun () ->
		let versionStepper = (fun v t -> match t with
										 | AndroidAbiTarget.X86 c -> v + 1
										 | AndroidAbiTarget.X86And64 c -> v + 2
										 | AndroidAbiTarget.ArmEabi c -> v + 3
										 | AndroidAbiTarget.ArmEabiV7a c -> v + 4
										 | AndroidAbiTarget.Arm64V8a c -> v + 5
										 | _ -> v)
		let abis = AndroidPackageAbiParam.SpecificAbis
						([ AndroidAbiTarget.X86({ SuffixAndExtension="-x86.apk"; })
						   AndroidAbiTarget.ArmEabi({ SuffixAndExtension="-armeabi.apk"; })
						   AndroidAbiTarget.ArmEabiV7a({ SuffixAndExtension="-armeabi-v7a.apk"; })
						   AndroidAbiTarget.X86And64({ SuffixAndExtension="-x86_64.apk"; })
						 ])
		let files = AndroidBuildPackages(fun defaults ->
								{ defaults with 
									ProjectPath = "Path to my project Droid.csproj"
									Configuration = "Release"
									OutputPath = androidBuildDir
									PackageAbiTargets = abis
									VersionStepper = Some(versionStepper)
								})

		for f in files do
			printfn "- apk: %s" f.Name

		files 
		|> Seq.iter (fun file -> file.CopyTo(Path.Combine(androidProdDir, file.Name)) |> ignore)
	)


    Target "Publish" (fun _ -> 
        // I like verbose script
        trace "publishing Android App"
        let apk = androidProdDir 
                        |> directoryInfo 
                        |> filesInDir 
                        |> Seq.filter(fun f -> f.Name.EndsWith(".apk"))
                        |> Seq.exactlyOne
        let apkPath = apk.FullName
        tracefn "Apk found: %s" apkPath
        let mail = "my service account mail@developer.gserviceaccount.com"
        // Path to the certificate file probably named 'Google Play Android Developer-xxxxxxxxxxxx.p12'
        let certificate = new X509Certificate2
                                    (
                                        @"Google Play Android Developer-xxxxxxxxxxxx.p12",
                                        "notasecret",
                                        X509KeyStorageFlags.Exportable
                                    )
        let packageName = "my Android package name"

        // to publish an alpha version: 
        PublishApk 
            { AlphaSettings with 
                Config = 
                    { 
                        Certificate = certificate;
                        PackageName = packageName;
                        AccountId = mail;
                        Apk = apkPath; 
                    }
            }

        // to publish a beta version: 
        //
        //PublishApk 
        //    { BetaSettings with 
        //        Config = 
        //            { 
        //                Certificate = certificate;
        //                PackageName = packageName;
        //                AccountId = mail;
        //                Apk = apkPath; 
        //            }
        //    }
        
        // to publish a production version: 
        //
        //PublishApk 
        //    { ProductionSettings with 
        //        Config = 
        //            { 
        //                Certificate = certificate;
        //                PackageName = packageName;
        //                AccountId = mail;
        //                Apk = apkPath; 
        //            }
        //    }
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
