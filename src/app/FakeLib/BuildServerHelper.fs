[<AutoOpen>]
/// Contains functions which allow build scripts to interact with a build server.
module Fake.BuildServerHelper

/// The server type option.
type BuildServer =
| TeamCity
| CCNet
| Jenkins
| LocalBuild

/// The trace mode option.
type TraceMode =
| Console
| Xml

/// Defines if FAKE will use verbose tracing.
/// This flage can be specified by setting the *verbose* build parameter.
let mutable verbose = hasBuildParam "verbose"

/// A constant label for local builds
/// [omit]            
let localBuildLabel = "LocalBuild"

/// Defines the XML output file - used for build servers like CruiseControl.NET.
/// This output file can be specified by using the *logfile* build parameter.
let mutable xmlOutputFile = getBuildParamOrDefault "logfile" "./output/Results.xml"

/// Build number retrieved from TeamCity
/// [omit]
let tcBuildNumber = environVar "BUILD_NUMBER"

/// Build number retrieved from Jenkins
/// [omit]
let jenkinsBuildNumber = tcBuildNumber

/// CruiseControl.NET Build label
/// [omit]
let ccBuildLabel = environVar "CCNETLABEL"

/// Determines the current BuildVersion and if it is a local build
let buildVersion,buildServer =
    let getVersion = getBuildParamOrDefault "buildVersion"
    if hasBuildParam "jenkins_home" then getVersion jenkinsBuildNumber,Jenkins else
    if not (isNullOrEmpty tcBuildNumber) then getVersion tcBuildNumber,TeamCity else
    if not (isNullOrEmpty ccBuildLabel) then getVersion ccBuildLabel,CCNet else 
    getVersion localBuildLabel,LocalBuild

/// Determines if the current build is a local build.
let isLocalBuild = LocalBuild = buildServer