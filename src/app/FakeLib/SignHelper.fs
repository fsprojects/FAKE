[<AutoOpen>]
module Fake.SignHelper

open System.IO

/// <summary>Signs all files in filesToSign with the certification file certFile, protected with the password in the file passFile. 
///   The signtool will be search in the toolPath.</summary>
/// <user/>
let SignTool toolsPath certFile passFile filesToSign =
  if File.Exists certFile then
    let signPath = toolsPath @@ "signtool"
    let password = ReadLine passFile
    
    filesToSign
    |> Seq.iter(fun msiFile ->  
      let args = "sign /a /p " + password + " /f " + certFile + " " + msiFile
      let result =
        ExecProcess (fun info ->
          info.FileName <- signPath
          info.Arguments <- args) System.TimeSpan.MaxValue
      if result <> 0 then failwithf "Error during sign call " )
