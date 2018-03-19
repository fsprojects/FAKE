[<AutoOpen>]
/// Contains functions which allow build scripts to interact with a build server.
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
module Fake.BuildServerHelper

/// The server type option.
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
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
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
type TraceMode = 
    | Console
    | Xml

/// Defines if FAKE will use verbose tracing.
/// This flag can be specified by setting the *verbose* build parameter.
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let mutable verbose = hasBuildParam "verbose"

/// A constant label for local builds
/// [omit]         
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]   
let localBuildLabel = "LocalBuild"

/// Defines the XML output file - used for build servers like CruiseControl.NET.
/// This output file can be specified by using the *logfile* build parameter.
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let mutable xmlOutputFile = getBuildParamOrDefault "logfile" "./output/Results.xml"

/// Build number retrieved from Bamboo
/// [omit]
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let bambooBuildNumber = environVar "bamboo_buildNumber"

/// Checks if we are on Bamboo
/// [omit]
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let isBambooBuild =
    isNotNullOrEmpty bambooBuildNumber

/// Checks if we are on Team Foundation
/// [omit]
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let isTFBuild =
    let tfbuild = environVar "TF_BUILD"
    tfbuild <> null && tfbuild.ToLowerInvariant() = "true"

/// Build number retrieved from Team Foundation
/// [omit]
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let tfBuildNumber = environVar "BUILD_BUILDNUMBER"

/// Build number retrieved from TeamCity
/// [omit]
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let tcBuildNumber = environVar "BUILD_NUMBER"

/// Build number retrieved from Travis
/// [omit]
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let travisBuildNumber = environVar "TRAVIS_BUILD_NUMBER"

/// Checks if we are on GitLab CI
/// [omit]
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let isGitlabCI = getEnvironmentVarAsBool "GITLAB_CI" || environVar "CI_SERVER_NAME" = "GitLab CI"

/// Build number retrieved from GitLab CI
/// [omit]
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let gitlabCIBuildNumber = if isGitlabCI then environVar "CI_BUILD_ID" else ""

/// Build number retrieved from Jenkins
/// [omit]
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let jenkinsBuildNumber = tcBuildNumber

/// CruiseControl.NET Build label
/// [omit]
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let ccBuildLabel = environVar "CCNETLABEL"

/// AppVeyor build number
/// [omit]
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let appVeyorBuildVersion = environVar "APPVEYOR_BUILD_VERSION"

/// The current build server
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let buildServer = 
    if hasBuildParam "JENKINS_HOME" then Jenkins
    elif hasBuildParam "TEAMCITY_VERSION" then TeamCity
    elif not (isNullOrEmpty ccBuildLabel) then CCNet
    elif not (isNullOrEmpty travisBuildNumber) then Travis
    elif not (isNullOrEmpty appVeyorBuildVersion) then AppVeyor
    elif isGitlabCI then GitLabCI
    elif isTFBuild then TeamFoundation
    elif isBambooBuild then Bamboo
    elif hasBuildParam "BITBUCKET_COMMIT" then BitbucketPipelines
    else LocalBuild

/// The current build version as detected from the current build server.
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let buildVersion = 
    let getVersion = getBuildParamOrDefault "buildVersion"
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
[<System.Obsolete("Use Fake.Core.Environment instead (FAKE0001 - package: Fake.Core.Environment)")>]
let isLocalBuild = LocalBuild = buildServer
