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
  | Sqb = 0b00000011
  | Req = 0b00000100
  | Arg = 0b00000101
  | Xor = 0b00000110
  | Seq = 0b00000111

[<AllowNullLiteral>]
type IAst =
  interface
    abstract Tag : Tag
    abstract MatchSopt : sopt:string * getArg:(string -> string) -> bool
    abstract MatchLopt : lopt:string * getArg:(string -> string) -> bool
    abstract MatchArg : arg:string -> bool
    abstract TryFill : args:Arguments.Dictionary -> bool
  end

type Eps() =
  class
    interface IAst with
      member __.Tag = Tag.Eps
      member __.MatchSopt(_, _) = false
      member __.MatchLopt(_, _) = false
      member __.MatchArg(_) = false
      member __.TryFill(_) = true
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
    end
    override __.ToString() = sprintf "Sop %A" (Seq.toList o')
    member __.AddRange(range':#IEnumerable<Option>) = o'.AddRange(range')
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
      member __.TryFill(a') = ast'.TryFill(a') || not matched // A←B
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
    end
    override __.ToString() = sprintf "Req (%A)" ast'
  end

type Arg(name':string) =
  class
    interface IAst with
      member __.Tag = Tag.Arg
      member __.MatchSopt(_, _) = false
      member __.MatchLopt(_, _) = false
      member __.MatchArg(_) = false
      member __.TryFill(_) = false
    end
  end

type Xor(l':IAst, r':IAst) =
  class
    interface IAst with
      member __.Tag = Tag.Xor
      member __.MatchSopt(_, _) = false
      member __.MatchLopt(_, _) = false
      member __.MatchArg(_) = false
      member __.TryFill(_) = false
    end
  end

type Seq(asts':GList<IAst>) =
  class
    interface IAst with
      member __.Tag = Tag.Seq
      member __.MatchSopt(s', a') =
        Seq.exists (fun (ast':IAst) -> ast'.MatchSopt(s', a')) asts'
      member __.MatchLopt(l', a') =
        Seq.exists (fun (ast':IAst) -> ast'.MatchLopt(l', a')) asts'
      member __.MatchArg(_) = false
      member __.TryFill(args') =
        Seq.forall (fun (ast':IAst) -> ast'.TryFill(args')) asts'
    end
    override __.ToString() = sprintf "Seq %A" (Seq.toList asts')
  end
