namespace Docopt
#nowarn "62"

open System
open System.Collections.Generic

type private 'a GList = System.Collections.Generic.List<'a>

[<RequireQualifiedAccess>]
type Tag =
  | Eps = 0b00000000
  | Ano = 0b00000001
  | Sop = 0b00000010
  | Lop = 0b00000011
  | Sqb = 0b00000100
  | Req = 0b00000101
  | Arg = 0b00000110
  | Xor = 0b00000111
  | Seq = 0b00001000
  | Cmd = 0b00001001
  | Ell = 0b00001010
  | Sdh = 0b00001011

[<RequireQualifiedAccess>]
type UsageAst =
  /// matches nothing?
  | Eps
  /// Matches an option annotation [options]
  | Ano of title:string * o':SafeOptions
  /// Short options
  | Sop of o':SafeOptions
  /// long option
  | Lop of o':SafeOption
  /// Marks the given item as optional
  | Sqb of ast':UsageAst
  /// Requires the given item
  | Req of ast':UsageAst
  /// Named/Positional argument
  | Arg of name':string
  | XorEmpty
  /// Either the one or the other
  | Xor of l':UsageAst * r':UsageAst
  /// Sequence of items, if the items are only options then order is ignored.
  | Seq of asts':UsageAst list
  /// Fixed command, like "push" in "git push"
  | Cmd of cmd':string
  /// Marks that the given item can be given multiple times 
  | Ell of ast':UsageAst
  /// matches the stdin [-]
  | Sdh
  member x.InstanceOfSop = match x with UsageAst.Sop _ -> true | _ -> false
  member x.ContainsOnlyOptions =
    match x with
    | Eps -> true
    | Ano _ -> true
    | Sop _ -> true
    | Lop _ -> true
    | Sqb ast -> ast.ContainsOnlyOptions
    | Req ast -> ast.ContainsOnlyOptions
    | Arg _ -> false
    | XorEmpty -> true
    | Xor (l, r) -> l.ContainsOnlyOptions && r.ContainsOnlyOptions
    | Seq asts ->
      asts |> Seq.forall (fun t -> t.ContainsOnlyOptions)
    | Cmd _ -> false
    | Ell ast -> ast.ContainsOnlyOptions
    | Sdh -> true

[<RequireQualifiedAccess>]
type UsageAstBuilder =
  | Eps
  | Ano of title:string * o':SafeOptions
  | Sop of o':SafeOptions
  | Lop of o':SafeOption
  | Sqb of ast':UsageAstCell
  | Req of ast':UsageAstCell
  | Arg of name':string
  | XorEmpty
  | Xor of l':UsageAstCell * r':UsageAstCell
  | Seq of asts':UsageAstCell list
  | Cmd of cmd':string
  | Ell of ast':UsageAstCell
  | Sdh
  static member ToCell x = { Content = Some x }
  member x.UsageTag =
    match x with
    | UsageAstBuilder.Eps _ -> Tag.Eps
    | UsageAstBuilder.Ano _ -> Tag.Ano
    | UsageAstBuilder.Sop _ -> Tag.Sop
    | UsageAstBuilder.Lop _ -> Tag.Lop
    | UsageAstBuilder.Sqb _ -> Tag.Sqb
    | UsageAstBuilder.Req _ -> Tag.Req
    | UsageAstBuilder.Arg _ -> Tag.Arg
    | UsageAstBuilder.XorEmpty _ -> Tag.Xor
    | UsageAstBuilder.Xor _ -> Tag.Xor
    | UsageAstBuilder.Seq _ -> Tag.Seq
    | UsageAstBuilder.Cmd _ -> Tag.Cmd
    | UsageAstBuilder.Ell _ -> Tag.Ell
    | UsageAstBuilder.Sdh _ -> Tag.Sdh

and UsageAstCell =
  { mutable Content : UsageAstBuilder option }
  static member FromBuilder x = { Content = Some x }  
  member x.Build () =
    match x.Content with
    | None -> failwithf "Nullref"
    | Some c ->
    match c with
    | UsageAstBuilder.Eps -> UsageAst.Eps
    | UsageAstBuilder.Ano (title, o') -> UsageAst.Ano (title, o')
    | UsageAstBuilder.Sop o' -> UsageAst.Sop o'
    | UsageAstBuilder.Lop o' -> UsageAst.Lop o'
    | UsageAstBuilder.Sqb ast' -> UsageAst.Sqb (ast'.Build())
    | UsageAstBuilder.Req ast' -> UsageAst.Req (ast'.Build())
    | UsageAstBuilder.Arg name' -> UsageAst.Arg name'
    | UsageAstBuilder.XorEmpty -> UsageAst.XorEmpty
    | UsageAstBuilder.Xor (l', r') -> UsageAst.Xor(l'.Build(), r'.Build())
    | UsageAstBuilder.Seq asts' -> UsageAst.Seq(asts' |> Seq.map (fun ast -> ast.Build()) |> Seq.toList)
    | UsageAstBuilder.Cmd cmd' -> UsageAst.Cmd cmd'
    | UsageAstBuilder.Ell ast' -> UsageAst.Ell (ast'.Build())
    | UsageAstBuilder.Sdh -> UsageAst.Sdh

[<AllowNullLiteral>]
type IAst =
  interface
    abstract Tag : Tag
    abstract MatchSopt : sopt:string * getArg:(string -> string) -> string
    abstract MatchLopt : lopt:string * getArg:(string -> string) -> bool
    abstract MatchArg : arg:string -> bool
    abstract TryFill : args:Arguments.Dictionary -> bool
    abstract DeepCopy : unit -> IAst
    abstract Instance : IAst
  end

type DebugAst private (ast : IAst) =
  class
    static member Create(ast:IAst) =
      //DebugAst(ast) :> IAst
      ast
    interface IAst with
      member __.Tag = ast.Tag
      member __.Instance = ast
      member __.MatchSopt(s', getArg) = 
        let res = ast.MatchSopt(s', getArg)
        printfn "DEBUGAST: %O MatchSopt(%O, _) -> %s" ast s' res
        res
      member __.MatchLopt(l', getArg) =
        let res = ast.MatchLopt(l', getArg)
        printfn "DEBUGAST: %O MatchLopt(%O, _) -> %b" ast l' res
        res
      member __.MatchArg(arg) =
        let res = ast.MatchArg(arg)
        printfn "DEBUGAST: %O MatchArg(%O) -> %b" ast arg res
        res
      member __.TryFill(args) =
        let res = ast.TryFill(args)
        printfn "DEBUGAST: %O MatchArg(%O) -> %b" ast args res
        res
      member __.DeepCopy() = DebugAst(ast.DeepCopy()) :> IAst
    end
    override __.ToString() = ast.ToString()
  end

type Eps private () =
  class
    static member Instance = DebugAst.Create(Eps())
    interface IAst with
      member __.Tag = Tag.Eps
      member x.Instance = x :> IAst
      member __.MatchSopt(s', _) = s'
      member __.MatchLopt(_, _) = false
      member __.MatchArg(_) = false
      member __.TryFill(_) = true
      member __.DeepCopy() = Eps.Instance
    end
    override __.ToString() = "Eps"
  end

type Sop private (o':Options) =
  class
    let matched = GList<Option * string option>(o'.Count)
    static member Success = ( = ) ""
    static member Create (o':Options) = DebugAst.Create(Sop(o'))
    interface IAst with
      member __.Tag = Tag.Sop
      member x.Instance = x :> IAst
      member __.MatchSopt(s', getArg') =
        let rec loop i = function
        | s when String.IsNullOrEmpty(s) || i >= s.Length -> s
        | s -> match o'.FindAndRemove(s.[i]) with
               | null -> loop (i + 1) s
               | opt  -> match opt.HasArgument, i = s.Length - 1 with
                         | true, true -> matched.Add(opt, Some(getArg' opt.ArgName));
                                         loop i (s.Remove(i, 1))
                         | true, _    -> let arg = s.Substring(i + 1) in
                                         matched.Add(opt, Some(arg));
                                         loop Int32.MaxValue (s.Substring(0, i))
                         | _          -> matched.Add(opt, None);
                                         loop i (s.Remove(i, 1))
        in loop 0 s'
      member __.MatchLopt(_, _) = false
      member __.MatchArg(_) = false
      member __.TryFill(args') =
        try
          for sopt, arg in matched do
            args'.AddOpt(sopt, ?arg'=arg)
          done;
          o'.Count = 0
        with :? KeyNotFoundException -> false
      member __.DeepCopy() = Sop(o'.Copy()) :> IAst
    end
    override __.ToString() = sprintf "Sop %A" (Seq.toList o')
    member __.AddRange(range':#IEnumerable<Option>) = o'.AddRange(range')
    member __.Option = o'.[o'.Count - 1]
  end

type Lop private(o':Option) =
  class
    let mutable matched = false
    let mutable arg = None
    static member Create (o':Option) = DebugAst.Create(Lop(o'))
    interface IAst with
      member __.Tag = Tag.Lop
      member x.Instance = x :> IAst
      member __.MatchSopt(s', _) = s'
      member __.MatchLopt(l', getArg') =
        if matched
        then false
        elif l' = o'.Long || o'.Long.StartsWith(l')
        then (if o'.HasArgument
              then arg <- Some(getArg' o'.ArgName);
              matched <- true; true)
        else false
      member __.MatchArg(_) = false
      member __.TryFill(args') =
        if matched
        then (args'.AddOpt(o', ?arg'=arg); true)
        else false
      member __.DeepCopy() = Lop(o') :> IAst
    end
    override __.ToString() = sprintf "Lop %A" o'
    member __.Option = o'
  end

type Ano private(o':Options) =
  class
    let matched = GList<Option * string option>(o'.Count)
    static member Create (o':Options) = DebugAst.Create(Ano(o'))
    interface IAst with
      member __.Tag = Tag.Ano
      member x.Instance = x :> IAst
      member __.MatchSopt(s', getArg') =
        let rec loop i = function
        | s when String.IsNullOrEmpty(s) || i >= s.Length -> s
        | s -> match o'.FindAndRemove(s.[i]) with
               | null -> loop 0 null
               | opt  -> match opt.HasArgument, i = s.Length - 1 with
                         | true, true -> matched.Add(opt, Some(getArg' opt.ArgName));
                                         loop i (s.Remove(i, 1))
                         | true, _    -> let arg = s.Substring(i + 1) in
                                         matched.Add(opt, Some(arg));
                                         loop Int32.MaxValue (s.Substring(0, i))
                         | _          -> matched.Add(opt, None);
                                         loop i (s.Remove(i, 1))
        in loop 0 s'
      member __.MatchLopt(l', getArg') =
        match o'.Find(l') with
        | null -> false
        | opt  -> if o'.FindLast(l') = opt
                  then (matched.Add(opt, if opt.HasArgument
                                         then Some(getArg' opt.ArgName)
                                         else None);
                        true)
                  else false
      member __.MatchArg(_) = false
      member __.TryFill(args') =
        try
          for opt, arg in matched do
            args'.AddOpt(opt, ?arg'=arg)
          done;
          true
        with :? KeyNotFoundException -> false
      member __.DeepCopy() = Ano(o') :> IAst
    end
    override __.ToString() = "Ano"
  end

type Sqb private(ast':IAst) =
  class
    let mutable matched = false
    let hasMatched = function
    | true -> matched <- true; true
    | _    -> false
    static member Create (ast':IAst) = DebugAst.Create(Sqb(ast'))
    interface IAst with
      member __.Tag = Tag.Sqb
      member x.Instance = x :> IAst
      member __.MatchSopt(s', a') =
        let res = ast'.MatchSopt(s', a') in
        res <> s' |> hasMatched |> ignore;
        res
      member __.MatchLopt(l', a') = ast'.MatchLopt(l', a') |> hasMatched
      member __.MatchArg(a') = ast'.MatchArg(a') |> hasMatched
      member __.TryFill(a') = if ast'.Tag = Tag.Seq || ast'.Tag = Tag.Sop
                              then (ast'.TryFill(a') |> ignore; true)
                              else ast'.TryFill(a') || not matched // A←B
      member __.DeepCopy() = Sqb(ast'.DeepCopy()) :> IAst
    end
    override __.ToString() = sprintf "Sqb (%A)" ast'
  end

type Req private(ast':IAst) =
  class
    static member Create (ast':IAst) = DebugAst.Create(Req(ast'))
    interface IAst with
      member __.Tag = Tag.Req
      member x.Instance = x :> IAst
      member __.MatchSopt(s', a') = ast'.MatchSopt(s', a')
      member __.MatchLopt(l', a') = ast'.MatchLopt(l', a')
      member __.MatchArg(a') = ast'.MatchArg(a')
      member __.TryFill(a') = ast'.TryFill(a')
      member __.DeepCopy() = Req(ast'.DeepCopy()) :> IAst
    end
    override __.ToString() = sprintf "Req (%A)" ast'
  end

type Arg private(name':string) =
  class
    let mutable value = null
    static member Create (name':string) = DebugAst.Create(Arg(name'))
    interface IAst with
      member __.Tag = Tag.Arg
      member x.Instance = x :> IAst
      member __.MatchSopt(s', _) = s'
      member __.MatchLopt(_, _) = false
      member __.MatchArg(value') =
        if isNull value
        then (value <- value'; true)
        else false
      member __.TryFill(args') =
        if isNull value
        then false
        else (args'.AddArg(name', value); true)
      member __.DeepCopy() = Arg(name') :> IAst
    end
    override __.ToString() = "Arg " + name'
  end

type Xor private(l':IAst, r':IAst) =
  class
    let mutable lOk = true
    let mutable rOk = true
    static member Create(l':IAst, r':IAst) = DebugAst.Create(Xor(l', r'))
    static member Create() = Xor.Create(Eps.Instance, Eps.Instance)
    interface IAst with
      member __.Tag = Tag.Xor
      member x.Instance = x :> IAst
      member __.MatchSopt(sopt', getArg') =
        let lres = lazy l'.MatchSopt(sopt', getArg') in
        let rres = lazy r'.MatchSopt(sopt', getArg') in
        let lmatch = lOk && (lres.Value |> Sop.Success) in
        let rmatch = rOk && (rres.Value |> Sop.Success) in
        match lmatch, rmatch with
        | true, true   -> lres.Value
        | true, false  -> rOk <- false; lres.Value
        | false, true  -> lOk <- false; rres.Value
        | false, false -> null
      member __.MatchLopt(lopt', getArg') =
        let getArg = let s = ref String.Empty in
                     let arg = lazy getArg' !s in
                     fun s' -> s := s'; arg.Value in
        let lmatch = lOk && l'.MatchLopt(lopt', getArg) in
        let rmatch = rOk && r'.MatchLopt(lopt', getArg) in
        match lmatch, rmatch with
        | true, true   -> true
        | true, false  -> rOk <- false; true
        | false, true  -> lOk <- false; true
        | false, false -> false
      member __.MatchArg(a') =
        let lmatch = lOk && l'.MatchArg(a') in
        let rmatch = rOk && r'.MatchArg(a') in
        match lmatch, rmatch with
        | true, true   -> true
        | true, false  -> rOk <- false; true
        | false, true  -> lOk <- false; true
        | false, false -> false
      member __.TryFill(a') =
        match lOk, rOk with
        | true, false  -> l'.TryFill(a')
        | false, true  -> r'.TryFill(a')
        | false, false -> false
        | true, true   -> let tempDict = Arguments.Dictionary() in
                          if l'.TryFill(tempDict)
                             || (tempDict.Clear(); r'.TryFill(tempDict))
                          then (a'.AddRange(tempDict); true)
                          else false
      member __.DeepCopy() = Xor(l'.DeepCopy(), r'.DeepCopy()) :> IAst
    end
    override __.ToString() = sprintf "Xor (%A | %A)" l' r'
  end

type Seq private(asts':GList<IAst>) =
  class
    static member Create (ast':GList<IAst>) = DebugAst.Create(Seq(ast'))
    interface IAst with
      member __.Tag = Tag.Seq
      member x.Instance = x :> IAst
      member __.MatchSopt(shorts', a') =
        asts'
        |> Seq.fold (fun shorts' ast' -> ast'.MatchSopt(shorts', a')) shorts'
      member __.MatchLopt(l', a') =
        Seq.exists (fun (ast':IAst) -> ast'.MatchLopt(l', a')) asts'
      member __.MatchArg(a') =
        Seq.exists (fun (ast':IAst) -> ast'.MatchArg(a')) asts'
      member __.TryFill(args') =
        Seq.forall (fun (ast':IAst) -> ast'.TryFill(args')) asts'
      member __.DeepCopy() =
        let astsCopy = seq {for ast in asts' -> ast.DeepCopy()} in
        Seq(GList<IAst>(astsCopy)) :> IAst
    end
    override __.ToString() = sprintf "Seq %A" (Seq.toList asts')
    member __.Asts = asts'
  end

type Cmd(cmd':string) =
  class
    let mutable matched = false
    static member Create (cmd':string) = DebugAst.Create(Cmd(cmd'))
    interface IAst with
      member __.Tag = Tag.Cmd
      member x.Instance = x :> IAst
      member __.MatchSopt(s', _) = s'
      member __.MatchLopt(_, _) = false
      member __.MatchArg(a') =
        match not matched && a' = cmd' with
        | true -> matched <- true; true
        | _    -> false
      member __.TryFill(args') =
        match matched with
        | true -> args'.AddCmd(cmd'); true
        | _    -> false
      member __.DeepCopy() = Cmd(cmd') :> IAst
    end
    override __.ToString() = "Cmd " + cmd'
  end

type Ell(ast':IAst) =
  class
    let mutable hasMatched = false
    let mutable currentAst = ast'.DeepCopy()
    let matched = GList<IAst>()
    static member Create (ast':IAst) = DebugAst.Create(Ell(ast'))
    interface IAst with
      member __.Tag = Tag.Ell
      member x.Instance = x :> IAst
      member xx.MatchSopt(s', a') =
        let res = currentAst.MatchSopt(s', a') in
        match res <> s' with
        | true -> hasMatched <- true;
                  if String.IsNullOrEmpty(res)
                  then res
                  else (xx :> IAst).MatchSopt(res, a')
        | _    -> match hasMatched with
                  | false -> res
                  | _     -> matched.Add(currentAst);
                             currentAst <- ast'.DeepCopy();
                             hasMatched <- false;
                             (xx :> IAst).MatchSopt(res, a')
      member xx.MatchLopt(l', a') =
        match currentAst.MatchLopt(l', a') with
        | true -> hasMatched <- true; true
        | _    -> match hasMatched with
                  | false -> false
                  | _     -> matched.Add(currentAst);
                             currentAst <- ast'.DeepCopy();
                             hasMatched <- false;
                             (xx :> IAst).MatchLopt(l', a')
      member xx.MatchArg(a') =
        match currentAst.MatchArg(a') with
        | true -> hasMatched <- true; true
        | _    -> match hasMatched with
                  | false -> false
                  | _     -> matched.Add(currentAst);
                             currentAst <- ast'.DeepCopy();
                             hasMatched <- false;
                             (xx :> IAst).MatchArg(a')
      member __.TryFill(args') =
        if hasMatched
        then matched.Add(currentAst);
        match matched.Count with
        | 0 -> ast'.Tag = Tag.Sqb
        | _ -> matched |> Seq.forall (fun ast' -> ast'.TryFill(args'))
      member __.DeepCopy() = Ell(ast') :> IAst // No need to copy ast'
    end
    override __.ToString() = sprintf "Ell (%A)" ast'
  end

type Sdh private () =
  class
    let mutable matched = false
    static member Instance = DebugAst.Create(Sdh())
    interface IAst with
      member __.Tag = Tag.Sdh
      member x.Instance = x :> IAst
      member __.MatchSopt(s', _) = s'
      member __.MatchLopt(_, _) = false
      member __.MatchArg(a') =
        match a' with
        | "-" -> matched <- true; true
        | _   -> false
      member __.TryFill(args') =
        match matched with
        | false -> true
        | true  -> args'.AddString("-");
                   matched <- false;
                   true
      member __.DeepCopy() = Sdh.Instance
    end
    override __.ToString() = "Sdh"
  end
