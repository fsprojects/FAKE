[<AutoOpen>]
module Fake.BuildServerHelper

/// The BuildServer type.
type BuildServer =
| TeamCity
| CCNet
| Jenkins
| LocalBuild

/// The trace Mode type.
type TraceMode =
| Console
| Xml

/// Trace verbose output
let mutable verbose = hasBuildParam "verbose"

/// A constant for local builds            
let localBuildLabel = "LocalBuild"

/// Definces the XML output file
/// Used for BuildServers like CruiseControl.NET
let mutable xmlOutputFile = getBuildParamOrDefault "logfile" @".\output\Results.xml"

/// Build number retrieved from TeamCity
let tcBuildNumber = environVar "BUILD_NUMBER"

/// Build number retrieved from Jenkins
let jenkinsBuildNumber = tcBuildNumber

/// CruiseControl.NET Build label
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