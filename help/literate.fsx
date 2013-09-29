(**
Literate programming for F#
===========================

Implementation
--------------

This document is written as a literate F# script file, so the remaining text
is an overview of the implementation. The implementation uses `FSharp.Markdown.dll`
and `FSharp.CodeFormat.dll` to colorize F# source & parse Markdown:
*)

(*** hide ***)
namespace FSharp.Literate
#if INTERACTIVE
#I @"..\tools\FSharp.Formatting\lib\net40"
#r "System.Web.dll"
#r "FSharp.Markdown.dll"
#r "FSharp.CodeFormat.dll"
#endif

open System.IO
open System.Web
open System.Reflection
open System.Collections.Generic

open FSharp.Patterns
open FSharp.CodeFormat
open FSharp.Markdown

(** 
### OutputKind type

The following type defines the two possible output types from literate script:
HTML and LaTeX.

*)

open System
[<RequireQualifiedAccess>]
type OutputKind =
  | Html
  | Latex
  (*[omit:(members omitted)]*)

  /// Name of the format (used as a file extension)
  override x.ToString() = 
    match x with
    | Html -> "html"
    | Latex -> "tex"
  
  /// Format a given document as HTML or LaTeX depending on the current kind
  member x.Format(doc) =
    match x with
    | OutputKind.Html -> Markdown.WriteHtml(doc)
    | OutputKind.Latex -> Markdown.WriteLatex(doc)