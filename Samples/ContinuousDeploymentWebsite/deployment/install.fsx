// include Fake libs
#I @"tools\FAKE\"
#r "FakeLib.dll"

open Fake

let targetWebsitePath = @"..\website\"

// Targets
Target "StopCassini" (fun _ ->
    killProcess "CassiniDev4"
)

Target "InstallWebsite" (fun _ ->
    XCopy @".\website\" targetWebsitePath
)


Target "StartCassini" (fun _ ->
    let args = "/a:" + @".\website"

    StartProcess
       (fun info ->  
           info.FileName <- @".\tools\cassini\CassiniDev4.exe"
           info.WorkingDirectory <- null
           info.Arguments <- args)
)

"StopCassini"
  ==> "InstallWebsite"
  ==> "StartCassini"
 
// start build
Run "StartCassini"