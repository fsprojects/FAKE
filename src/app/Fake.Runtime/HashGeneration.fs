/// Contains helper functions which allow to interact with the F# Interactive.
module internal Fake.Runtime.HashGeneration

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

let getAllScripts (ignoreWhitespace:bool) defines (tokens:Fake.Runtime.FSharpParser.TokenizedScript) scriptPath : Script list =
    let rec getAllScriptsRec (tokens:Fake.Runtime.FSharpParser.TokenizedScript) workDir (scriptName:string) parentIncludes : Script list =
        let tryResolvePath currentIncludes currentDir relativeOrAbsolute isDir =
            let possiblePaths =
              if Path.IsPathRooted relativeOrAbsolute then [ relativeOrAbsolute ]
              else
                currentDir :: currentIncludes
                |> List.map (fun bas -> Path.Combine(bas, relativeOrAbsolute))
            possiblePaths
            |> Seq.tryFind (if isDir then Directory.Exists else File.Exists)
            |> Option.map Path.GetFullPath
        let resolvePath currentIncludes currentDir relativeOrAbsolute isDir =
            match tryResolvePath currentIncludes currentDir relativeOrAbsolute isDir with
            | Some f -> f
            | None ->
              failwithf "FAKE-CACHING: Could not find %s '%s' in any paths searched. Searched paths:\n%A" (if isDir then "directory" else "file") relativeOrAbsolute (currentDir :: currentIncludes)

        let loadedContents =
            ((parentIncludes, workDir, []), FSharpParser.findProcessorDirectives tokens)
            ||> List.fold (fun ((currentIncludes, currentDir, childScripts) as state) preprocessorDirective ->
                let (|MatchFirstString|_|) (l:FSharpParser.StringLike list) =
                  match l with
                  | FSharpParser.StringLike.StringKeyword FSharpParser.SourceDirectory :: _ ->
                    Some (".")
                  | FSharpParser.StringLike.StringKeyword FSharpParser.SourceFile :: _ ->
                    Some (scriptName)
                  | FSharpParser.StringLike.StringKeyword (FSharpParser.Unknown s) :: _ ->
                    printfn "FAKE-CACHING: Unknown special key '%s' in preprocessor directive: %A" s preprocessorDirective.Token
                    None
                  | FSharpParser.StringLike.StringItem s :: _ -> Some s
                  | _ ->
                    printfn "FAKE-CACHING: Unknown preprocessor directive found, please check your script (try to start it with fsi to get a more detailed error)! Preprocessor directive was: %A" preprocessorDirective.Token
                    None
                match preprocessorDirective with
                | { Token = { Representation = "#load" }; Strings = MatchFirstString childScriptRelPath } ->
                  let name = Path.GetFileName childScriptRelPath
                  // ignore intellisense file, because it might not be generated yet
                  if name = Runners.loadScriptName && childScriptRelPath.StartsWith ".fake"
                  then currentIncludes, currentDir, childScripts
                  else
                      let realPath =
                        try resolvePath currentIncludes currentDir childScriptRelPath false
                        with e ->
                            let p = String.Join("\n ", currentDir :: currentIncludes)
                            let msg =
                                sprintf "%s(%d,%d): error FS0078: Unable to find the file '%s' in any of\n %s"
                                    (Path.Combine(workDir, scriptPath))
                                    preprocessorDirective.Token.LineNumber
                                    (match preprocessorDirective.Token.TokenInfo with Some t -> t.LeftColumn + 1 | None -> 1)
                                    childScriptRelPath
                                    p
                            raise <| exn(msg, e)
                      let newWorkDir = Path.GetDirectoryName realPath
                      let newScriptName = Path.GetFileName realPath
                      let nestedTokens =
                          File.ReadLines realPath
                          |> FSharpParser.getTokenized realPath defines
                      currentIncludes, currentDir, getAllScriptsRec nestedTokens newWorkDir newScriptName currentIncludes @ childScripts
                | { Token = { Representation = "#cd" }; Strings = MatchFirstString relOrAbsolute } ->
                  let realPath = 
                    try resolvePath [] currentDir relOrAbsolute true
                    with e ->
                        let p = Path.Combine(currentDir, relOrAbsolute)
                        let msg =
                            sprintf "%s(%d,%d): error FS2302: Directory '%s' doesn't exist"
                                (Path.Combine(workDir, scriptPath))
                                preprocessorDirective.Token.LineNumber
                                (match preprocessorDirective.Token.TokenInfo with Some t -> t.LeftColumn + 1 | None -> 1)
                                p
                        raise <| exn(msg, e)
                  currentIncludes, realPath, childScripts
                | { Token = { Representation = "#I" }; Strings = MatchFirstString relOrAbsolute } ->
                  match tryResolvePath currentIncludes currentDir relOrAbsolute true with
                  | Some realPath ->
                    realPath :: currentIncludes, currentDir, childScripts
                  | None -> currentIncludes, currentDir, childScripts                
                | _ -> state
            ) 
            |> fun (_, _, c) -> c
        { Location = Path.Combine(workDir, scriptName)
          HashContent = FSharpParser.getHashableString ignoreWhitespace tokens } :: loadedContents
    let dir = Path.GetDirectoryName scriptPath
    let name = Path.GetFileName scriptPath
    getAllScriptsRec tokens dir name []

let getStringHash (s:string) =
    use sha256 = System.Security.Cryptography.SHA256.Create()
    s
    |> System.Text.Encoding.UTF8.GetBytes
    |> sha256.ComputeHash
    |> BitConverter.ToString
    |> fun s -> s.Replace("-", "")

let getCombinedString pathsAndContents compileOptions =
    let sb = new System.Text.StringBuilder()
    let inline appendSeq sequence =
        for s in sequence do sb.AppendLine s |> ignore
    appendSeq (getAllScriptContents pathsAndContents)
    appendSeq (pathsAndContents |> Seq.map(fun x -> x.Location |> Path.normalizePath))
    appendSeq compileOptions
    // remove last \n
    sb.Length <- sb.Length - 1
    sb.ToString()

let getScriptHash pathsAndContents compileOptions =
    getCombinedString pathsAndContents compileOptions
    |> getStringHash
