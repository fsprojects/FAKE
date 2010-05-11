[<AutoOpen>]
module Fake.BuildServerHelper

/// The BuildServer type.
type BuildServer =
| TeamCity
| CCNet       
| LocalBuild

/// The trace Mode type.
type TraceMode =
| Console
| Xml

/// A constant for local builds            
let localBuildLabel = "LocalBuild"

/// Definces the XML output file
/// Used for BuildServers like CruiseControl.NET
let mutable xmlOutputFile = getBuildParamOrDefault "xmloutput" @".\output\Results.xml"

/// Build number retrieved from TeamCity
let tcBuildNumber = environVar "BUILD_NUMBER" 

/// CruiseControl.NET Build label
let ccBuildLabel = environVar "CCNETLABEL"

/// Determines the current BuildVersion and if it is a local build
let buildVersion,buildServer =     
    if not (isNullOrEmpty tcBuildNumber) then tcBuildNumber,TeamCity else
    if not (isNullOrEmpty ccBuildLabel) then ccBuildLabel,CCNet else 
    localBuildLabel,LocalBuild

/// Determines if the current build is a local build.
let isLocalBuild = LocalBuild = buildServer

/// The actual trace mode.
let mutable traceMode = 
    match buildServer with
    | TeamCity   -> Console
    | CCNet      -> Xml
    | LocalBuild -> Console