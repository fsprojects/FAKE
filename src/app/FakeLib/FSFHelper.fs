/// Contains tasks which allow to run FSharp.Formatting for generating documentation.
module Fake.FSFHelper

/// Specifies the fsformatting executable
let mutable fsformattingPath = findToolInSubPath "fsformatting.exe" (currentDirectory @@ "tools" @@ "fsformatting")
    
/// Specifies a global timeout for fsformatting.exe
let mutable fsformattingTimeOut = System.TimeSpan.MaxValue

/// Runs fsformatting.exe with the given command in the given repository directory.
let runFSFormattingCommand workingDir command =
    if 0 <> ExecProcess (fun info ->  
        info.FileName <- fsformattingPath
        info.WorkingDirectory <- workingDir
        info.Arguments <- command) fsformattingTimeOut
    then
        failwithf "FSharp.Formatting %s failed." command
