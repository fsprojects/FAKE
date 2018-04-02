namespace Fake.Core.CommandLineParsing

open System
open System.Collections.Generic

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
