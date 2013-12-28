[<AutoOpen>]
/// Contains tasks which allow to run FSharp.Formatting for generating documentation.
module Fake.FSFHelper

/// Specifies the fsformatting executable
let mutable fsformattingPath = "./tools/fsformatting/fsformatting.exe"

/// Specifies a global timeout for fsformatting.exe
let mutable fsformattingTimeOut = System.TimeSpan.MaxValue

/// Runs fsformatting.exe with the given command in the given repository directory.
let runFSFormattingCommand workingDir command = 
    let processResult = 
        ExecProcessAndReturnMessages (fun info ->  
            info.FileName <- fsformattingPath
            info.WorkingDirectory <- workingDir
            info.Arguments <- command) fsformattingTimeOut
    processResult.OK,processResult.Messages,toLines processResult.Errors

