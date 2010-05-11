[<AutoOpen>]
module Fake.FSIHelper
   
let fsiPath = 
    let ev = environVar "FSI"
    if not (isNullOrEmpty ev) then ev else findPath "FSIPath" "fsi.exe"
      
/// Run the buildscript with fsi
let runBuildScript script args = 
    traceFAKE "Running Buildscript: %s" script
  
    let result = execProcess (fun info ->  
      info.FileName <- fsiPath
      info.Arguments <- script           
      Seq.iter info.EnvironmentVariables.Add args      
      if not <| info.EnvironmentVariables.ContainsKey "MSBuild" then
          info.EnvironmentVariables.Add("MSBuild", msBuildExe)
      if not <| info.EnvironmentVariables.ContainsKey "FSI" then
          info.EnvironmentVariables.Add("FSI", fsiPath) )
    
    System.Threading.Thread.Sleep 1000
    result