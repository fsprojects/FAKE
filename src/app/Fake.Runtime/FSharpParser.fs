module Fake.Runtime.FSharpParser

open Microsoft.FSharp.Compiler.SourceCodeServices
type Token = { Representation : string; LineNumber : int; TokenInfo : FSharpTokenInfo option }
type TokenizedScript =
    private { Tokens : Token list }

let getTokenized (filePath:string) defines lines =
    let tokenizer = FSharpSourceTokenizer(defines, Some filePath)
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


/// Parse #r references for `paket:` lines
type InterestingItem =
  | Reference of string
 
type AnalyseState =
  | NoAnalysis
  | Reference of string option

let findInterestingItems (scriptFile:string) (scriptText:string) =
  let rec tokenizeLine results (line:string) (tokenizer:FSharpLineTokenizer) state =
      match tokenizer.ScanToken(state) with
      | Some tok, state ->
          // Print token name
          let result = Some tok, (line.Substring(tok.LeftColumn, tok.RightColumn - tok.LeftColumn + 1))
          // Tokenize the rest, in the new state
          tokenizeLine (result::results) line tokenizer state
      | None, state -> results |> List.rev, state
  let rec tokenizeLines (sourceTok : FSharpSourceTokenizer) state lines = seq {
      match lines with
      | line::lines ->
          // Create tokenizer & tokenize single line
          let tokenizer = sourceTok.CreateLineTokenizer(line)
          let lineResults, state = tokenizeLine [] line tokenizer state
          yield! lineResults
          yield None, "\n" 
          // Tokenize the rest using new state
          yield! tokenizeLines sourceTok state lines
      | [] -> () }
  let rec analyseNextToken (_, state) (tok :FSharpTokenInfo option, text) =
    match state with
    | NoAnalysis ->
      if tok.IsSome && tok.Value.TokenName = "HASH" && text = "#r" then
        None, Reference None
      else None, NoAnalysis
    | Reference None -> // Initial string start
      if tok.IsSome && tok.Value.TokenName = "STRING_TEXT" then
        None, Reference (Some "")
      else None, Reference None
    | Reference (Some s) -> // read string
      if tok.IsNone || (tok.IsSome && tok.Value.TokenName = "STRING_TEXT") then
        None, Reference (Some (s + text))
      elif tok.IsSome && tok.Value.TokenName = "STRING" then
        Some (InterestingItem.Reference s), NoAnalysis
      else failwithf "No idea how %A can happen in a string" (tok, text) // None, Reference (Some s)

  let sourceTok = FSharpSourceTokenizer(["FAKE_DEPENDENCIES"], Some scriptFile)            
  scriptText.Split('\r','\n')
    |> List.ofSeq
    |> tokenizeLines sourceTok 0L
    |> Seq.scan analyseNextToken (None, NoAnalysis)
    |> Seq.choose fst
    |> Seq.toList