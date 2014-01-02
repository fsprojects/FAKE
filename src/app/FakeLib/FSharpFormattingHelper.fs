/// Contains tasks which allow to run FSharp.Formatting for generating documentation.
module Fake.FSFHelper

/// Specifies the fsformatting executable
let mutable fsformattingPath = findToolInSubPath "fsformatting.exe" (currentDirectory @@ "tools" @@ "fsformatting")
    
/// Specifies a global timeout for fsformatting.exe
let mutable fsformattingTimeOut = System.TimeSpan.MaxValue

/// Runs fsformatting.exe with the given command in the given repository directory.
let runFSFormattingCommand workingDir quiet command =
    let redirectOutputToTrace_ = redirectOutputToTrace
    redirectOutputToTrace <- quiet
    let ret = ExecProcess (fun info ->  
        info.FileName <- fsformattingPath
        info.WorkingDirectory <- workingDir
        info.Arguments <- command) fsformattingTimeOut
    redirectOutputToTrace <- redirectOutputToTrace_
    if ret <> 0
    then
        failwithf "FSharp.Formatting %s failed." command
   
let CreateDocsForDlls workingDir quiet command dllFiles =
    for file in dllFiles do 
        let command = command + sprintf "--dllfiles %s" file
                
        runFSFormattingCommand "." quiet command
        printfn "Successfully generated doc for DLL %s" file