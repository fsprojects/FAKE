/// Contains tasks which allow to run FSharp.Formatting for generating documentation.
module Fake.FSharpFormatting

/// Specifies the fsformatting executable
let mutable fsformattingPath = findToolInSubPath "fsformatting.exe" (currentDirectory @@ "tools" @@ "fsformatting")
    
/// Specifies a global timeout for fsformatting.exe
let mutable fsformattingTimeOut = System.TimeSpan.MaxValue

/// Runs fsformatting.exe with the given command in the given repository directory.
let internal runFSFormattingCommand command =
    if 0 <> ExecProcess (fun info ->  
        info.FileName <- fsformattingPath
        info.Arguments <- command) fsformattingTimeOut
    then
        failwithf "FSharp.Formatting %s failed." command

let CreateDocs source outputDir template projectParameters =    
    let command =
        projectParameters 
        |> Seq.map (fun (k,v) -> [k;v]) 
        |> Seq.concat
        |> Seq.append (
            [ "literate";
              "--processdirectory";
              "--inputdirectory"; source
              "--templatefile"; template
              "--outputDirectory"; outputDir
              "--replacements" ])
        |> Seq.map (fun s -> if s.StartsWith "\"" then s else sprintf "\"%s\"" s)
        |> separated " " 

    runFSFormattingCommand command
    printfn "Successfully generated docs for %s" source
           
let CreateDocsForDlls workingDir outputDir templatesDir projectParameters dllFiles = 
    let command =
        projectParameters 
        |> Seq.map (fun (k,v) -> [k;v])
        |> Seq.concat
        |> Seq.append (
            [ "metadataformat"
              "--generate";
              "--outdir"; outputDir
              "--layoutroots"; 
              "./help/templates/"; templatesDir;
              "--parameters" ])
        |> Seq.map (fun s -> if s.StartsWith "\"" then s else sprintf "\"%s\"" s)
        |> separated " " 

    for file in dllFiles do 
        let command = command + sprintf " --dllfiles \"%s\"" file
                
        runFSFormattingCommand command
        printfn "Successfully generated docs for DLL %s" file