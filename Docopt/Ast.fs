namespace Docopt
#nowarn "62"
#light "off"

open System
open System.Collections.Generic

type private 'a GList = System.Collections.Generic.List<'a>

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

[<AllowNullLiteral>]
type IAst =
  interface
    abstract Tag : Tag
    abstract MatchSopt : sopt:string * getArg:(string -> string) -> bool
    abstract MatchLopt : lopt:string * getArg:(string -> string) -> bool
    abstract MatchArg : arg:string -> bool
    abstract TryFill : args:Arguments.Dictionary -> bool
    abstract DeepCopy : unit -> IAst
  end

type Eps private () =
  class
    static member Instance = Eps() :> IAst
    interface IAst with
      member __.Tag = Tag.Eps
      member __.MatchSopt(_, _) = false
      member __.MatchLopt(_, _) = false
      member __.MatchArg(_) = false
      member __.TryFill(_) = true
      member __.DeepCopy() = Eps.Instance
    end
    override __.ToString() = "Eps"
  end

type Sop(o':Options) =
  class
    let matched = GList<Option * string option>(o'.Count)
    interface IAst with
      member __.Tag = Tag.Sop
      member __.MatchSopt(s', getArg') =
        let mutable ret = true in
        let mutable i = 0 in
        while i < s'.Length do
          (match o'.FindAndRemove(s'.[i]) with
           | null -> ret <- false
           | opt  -> ret <- true;
                     matched.Add(opt, if opt.HasArgument && i = s'.Length - 1
                                      then Some(getArg' opt.ArgName)
                                      elif opt.HasArgument
                                      then (let arg = s'.Substring(i + 1) in
                                            i <- s'.Length;
                                            Some(arg))
                                      else None));
          i <- i + 1
        done;
        ret
      member __.MatchLopt(_, _) = false
      member __.MatchArg(_) = false
      member __.TryFill(args') =
        if o'.Count <> 0
        then false
        else try
          for sopt, arg in matched do
            args'.AddShort(sopt, ?arg'=arg)
          done;
          true
        with :? KeyNotFoundException -> false
      member __.DeepCopy() = Sop(o'.Copy()) :> IAst
    end
    override __.ToString() = sprintf "Sop %A" (Seq.toList o')
    member __.AddRange(range':#IEnumerable<Option>) = o'.AddRange(range')
    member __.Option = o'.[o'.Count - 1]
  end

type Lop(o':Option) =
  class
    let mutable matched = false
    let mutable arg = None
    interface IAst with
      member __.Tag = Tag.Lop
      member __.MatchSopt(_, _) = false
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
        then (args'.AddLong(o', ?arg'=arg); true)
        else false
      member __.DeepCopy() = Lop(o') :> IAst
    end
    override __.ToString() = sprintf "Lop %A" o'
    member __.Option = o'
  end

type Ano(o':Options) =
  class
    let matched = GList<Option * string option>(o'.Count)
    interface IAst with
      member __.Tag = Tag.Ano
      member __.MatchSopt(s', getArg') =
        let mutable ret = true in
        let mutable i = 0 in
        while i < s'.Length do
          (match o'.Find(s'.[i]) with
           | null -> ret <- false; i <- s'.Length
           | opt  -> matched.Add(opt, if opt.HasArgument && i = s'.Length - 1
                                      then Some(getArg' opt.ArgName)
                                      elif opt.HasArgument
                                      then (let j = i + 1 in
                                            i <- s'.Length;
                                            Some(s'.Substring(j)))
                                      else None));
          i <- i + 1
        done;
        ret
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

type Sqb(ast':IAst) =
  class
    let mutable matched = false
    let hasMatched = function
    | true -> matched <- true; true
    | _    -> false
    interface IAst with
      member __.Tag = Tag.Sqb
      member __.MatchSopt(s', a') = ast'.MatchSopt(s', a') |> hasMatched
      member __.MatchLopt(l', a') = ast'.MatchLopt(l', a') |> hasMatched
      member __.MatchArg(a') = ast'.MatchArg(a') |> hasMatched
      member __.TryFill(a') = if ast'.Tag <> Tag.Seq
                              then ast'.TryFill(a') || not matched // A←B
                              else (ast'.TryFill(a') |> ignore; true)
      member __.DeepCopy() = Sqb(ast'.DeepCopy()) :> IAst
    end
    override __.ToString() = sprintf "Sqb (%A)" ast'
  end

type Req(ast':IAst) =
  class
    interface IAst with
      member __.Tag = Tag.Req
      member __.MatchSopt(s', a') = ast'.MatchSopt(s', a')
      member __.MatchLopt(l', a') = ast'.MatchLopt(l', a')
      member __.MatchArg(a') = ast'.MatchArg(a')
      member __.TryFill(a') = ast'.TryFill(a')
      member __.DeepCopy() = Req(ast'.DeepCopy()) :> IAst
    end
    override __.ToString() = sprintf "Req (%A)" ast'
  end

type Arg(name':string) =
  class
    let mutable value = null
    interface IAst with
      member __.Tag = Tag.Arg
      member __.MatchSopt(_, _) = false
      member __.MatchLopt(_, _) = false
      member __.MatchArg(value') =
        if value = null
        then (value <- value'; true)
        else false
      member __.TryFill(args') =
        if value = null
        then false
        else (args'.AddArg(name', value); true)
      member __.DeepCopy() = Arg(name') :> IAst
    end
    override __.ToString() = "Arg " + name'
  end

type Xor(l':IAst, r':IAst) =
  class
    let mutable lOk = true
    let mutable rOk = true
    new() = Xor(Eps.Instance, Eps.Instance)
    interface IAst with
      member __.Tag = Tag.Xor
      member __.MatchSopt(sopt', getArg') =
        let getArg = let s = ref String.Empty in     // In case getArg' is
                     let arg = lazy getArg' !s in    // called by l' and r',
                     fun s' -> s := s'; arg.Value in // make it a lazy value
        let lmatch = lOk && l'.MatchSopt(sopt', getArg) in
        let rmatch = rOk && r'.MatchSopt(sopt', getArg) in
        match lmatch, rmatch with
        | true, true   -> true
        | true, false  -> rOk <- false; true
        | false, true  -> lOk <- false; true
        | false, false -> false
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
        | true, true   -> let tempDict = Arguments.Dictionary(Options()) in
                          if l'.TryFill(tempDict)
                          then (a'.AddRange(tempDict); true)
                          elif (tempDict.Clear(); r'.TryFill(tempDict))
                          then (a'.AddRange(tempDict); true)
                          else false
      member __.DeepCopy() = Xor(l'.DeepCopy(), r'.DeepCopy()) :> IAst
    end
    override __.ToString() = sprintf "Xor (%A | %A)" l' r'
  end

type Seq(asts':GList<IAst>) =
  class
    interface IAst with
      member __.Tag = Tag.Seq
      member __.MatchSopt(s', a') =
        Seq.exists (fun (ast':IAst) -> ast'.MatchSopt(s', a')) asts'
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
    interface IAst with
      member __.Tag = Tag.Cmd
      member __.MatchSopt(_, _) = false
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
    interface IAst with
      member __.Tag = Tag.Ell
      member __.MatchSopt(s', a') = false
      member __.MatchLopt(s', a') = false
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
