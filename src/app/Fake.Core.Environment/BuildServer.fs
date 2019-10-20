/// Contains functions which allow build scripts to interact with a build server.
namespace Fake.Core

open System

/// The server type option.
type BuildServer =
    | TeamFoundation
    | TeamCity
    | CCNet
    | Jenkins
    | Travis
    | AppVeyor
    | GitLabCI
    | Bamboo
    | BitbucketPipelines
    | LocalBuild

/// The trace mode option.
type TraceMode =
    | Console
    | Xml

[<AbstractClass>]
type BuildServerInstaller () =
    abstract member Install : unit -> unit
    abstract member Detect : unit -> bool

[<RequireQualifiedAccess>]
module BuildServer =
    /// Defines if FAKE will use verbose tracing.
    /// This flag can be specified by setting the *verbose* build parameter.
    [<Obsolete "use 'Trace.isVerbose ()' and 'Trace.setVerbose true' instead">]
    let mutable verbose = Environment.hasEnvironVar "verbose"

    /// A constant label for local builds
    /// [omit]
    let localBuildLabel = "LocalBuild"

    /// Defines the XML output file - used for build servers like CruiseControl.NET.
    /// This output file can be specified by using the *logfile* build parameter.
    let mutable xmlOutputFile = Environment.environVarOrDefault "logfile" "./output/Results.xml"

    /// Build number retrieved from Bamboo
    /// [omit]
    let bambooBuildNumber = Environment.environVar "bamboo_buildNumber"

    /// Checks if we are on Bamboo
    /// [omit]
    let isBambooBuild =
        String.IsNullOrEmpty bambooBuildNumber |> not

    /// Checks if we are on Team Foundation
    /// [omit]
    let isTFBuild =
        let tfbuild = Environment.environVar "TF_BUILD"
        not (isNull tfbuild) && tfbuild.ToLowerInvariant() = "true"

    /// Build number retrieved from Team Foundation
    /// [omit]
    let tfBuildNumber = Environment.environVar "BUILD_BUILDNUMBER"

    /// Build number retrieved from TeamCity
    /// [omit]
    let tcBuildNumber = Environment.environVar "BUILD_NUMBER"

    /// Build number retrieved from Travis
    /// [omit]
    let travisBuildNumber = Environment.environVar "TRAVIS_BUILD_NUMBER"

    /// Checks if we are on GitLab CI
    /// [omit]
    let isGitlabCI = Environment.environVar "CI_SERVER_NAME" = "GitLab CI" || Environment.environVar "GITLAB_CI" = "true"

    /// Build number retrieved from GitLab CI
    /// [omit]
    let gitlabCIBuildNumber =
        if isGitlabCI then
            // https://github.com/fsharp/FAKE/issues/2290
            let s = Environment.environVar "CI_PIPELINE_ID"
            if String.IsNullOrEmpty s then
                let id = Environment.environVar "CI_BUILD_ID"
                if isNull id then "" else id
            else s
        else ""

    /// Build number retrieved from Jenkins
    /// [omit]
    let jenkinsBuildNumber = tcBuildNumber

    /// CruiseControl.NET Build label
    /// [omit]
    let ccBuildLabel = Environment.environVar "CCNETLABEL"

    /// AppVeyor build number
    /// [omit]
    let appVeyorBuildVersion = Environment.environVar "APPVEYOR_BUILD_VERSION"

    /// The current build server
    let buildServer =
        if Environment.hasEnvironVar "JENKINS_HOME" then Jenkins
        elif Environment.hasEnvironVar "TEAMCITY_VERSION" then TeamCity
        elif not (String.IsNullOrEmpty ccBuildLabel) then CCNet
        elif not (String.IsNullOrEmpty travisBuildNumber) then Travis
        elif not (String.IsNullOrEmpty appVeyorBuildVersion) then AppVeyor
        elif isGitlabCI then GitLabCI
        elif isTFBuild then TeamFoundation
        elif isBambooBuild then Bamboo
        elif Environment.hasEnvironVar "BITBUCKET_COMMIT" then BitbucketPipelines
        else LocalBuild

    /// The current build version as detected from the current build server.
    let buildVersion =
        let getVersion = Environment.environVarOrDefault "buildVersion"
        match buildServer with
        | Jenkins -> getVersion jenkinsBuildNumber
        | TeamCity -> getVersion tcBuildNumber
        | CCNet -> getVersion ccBuildLabel
        | Travis -> getVersion travisBuildNumber
        | AppVeyor -> getVersion appVeyorBuildVersion
        | GitLabCI -> getVersion gitlabCIBuildNumber
        | TeamFoundation -> getVersion tfBuildNumber
        | Bamboo -> getVersion bambooBuildNumber
        | LocalBuild -> getVersion localBuildLabel
        | BitbucketPipelines -> getVersion ""

    /// Is true when the current build is a local build.
    let isLocalBuild = LocalBuild = buildServer

    let mutable ansiColorSupport =
        match buildServer with
        | Jenkins -> false
        | TeamCity -> true
        | CCNet -> false
        | Travis -> true
        | AppVeyor -> true
        | GitLabCI -> true
        | TeamFoundation -> false
        | Bamboo -> false
        | BitbucketPipelines -> false
        | LocalBuild -> false


    let install (servers: BuildServerInstaller list) =
        servers
        |> List.iter (fun f ->
            if f.Detect() then
                f.Install())
