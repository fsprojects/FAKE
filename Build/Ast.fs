namespace Docopt
#nowarn "62"
#light "off"

type private 'a GList = System.Collections.Generic.List<'a>

type (*private*) Tag =
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
  end

type Sop(o':Options) =
  class
    let matched = GList<Option * string>(o'.Count)
    interface IAst with
      member __.Tag = Tag.Sop
      member __.MatchSopt(s', getArg') = 
        let mutable ret = true in
        let mutable i = 0 in
        while i < s'.Length do
          (match o'.FindAndRemove(s'.[i]) with
           | null -> ret <- false; i <-s'.Length
           | opt  -> matched.Add(opt, if opt.HasArgument && i = s'.Length - 1
                                      then getArg' opt.ArgName
                                      elif opt.HasArgument
                                      then (let j = i + 1 in
                                            i <- s'.Length;
                                            s'.Substring(j))
                                      else null));
          i <- i + 1
        done;
        ret
      member __.MatchLopt(_, _) = false
      member __.MatchArg(_) = false
      member __.TryFill(_) = false
    end
  end

type Ano(o':Options) =
  class
    let matched = GList<Option * string>(o'.Count)
    interface IAst with
      member __.Tag = Tag.Ano
      member __.MatchSopt(s', getArg') =
        let mutable ret = true in
        let mutable i = 0 in
        while i < s'.Length do
          (match o'.Find(s'.[i]) with
           | null -> ret <- false; i <-s'.Length
           | opt  -> matched.Add(opt, if opt.HasArgument && i = s'.Length - 1
                                      then getArg' opt.ArgName
                                      elif opt.HasArgument
                                      then (let j = i + 1 in
                                            i <- s'.Length;
                                            s'.Substring(j))
                                      else null));
          i <- i + 1
        done;
        ret
      member __.MatchLopt(_, _) = false
      member __.MatchArg(_) = false
      member __.TryFill(_) = false
    end
  end

type Sqb(ast':IAst) =
  class
    interface IAst with
      member __.Tag = Tag.Sqb
      member __.MatchSopt(_, _) = false
      member __.MatchLopt(_, _) = false
      member __.MatchArg(_) = false
      member __.TryFill(_) = false
    end
  end

type Req(ast':IAst) =
  class
    interface IAst with
      member __.Tag = Tag.Req
      member __.MatchSopt(_, _) = false
      member __.MatchLopt(_, _) = false
      member __.MatchArg(_) = false
      member __.TryFill(_) = false
    end
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

type Seq(ast':GList<IAst>) =
  class
    interface IAst with
      member __.Tag = Tag.Seq
      member __.MatchSopt(_, _) = false
      member __.MatchLopt(_, _) = false
      member __.MatchArg(_) = false
      member __.TryFill(_) = false
    end
  end

(*
[<NoComparison>]
type Ast =
  | Eps
  | Ano of Options
  | Sop of Options
  | Sqb of Ast * bool ref  // bool holds if the list has been matched
  | Req of Ast
  | Arg of string ref
  | Cmd of string
  | Ell of Ast * bool ref
  | Kln of Ast
  | Xor of Ast * Ast
  | Xoq of Ast GList
  | Seq of Ast GList
  with
    static member MatchSopt(s':char, ast':Ast) = match ast' with
    | Ano(ano)      -> ano.Find(s')
    | Sop(sop)      -> sop.FindAndRemove(s')
    | Sqb(ast, ism) -> (match Ast.MatchSopt(s', ast) with
                        | null -> null
                        | opt  -> ism := true;
                                  opt)
    | Req(ast)      -> Ast.MatchSopt(s', ast)
    | Xor(lft, rgt) -> (match Ast.MatchSopt(s', lft),
                              Ast.MatchSopt(s', rgt) with
                        | null, null -> null
                        | optl, null -> optl
                        | null, optr -> optr
                        | optl, optr -> optl) // Maybe check if optl <> optr
    | Seq(seq)      -> let mutable ret = null in
                       let mutable i = 0 in
                       while i < seq.Count do
                         match Ast.MatchSopt(s', seq.[i]) with
                         | null -> i <- i + 1;
                         | opt  -> ret <- opt;
                                   i <- seq.Count
                       done;
                       ret
    | _             -> null
    static member MatchLopt(l':string, ast':Ast) = match ast' with
    | Ano(ano) -> ano.Find(l')
    | _        -> null
    static member MatchArg = function
    | Sqb(ast, ism) -> (match Ast.MatchArg(ast) with
                        | null -> null
                        | opt  -> ism := true;
                                  opt)
    | Arg(arg)      -> let ret = !arg in
                       arg := null;
                       ret
    | Seq(seq)      -> let mutable ret = null in
                       let mutable i = 0 in
                       while i < seq.Count do
                         match Ast.MatchArg(seq.[i]) with
                         | null -> i <- i + 1;
                         | arg  -> ret <- arg;
                                   i <- seq.Count
                       done;
                       ret
    | _             -> null
    static member Success = function
    | Eps
    | Ano(_)        -> true
    | Req(ast)      -> Ast.Success ast
    | Arg(arg)      -> !arg = null
    | Sop(sop)      -> sop.Count = 0
    | Xor(lft, rgt) -> let l = Ast.Success lft in
                       let r = Ast.Success rgt in
                       l <> r
    | Seq(seq)      -> seq.Count = 0 || Seq.forall (Ast.Success) seq
    | Sqb(ast, ism) -> (match ast with
                        | Seq(_) -> true
                        | _      -> Ast.Success ast || not !ism) // A←B
    | _             -> false
    member xx.IsSopCase = match xx with Sop(_) -> true | _ -> false
  end
*)
