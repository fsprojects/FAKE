// include Fake libs
#I @"tools\FAKE\"
#r "FakeLib.dll"

open Fake

let targetWebsitePath = @"..\website\"
let targetServerPath = @"..\server\"

// Targets
Target "StopCassini" (fun _ ->
    killProcess "CassiniDev4"
)

Target "InstallWebsite" (fun _ ->
    XCopy @".\website\" targetWebsitePath
    XCopy @".\tools\cassini\" targetServerPath
)

Target "StartCassini" (fun _ ->
    let args = "/a:" + targetWebsitePath

    StartProcess
       (fun info ->  
           info.FileName <- targetServerPath @@ "CassiniDev4.exe"
           info.WorkingDirectory <- null
           info.Arguments <- args)
)

"StopCassini"
  ==> "InstallWebsite"
  ==> "StartCassini"
 
// start build
Run "StartCassini"