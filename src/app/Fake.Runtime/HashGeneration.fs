/// Contains helper functions which allow to interact with the F# Interactive.
module Fake.Runtime.HashGeneration

open System
open System.IO
open System.Text.RegularExpressions
open System.Xml.Linq
open Yaaf.FSharp.Scripting
open Fake.Runtime


type Script = {
    HashContent : string
    Location : string
}

let getAllScriptContents (pathsAndContents : seq<Script>) =
    pathsAndContents |> Seq.map(fun s -> s.HashContent)

let getAllScripts defines scriptPath : Script list =
    let rec getAllScriptsRec scriptPath parentIncludes : Script list =
        let scriptContents =
          File.ReadLines scriptPath
          |> FSharpParser.getTokenized scriptPath defines
        //let searchPaths = getSearchPaths scriptContents |> Seq.toList
        let resolvePath currentIncludes currentDir relativeOrAbsolute isDir =
            let possiblePaths =
              if Path.IsPathRooted relativeOrAbsolute then [ relativeOrAbsolute ]
              else
                currentDir :: currentIncludes
                |> List.map (fun bas -> Path.Combine(bas, relativeOrAbsolute))
            let realPath =
              match possiblePaths |> Seq.tryFind (if isDir then Directory.Exists else File.Exists) with
              | Some f -> f
              | None ->
                failwithf "FAKE-CACHING: Could not find %s '%s' in any paths searched. Searched paths:\n%A" (if isDir then "directory" else "file") relativeOrAbsolute (currentDir :: currentIncludes)
            realPath

        let loadedContents =
            scriptContents
            |> FSharpParser.findProcessorDirectives
            |> List.fold (fun ((currentIncludes, currentDir, childScripts) as state) preprocessorDirective ->
                let (|MatchFirstString|_|) (l:FSharpParser.StringLike list) =
                  match l with
                  | FSharpParser.StringLike.StringKeyword FSharpParser.SourceDirectory :: _ ->
                    Some (Path.GetDirectoryName scriptPath)
                  | FSharpParser.StringLike.StringKeyword FSharpParser.SourceFile :: _ ->
                    Some (Path.GetFileName scriptPath)
                  | FSharpParser.StringLike.StringKeyword (FSharpParser.Unknown s) :: _ ->
                    printfn "FAKE-CACHING: Unknown special key '%s' in preprocessor directive: %A" s preprocessorDirective.Token
                    None
                  | FSharpParser.StringLike.StringItem s :: _ -> Some s
                  | _ ->
                    printfn "FAKE-CACHING: Unknown preprocessor directive found, please check your script (try to start it with fsi to get a more detailed error)! Preprocessor directive was: %A" preprocessorDirective.Token
                    None
                match preprocessorDirective with
                | { Token = { Representation = "#load" }; Strings = MatchFirstString childScriptRelPath } ->
                  let realPath = resolvePath currentIncludes currentDir childScriptRelPath false
                  currentIncludes, currentDir, getAllScriptsRec realPath currentIncludes @ childScripts
                | { Token = { Representation = "#cd" }; Strings = MatchFirstString relOrAbsolute } ->
                  let realPath = resolvePath currentIncludes currentDir relOrAbsolute true
                  currentIncludes, realPath, childScripts
                | { Token = { Representation = "#I" }; Strings = MatchFirstString relOrAbsolute } ->
                  let realPath = resolvePath currentIncludes currentDir relOrAbsolute true
                  realPath :: currentIncludes, currentDir, childScripts
                | _ -> state
            ) (parentIncludes, Path.GetDirectoryName scriptPath, [])
            |> fun (_, _, c) -> c
            |> List.rev
        { Location = scriptPath
          HashContent = FSharpParser.getHashableString scriptContents } :: loadedContents

    getAllScriptsRec scriptPath []

let getStringHash (s:string) =
    use sha256 = System.Security.Cryptography.SHA256.Create()
    s
    |> System.Text.Encoding.UTF8.GetBytes
    |> sha256.ComputeHash
    |> BitConverter.ToString
    |> fun s -> s.Replace("-", "")

let getScriptHash pathsAndContents compileOptions =
    (getAllScriptContents pathsAndContents |> String.concat "\n")
    + (pathsAndContents |> Seq.map(fun x -> x.Location |> Path.normalizePath) |> String.concat "\n")
    + (compileOptions |> String.concat "\n")
    |> getStringHash