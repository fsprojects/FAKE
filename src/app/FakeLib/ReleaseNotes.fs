namespace Fake

open System
open System.IO
open System.Text.RegularExpressions

type ReleaseNotes =
    { AssemblyVersion: string
      NugetVersion: string
      Notes: string list }
    override x.ToString() = sprintf "%A" x

[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module ReleaseNotes =
    let nugetRegex = Regex (@"([0-9]+.)+[0-9]+(-[a-zA-Z]+)?")
    let assemblyRegex = Regex (@"([0-9]+.)+[0-9]+")

    /// Parse simple release notes sequence
    let parseSimpleReleaseNotes (text: seq<string>) = 
        let lastLine = text |> Seq.last
        let assemblyVersion, nugetVersion = assemblyRegex.Match (lastLine), nugetRegex.Match (lastLine)
        if not assemblyVersion.Success
        then failwith "Unable to parse valid Assembly version from release notes."
        let notes = lastLine.Substring(nugetVersion.Index + nugetVersion.Length).Trim ([|' '; '-'|])
        { AssemblyVersion = assemblyVersion.Value
          NugetVersion = nugetVersion.Value
          Notes = [notes] }

    /// Parse "complex" release notes text sequence
    let parseComplexReleaseNotes (text: seq<string>) = 
        let indexOfLastBlock = 
            text
            |> Seq.fold (fun (index, ctr) str -> 
                       if String.IsNullOrEmpty (str)
                       then (index, ctr + 1)
                       elif str.Trim().[0] = '#'
                       then (index + ctr, 1)
                       else (index, ctr + 1)) (0, 0)
            |> fst
        let lastBlock = text |> Seq.skip indexOfLastBlock
        let blockHeader = lastBlock |> Seq.head
        let notes = 
            lastBlock
            |> Seq.skip 1
            |> Seq.map (fun str -> str.Trim ([|' '; '*'|]))
            |> Seq.toList
        let assemblyVersion, nugetVersion = assemblyRegex.Match (blockHeader), nugetRegex.Match (blockHeader)
        if not assemblyVersion.Success
        then failwith "Unable to parse valid Assembly version from release notes."
        { AssemblyVersion = assemblyVersion.Value
          NugetVersion = nugetVersion.Value
          Notes = notes }

    /// Parse a Release Notes File - Either simple or "complex" format
    /// See: https://github.com/fsharp/FAKE/issues/171
    /// <param name="filePath">The path to the release notes file</param>
    /// <returns> (assembly_version, nuget_version, [release_notes]) </returns>
    let parseReleaseNotes (data: seq<string>) = 
        let text = 
            let num_real_lines = 
                data
                |> Seq.fold (fun (line_count, blank_count) str -> 
                           if String.IsNullOrEmpty (str.Trim ())
                           then line_count, blank_count + 1
                           else line_count + 1 + blank_count, 0) (0, 0)
                |> fst
            data
            |> Seq.take num_real_lines
            |> Seq.skipWhile (fun str -> String.IsNullOrWhiteSpace (str.Trim ()))
        if text |> Seq.isEmpty
        then failwith "Empty Realease file."
        let (|Simple|Complex|Invalid|) c = 
            match c with
            | '*' -> Simple
            | '#' -> Complex
            | _ -> Invalid
        let firstNonEmptyChar = (text |> Seq.head).Trim([|'-'; ' '|]).[0]
        match firstNonEmptyChar with
        | Simple -> parseSimpleReleaseNotes text
        | Complex -> parseComplexReleaseNotes text
        | Invalid -> failwith "Invalid Release Notes format."