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
    { Program          = @".\tools\cassini\CassiniDev4.exe"
      WorkingDirectory = "."
      CommandLine      = ""
      Args             = ["/a:",@".\website"]}
        |> shellExec
        |> ignore
)

"StopCassini"
  ==> "InstallWebsite"
  ==> "StartCassini"
 
// start build
Run "StartCassini"