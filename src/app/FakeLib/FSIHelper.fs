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
      args
        |> Seq.iter (fun (k,v) -> setEnvironVar k v)

      setEnvironVar "MSBuild" msBuildExe
      setEnvironVar "FSI" fsiPath) System.TimeSpan.MaxValue
    
    System.Threading.Thread.Sleep 1000
    result