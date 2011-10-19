[<AutoOpen>]
module Fake.FSIHelper
   
/// The Path to the F# interactive tool
let fsiPath = 
    if isUnix then
        "fsi"
    else
        let ev = environVar "FSI"
        if not (isNullOrEmpty ev) then ev else findPath "FSIPath" "fsi.exe"
      
/// Run the given buildscript with fsi.exe
let runBuildScript printDetails script args = 
    if printDetails then traceFAKE "Running Buildscript: %s" script
    
    let result = execProcess (fun info ->  
        info.FileName <- fsiPath
        info.Arguments <- script     
            
        let setVar (k,v) =
            if info.EnvironmentVariables.ContainsKey k then
                info.EnvironmentVariables.[k] <- v
            else 
                info.EnvironmentVariables.Add(k,v)

        args |> Seq.iter setVar

        setVar("MSBuild",msBuildExe)
        setVar("GIT",Git.CommandHelper.gitPath)
        setVar("FSI",fsiPath)) System.TimeSpan.MaxValue
    
    System.Threading.Thread.Sleep 1000
    result
