[<AutoOpen>]
module Fake.BuildServerHelper

type BuildServer =
| TeamCity
| CCNet       
            
let localBuildLabel = "LocalBuild"
let mutable xmlOutputFile = 
    if hasBuildParam "xmloutput" then
        getBuildParam "xmloutput"
    else
        @".\output\Results.xml"

let tcBuildNumber = environVar "BUILD_NUMBER" 

/// Determines the current BuildVersion and if it is a local build
let buildVersion,isLocalBuild,buildServer =     
  if tcBuildNumber <> "" && tcBuildNumber <> null then tcBuildNumber,false,Some TeamCity else
  if ccBuildLabel <> "" && ccBuildLabel <> null then ccBuildLabel,false,Some CCNet else 
  localBuildLabel,true,None