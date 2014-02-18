/// Shell-like functions. Similar to [Ruby's FileUtils](http://www.ruby-doc.org/stdlib-2.0.0/libdoc/rake/rdoc/FileUtils.html).
module Fake.FileUtils

open System.IO

let private flip f x y = f y x

/// Deletes a file if it exists
let rm = DeleteFile

/// Like "rm -rf" in a shell. Removes files recursively, ignoring nonexisting files
let rm_rf f = 
    if Directory.Exists f then DeleteDir f
    else DeleteFile f

/// Creates a directory if it doesn't exist.
let mkdir = CreateDir

/// <summary>
/// Like "cp -r" in a shell. Copies a file or directory recursively.
/// </summary>
/// <param name="src">The source</param>
/// <param name="dest">The destination</param>
let cp_r src dest = 
    if Directory.Exists src then CopyDir dest src allFiles
    else CopyFile dest src

/// Like "cp" in a shell. Copies a single file.
let cp = flip CopyFile

/// Changes working directory
let chdir = Directory.SetCurrentDirectory

/// Changes working directory
let cd = chdir

/// Gets working directory
let pwd = Directory.GetCurrentDirectory

/// Like "mv" in a shell. Moves/renames a file
let mv = flip MoveFile