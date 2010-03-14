[<AutoOpen>]
module Fake.WiXHelper

open System.IO

let mutable fileCount = 0

let wixFile (fi:FileInfo) =
    fileCount <- fileCount + 1
    sprintf "<File Id=\"fi_%d\" Name=\"%s\" Source=\"%s\" />" 
      fileCount fi.Name fi.FullName

let rec wixDir (dir:System.IO.DirectoryInfo) =
    let dirs =
      dir.GetDirectories()
        |> Seq.map wixDir
        |> separated ""

    let files =
      dir.GetFiles()
        |> Seq.map wixFile
        |> separated ""

    let compo =
      if files = "" then "" else
      sprintf "<Component Id=\"%s\" Guid=\"%s\">%s</Component>" dir.Name (System.Guid.NewGuid().ToString()) files

    sprintf "<Directory Id=\"%s\" Name=\"%s\">%s%s</Directory>" dir.Name dir.Name dirs compo

let rec wixComponentRefs (dir:DirectoryInfo) =
    let compos =
      dir.GetDirectories()
        |> Seq.map wixComponentRefs
        |> separated ""

    if dir.GetFiles().Length > 0 then sprintf "%s<ComponentRef Id=\"%s\"/>" compos dir.Name else compos