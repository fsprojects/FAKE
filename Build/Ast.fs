namespace Docopt
#nowarn "62"
#light "off"

type 'a GList = System.Collections.Generic.List<'a>

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
    static member Reduce = function
    | Sqb(ast, ism)    -> Sqb(Ast.Reduce ast, ism)
    | Req(ast)         -> Req(Ast.Reduce ast)
    | Seq(seq) when seq.Count = 1 -> Ast.Reduce seq.[0]
    | Seq(seq) as ast  -> let mutable i = 0 in
                          while i < seq.Count do
                            seq.[i] <- Ast.Reduce seq.[i];
                            i <- i + 1
                          done;
                          ast
    | ast              -> ast
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
