/// This module contains helper function for the ngen.exe
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module Fake.NGenHelper

open System

/// NGen parameters
[<CLIMutable>]
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type NGenParams = 
    { /// (Required) Path to the NGenutil
      ToolPath : string
      /// The timeout for the process.
      TimeOut : TimeSpan
      /// The directory where the process will be started.
      WorkingDir : string }

let private winDir = Environment.GetFolderPath Environment.SpecialFolder.Windows
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let mutable NGen32 = winDir @@ "Microsoft.NET/Framework/v4.0.30319/ngen.exe"
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let mutable NGen64 = winDir @@ "Microsoft.NET/Framework64/v4.0.30319/ngen.exe"
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let UseNGen64 p : NGenParams = { p with ToolPath = NGen64 }

/// NGen default parameters
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let NGenDefaults = 
    { ToolPath = NGen32
      TimeOut = TimeSpan.FromMinutes 5.
      WorkingDir = currentDirectory }

let private ngen param command = 
    let ok = 
        execProcess (fun info -> 
            info.FileName <- param.ToolPath
            if param.WorkingDir <> String.Empty then info.WorkingDirectory <- param.WorkingDir
            info.Arguments <- command) param.TimeOut
    if not ok then failwith "NGen reported errors."

/// Runs ngen.exe with the given command.
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let NGen setParams command = 
    let taskName = "NGen"
    use __ = traceStartTaskUsing taskName command
    ngen (setParams NGenDefaults) command

/// Runs ngen.exe install on given assemblies.
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let Install setParams assemblies = 
    let taskName = "NGen Install"
    use __ = traceStartTaskUsing taskName ""
    let param = setParams NGenDefaults
    match assemblies |> Seq.toList with
    | [] -> ()
    | [ assembly ] -> ngen param (sprintf "install \"%s\" /nologo" assembly)
    | assemblies -> 
        for assembly in assemblies do
            ngen param (sprintf "install \"%s\" /queue:1 /nologo" assembly)
        ngen param "executeQueuedItems 1 /nologo"
