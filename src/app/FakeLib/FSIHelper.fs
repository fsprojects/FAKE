[<AutoOpen>]
module Fake.FSIHelper
   
/// The Path to the F# interactive tool
let fsiPath = 
    let ev = environVar "FSI"
    if not (isNullOrEmpty ev) then ev else 
    if isUnix then
        let paths = appSettings "FSIPath"
        // The standard name on *nix is "fsharpi"
        match tryFindFile paths "fsharpi" with
        | Some file -> file
        | None -> 
        // The early F# 2.0 name on *nix was "fsi"
        match tryFindFile paths "fsi" with
        | Some file -> file
        | None -> "fsharpi" 
    else
        if System.IO.File.Exists("fsi.exe") then "fsi.exe" else
        findPath "FSIPath" "fsi.exe"
      
/// Run the given buildscript with fsi.exe
let runBuildScriptAt workingDirectory printDetails script args =
    if printDetails then traceFAKE "Running Buildscript: %s" script
    
    let result = execProcess (fun info ->  
        info.FileName <- fsiPath
        info.Arguments <- script
        info.WorkingDirectory <- workingDirectory
            
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

let runBuildScript printDetails script args =
    runBuildScriptAt "" printDetails script args
    
