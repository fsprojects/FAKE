# Fake.BuildServer

This namespace bundles support for various Build-Servers.

Supported Build-Server (Note: Not supported doesn't mean that it won't work, but colors and deep integration might be missing):

- `Fake.BuildServer.AppVeyor`
- `Fake.BuildServer.Travis`
- `Fake.BuildServer.TeamCity` - [legacy documentation](legacy-teamcity.html)
- `Fake.BuildServer.TeamFoundation`

## General API usage

Generally there is no need to write a script with a particular build server in mind. The usual integration works by pulling the Fake 5 module and calling `BuildServer.Install [ Server.Installer ]`.
By using the `Fake.Core.Trace`-Api Fake will use all available build-server features and provide the best integration into the platform. If you feel like we missed some feature on your CI/CD-server please let us know and open an issue. Or consider sending a pull request.

Example:

```fsharp

#r "paket:
nuget Fake.BuildServer.TeamCity
nuget Fake.BuildServer.TeamFoundation
nuget Fake.Core.Target //"

open System.IO
open Fake.Core
open Fake.BuildServer

BuildServer.install [
    TeamCity.Installer
    TeamFoundation.Installer
]

// If you additionally want output in the console, even on the build-server (otherwise remove this line).
CoreTracing.ensureConsoleListener ()

Target.create "Test" (fun _ ->
    File.WriteAllText("myfile.txt", "some content")

    // traceTag can be used to open scopes/blocks. They will be shown in the build-server visualization supported.
    ( use testsuite = Trace.traceTag (KnownTags.TestSuite "some-testsuite") "Starting unit test"
      ( use _ = Trace.traceTag (KnownTags.Test "some-test") "Starting unit test 1"
        // Scope of the test
        Trace.testOutput "some-test" "standard output" "standard error")
      ( use _ = Trace.traceTag (KnownTags.Test "some-test2") "Starting unit test 2"
        // Scope of test2
        Trace.testOutput "some-test2" "standard output" "standard error"))

    // Uploads an artifact no matter the build-server
    Trace.publish ImportData.BuildArtifact "myfile.txt"

    Trace.setBuildNumber "my-build-number"

    Trace.traceImportant "tries to write in yellow"

    Trace.trace "tries to write in green"

    Trace.log "tries to write in white (normal color)"
)

```

## Implementing support for a build-server

Please look at the existing implementations.
