/// Contains helper functions which allow to interact with the F# Interactive.
module Fake.Runtime.HashGeneration

open System
open System.IO
open System.Text.RegularExpressions
open System.Xml.Linq
open Yaaf.FSharp.Scripting

module TokenizerHelper =
    open Microsoft.FSharp.Compiler.SourceCodeServices
    type Token = { Representation : string; LineNumber : int; TokenInfo : FSharpTokenInfo option }
    type TokenizedScript =
        private { Tokens : Token list }

    let getTokenized filePath defines lines = 
        let tokenizer = FSharpSourceTokenizer(defines, filePath)
        /// Tokenize a single line of F# code
        let rec tokenizeLine (tokenizer:FSharpLineTokenizer) state =
          let raw =
            Seq.initInfinite (fun _ -> tokenizer)
            |> Seq.scan (fun (_, prev, state) tokenizer -> let cur, state = tokenizer.ScanToken state in prev, cur, state) (None, None, state)
            |> Seq.skip 1
            |> Seq.takeWhile (fun (prev,cur, _) -> prev.IsSome || cur.IsSome)
            |> Seq.map (fun (_, cur, state) -> cur, state)
            |> Seq.toList
          raw 
          |> List.choose fst, raw |> List.tryLast |> Option.map snd

        lines
        |> Seq.mapi (fun lineNr line -> lineNr + 1, line, tokenizer.CreateLineTokenizer line)
        |> Seq.scan (fun (_, state) (lineNr, line, tokenizer) ->
          let tokens, newState = tokenizeLine tokenizer state
          let newState = defaultArg newState state
          tokens
          |> List.map (fun (token) -> 
              { Representation = line.Substring(token.LeftColumn, token.RightColumn - token.LeftColumn + 1)
                LineNumber = lineNr
                TokenInfo = Some token })
          |> (fun l -> { Representation = "\n"; LineNumber = lineNr; TokenInfo = None } :: l), newState) ([], 0L)
        |> Seq.collect (fst)
        |> Seq.toList
        |> fun t -> { Tokens = t }
    
    let getHashableString { Tokens = tokens } =
        let mutable rawS =
          tokens
          |> Seq.filter (fun (token) ->
            match token.TokenInfo with
            | Some tok when tok.TokenName = "INACTIVECODE" -> false
            | Some tok when tok.TokenName = "LINE_COMMENT" -> false
            | Some tok when tok.TokenName = "COMMENT" -> false
            | _ -> true)
          |> Seq.map (fun token -> token.Representation)
          |> fun s -> System.String.Join("", s).Replace("\r\n", "\n").Replace("\r", "\n")
        // This is to ensure the hash doesn't change when an #else section modified
        // Note that this doesn't get all cases, but probably the most
        while rawS.Contains("\n\n\n") do
          rawS <- rawS.Replace("\n\n\n", "\n\n")
        rawS

    type StringKeyword =
        | Unknown of string
        | SourceFile
        | SourceDirectory
    type StringLike =
        | StringItem of string
        | StringKeyword of StringKeyword
    type PreprocessorDirective = 
        { Token : Token; Strings : StringLike list }

    let handleRawString (s:string) =
        if s.StartsWith("\"") then
          s.Substring(1, s.Length - 2).Replace("\\\\", "\\")
        elif s.StartsWith ("@\"") then
          s.Substring(2, s.Length - 3).Replace("\"\"", "\"")
        else failwithf "cannot handle raw string %s" s

    let private handlePreprocessorTokens (tokens:Token list) =
        let (firstTok) = tokens |> List.head
        
        let strings =
          tokens
          |> Seq.skip 1
          |> Seq.fold (fun state (token) ->
            let tokenInfo =
              match token.TokenInfo with
              | Some ti -> ti
              | None -> failwith "Didn't expect newline token at this point."
            let data = token.Representation
            match state, tokenInfo with
            | [], tok when tok.TokenName = "STRING_TEXT" -> [[StringItem data]]
            | h :: t, tok when tok.TokenName = "STRING_TEXT" -> (StringItem data :: h) :: t
            | [], tok when tok.TokenName = "STRING" -> [[]; [StringItem data]]
            | h :: t, tok when tok.TokenName = "STRING" -> [] :: (StringItem data :: h) :: t
            | _, tok when tok.TokenName = "KEYWORD_STRING" ->
              match data with
              | "__SOURCE_FILE__" -> [] :: [StringKeyword StringKeyword.SourceFile] :: state
              | "__SOURCE_DIRECTORY__" -> [] :: [StringKeyword StringKeyword.SourceDirectory] :: state
              | _ -> [] :: [StringKeyword (StringKeyword.Unknown data)] :: state
            | _ -> state
            ) []
          |> function [] :: t -> t | r -> r // skip empty item
          |> List.rev
          |> List.map (fun items ->
            match items with
            | [s] -> s
            | [] -> failwith "unexpected empty list"
            | _ -> items 
                   |> List.map (function | StringItem i -> i | _ -> failwith "string cannot be combined with something else!")
                   |> List.fold (fun s item -> item + s) ""
                   |> StringItem)
          |> List.map (function | StringItem i -> StringItem (handleRawString i) | a -> a)

        { Token = firstTok; Strings = strings }

    let findProcessorDirectives { Tokens = tokens } =
        tokens
        |> Seq.fold (fun (items, collectDirective) (token) ->
          match items, collectDirective, token.TokenInfo with
          | h :: rest, true, Some _ -> (token :: h) :: rest, true
          | _, true, None -> items, false
          | _, false, Some (tok) when tok.TokenName = "HASH" ->
            [token] :: items, true
          | _, false, _ -> items, false
          | _ -> failwithf "Unknown state %A" (items, collectDirective, token)
          ) ([], false)
        |> fst
        |> List.map (List.rev >> handlePreprocessorTokens)
        |> List.rev


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
          |> TokenizerHelper.getTokenized scriptPath defines
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
            |> TokenizerHelper.findProcessorDirectives
            |> List.fold (fun ((currentIncludes, currentDir, childScripts) as state) preprocessorDirective ->
                let (|MatchFirstString|_|) (l:TokenizerHelper.StringLike list) =
                  match l with
                  | TokenizerHelper.StringLike.StringKeyword TokenizerHelper.SourceDirectory :: _ -> 
                    Some (Path.GetDirectoryName scriptPath)
                  | TokenizerHelper.StringLike.StringKeyword TokenizerHelper.SourceFile :: _ -> 
                    Some (Path.GetFileName scriptPath)
                  | TokenizerHelper.StringLike.StringKeyword (TokenizerHelper.Unknown s) :: _ -> 
                    printfn "FAKE-CACHING: Unknown special key '%s' in preprocessor directive: %A" s preprocessorDirective.Token
                    None
                  | TokenizerHelper.StringLike.StringItem s :: _ -> Some s
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
          HashContent = TokenizerHelper.getHashableString scriptContents } :: loadedContents

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