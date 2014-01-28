/// Contains tasks which allow to run FSharp.Formatting for generating documentation.
module Fake.FSharpFormatting

/// Specifies the fsformatting executable
let mutable toolPath = findToolInSubPath "fsformatting.exe" (currentDirectory @@ "tools" @@ "fsformatting")
    
/// Runs fsformatting.exe with the given command in the given repository directory.
let run command =
    if 0 <> ExecProcess (fun info ->  
        info.FileName <- toolPath
        info.Arguments <- command) System.TimeSpan.MaxValue
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
              "--includesource" // TODO:         
              "--replacements" ])
        |> Seq.map (fun s -> if s.StartsWith "\"" then s else sprintf "\"%s\"" s)
        |> separated " " 

    run command
    printfn "Successfully generated docs for %s" source
           
let CreateDocsForDlls outputDir templatesDir projectParameters dllFiles = 
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
              "--includesource" // TODO:
              "--parameters" ])
        |> Seq.map (fun s -> if s.StartsWith "\"" then s else sprintf "\"%s\"" s)
        |> separated " " 

    for file in dllFiles do 
        let command = command + sprintf " --dllfiles \"%s\"" file
                
        run command
        printfn "Successfully generated docs for DLL %s" file