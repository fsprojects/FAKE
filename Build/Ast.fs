namespace Docopt
#nowarn "62"
#light "off"

type 'a GList = System.Collections.Generic.List<'a>

[<NoComparison>]
type Ast =
  | Eps
  | Ano of Options
  | Sop of Options
  | Sqb of Ast
  | Req of Ast
  | Arg of string
  | Cmd of string
  | Ell of Ast * bool ref
  | Kln of Ast
  | Xor of Ast * Ast
  | Xoq of Ast GList
  | Seq of Ast GList
  | Aon of Ast * bool ref // All Or None, bool holds if the list has been matched
  with
    static member Reduce = function
    | Sqb(ast)         -> (match Ast.Reduce ast with
                            | Req(ast) -> Aon(Ast.Reduce ast, ref false)
                            | ast      -> Sqb(ast))
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
    | Ano(ano) -> ano.Find(s')
    | Sop(sop) -> sop.FindAndRemove(s')
    | Sqb(ast) -> (match Ast.MatchSopt(s', ast) with
                   | null -> Option.Trash
                   | opt  -> opt)
    | Req(ast) -> Ast.MatchSopt(s', ast)
    | Seq(seq) -> let mutable ret = null in
                  let mutable i = 0 in
                  while i < seq.Count do
                    match Ast.MatchSopt(s', seq.[i]) with
                    | null -> i <- i + 1;
                    | opt  -> ret <- opt;
                              i <- seq.Count
                  done;
                  ret
    | _        -> null
    static member MatchLopt(l':string, ast':Ast) = match ast' with
    | Ano(ano) -> ano.Find(l')
    | _        -> null
    static member Success = function
    | Eps
    | Ano(_)
    | Sqb(_)    -> true
    | Req(ast)  -> Ast.Success ast
    | Sop(sop)  -> sop.Count = 0
    | Seq(seq)  -> seq.Count = 0 || Seq.forall (Ast.Success) seq
    | _         -> false
    member xx.IsSopCase = match xx with Sop(_) -> true | _ -> false
  end
