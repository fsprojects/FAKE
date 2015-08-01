#r @"../../app/FakeLib/bin/Debug/FakeLib.dll"

open Fake
open Fake.FileHelper
open System
open System.IO
open TimeSignatures

// helper function for processes
let startProcess fileName args =
    let result =
        ExecProcess (fun info ->
            info.FileName <- fileName
            info.WorkingDirectory <- FullName "."
            info.Arguments <- args) (TimeSpan.FromSeconds(120.0))
    if result <> 0 then
        failwithf "Process '%s' failed with exit code '%d'" fileName result

// a generic task generator based on file extensions
let task_gen proc dir_from dir_to ext_from ext_to x =
    let files_in = dir_from </> ("*." + ext_from) |> Include |> Seq.toList
    let files_out = files_in |> List.map (fun filename -> changeExt ext_to filename)
    let uptodate_in = updateMD5 files_in
    traceFAKE "Changed input files: %A" uptodate_in
    let uptodate_out = files_out |> List.filter (fun filename -> not(File.Exists(filename)))
    traceFAKE "Changed output files: %A" uptodate_out
    let uptodate = Set.union (Set.ofList uptodate_in) (Set.ofList uptodate_out)
    traceFAKE "Files needing update: %A" uptodate
    Set.iter (fun file -> ZipFile (changeExt "zip" file) file) uptodate

let task_zip x =
    task_gen (fun from to_ -> ZipFile to_ from) "." "." "fsx" "zip" x

let xml = initTS

Description "Zip changed fsx files"
TargetTemplate task_zip "task_zip" xml

Run "task_zip"
