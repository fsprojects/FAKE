﻿/// Contains tasks which allow to run FSharp.Formatting for generating documentation.
[<System.Obsolete "use Fake.DotNet.FSFormatting instead">]
module Fake.FSharpFormatting

#nowarn "44"

/// Specifies the fsformatting executable
[<System.Obsolete "use Fake.DotNet.FSFormatting instead">]
let mutable toolPath =
    findToolInSubPath "fsformatting.exe" (currentDirectory @@ "tools" @@ "FSharp.Formatting.CommandTool" @@ "tools")

/// Runs fsformatting.exe with the given command in the given repository directory.
[<System.Obsolete "use Fake.DotNet.FSFormatting instead">]
let run command =
    if
        0
        <> ExecProcess
            (fun info ->
                info.FileName <- toolPath
                info.Arguments <- command)
            System.TimeSpan.MaxValue
    then
        failwithf "FSharp.Formatting %s failed." command

[<System.Obsolete "use Fake.DotNet.FSFormatting instead">]
let CreateDocs source outputDir template projectParameters =
    let command =
        projectParameters
        |> Seq.map (fun (k, v) -> [ k; v ])
        |> Seq.concat
        |> Seq.append (
            [ "literate"
              "--processdirectory"
              "--inputdirectory"
              source
              "--templatefile"
              template
              "--outputDirectory"
              outputDir
              "--replacements" ]
        )
        |> Seq.map (fun s -> if s.StartsWith "\"" then s else sprintf "\"%s\"" s)
        |> separated " "

    run command
    printfn "Successfully generated docs for %s" source

[<System.Obsolete "use Fake.DotNet.FSFormatting instead">]
let CreateDocsForDlls outputDir templatesDir projectParameters sourceRepo dllFiles =
    for file in dllFiles do
        projectParameters
        |> Seq.map (fun (k, v) -> [ k; v ])
        |> Seq.concat
        |> Seq.append (
            [ "metadataformat"
              "--generate"
              "--outdir"
              outputDir
              "--layoutroots"
              "./help/templates/"
              templatesDir
              "--sourceRepo"
              sourceRepo
              "--sourceFolder"
              currentDirectory
              "--parameters" ]
        )
        |> Seq.map (fun s -> if s.StartsWith "\"" then s else sprintf "\"%s\"" s)
        |> separated " "
        |> fun prefix -> sprintf "%s --dllfiles \"%s\"" prefix file
        |> run

        printfn "Successfully generated docs for DLL %s" file
