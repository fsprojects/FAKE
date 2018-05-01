[<AutoOpen>]
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
/// Contains helper function which allow FAKE to interact with other applications via message files.
module Fake.MessageHelper

/// Waits for other applications to create a output files.
/// If the timeout is reached an exception will be raised.
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let WaitForMessageFiles files timeOut =
    let files = Seq.cache files
    tracefn "Waiting for message files %A (Timeout: %A)" files timeOut

    let time = 
        TaskRunnerHelper.waitFor 
            (fun _ -> allFilesExist files) 
            timeOut 
            100 
            (fun _ -> failwith "MessageFile timeout") 
    System.Threading.Thread.Sleep 100
    time
  
/// Waits for another application to create a output file.
/// If the timeout is reached an exception will be raised.
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let WaitForMessageFile file timeOut = WaitForMessageFiles [file] timeOut
