/// Contains functions which allow build scripts to interact with a build server.
module Fake.Core.BuildServer
open Fake.Core.Environment
open Fake.Core.String

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

/// Defines if FAKE will use verbose tracing.
/// This flag can be specified by setting the *verbose* build parameter.
let mutable verbose = hasEnvironVar "verbose"

/// A constant label for local builds
/// [omit]            
let localBuildLabel = "LocalBuild"

/// Defines the XML output file - used for build servers like CruiseControl.NET.
/// This output file can be specified by using the *logfile* build parameter.
let mutable xmlOutputFile = environVarOrDefault "logfile" "./output/Results.xml"

/// Build number retrieved from Bamboo
/// [omit]
let bambooBuildNumber = environVar "bamboo_buildNumber"

/// Checks if we are on Bamboo
/// [omit]
let isBambooBuild =
    isNotNullOrEmpty bambooBuildNumber

/// Checks if we are on Team Foundation
/// [omit]
let isTFBuild =
    let tfbuild = environVar "TF_BUILD"
    not (isNull tfbuild) && tfbuild.ToLowerInvariant() = "true"

/// Build number retrieved from Team Foundation
/// [omit]
let tfBuildNumber = environVar "BUILD_BUILDNUMBER"

/// Build number retrieved from TeamCity
/// [omit]
let tcBuildNumber = environVar "BUILD_NUMBER"

/// Build number retrieved from Travis
/// [omit]
let travisBuildNumber = environVar "TRAVIS_BUILD_NUMBER"

/// Checks if we are on GitLab CI
/// [omit]
let isGitlabCI = environVar "CI_SERVER_NAME" = "GitLab CI"

/// Build number retrieved from GitLab CI
/// [omit]
let gitlabCIBuildNumber = if isGitlabCI then environVar "CI_BUILD_ID" else ""

/// Build number retrieved from Jenkins
/// [omit]
let jenkinsBuildNumber = tcBuildNumber

/// CruiseControl.NET Build label
/// [omit]
let ccBuildLabel = environVar "CCNETLABEL"

/// AppVeyor build number
/// [omit]
let appVeyorBuildVersion = environVar "APPVEYOR_BUILD_VERSION"

/// The current build server
let buildServer = 
    if hasEnvironVar "JENKINS_HOME" then Jenkins
    elif hasEnvironVar "TEAMCITY_VERSION" then TeamCity
    elif not (isNullOrEmpty ccBuildLabel) then CCNet
    elif not (isNullOrEmpty travisBuildNumber) then Travis
    elif not (isNullOrEmpty appVeyorBuildVersion) then AppVeyor
    elif isGitlabCI then GitLabCI
    elif isTFBuild then TeamFoundation
    elif isBambooBuild then Bamboo
    elif hasEnvironVar "BITBUCKET_COMMIT" then BitbucketPipelines
    else LocalBuild

/// The current build version as detected from the current build server.
let buildVersion = 
    let getVersion = environVarOrDefault "buildVersion"
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
