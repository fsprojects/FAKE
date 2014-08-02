/// Shell-like functions. Similar to [Ruby's FileUtils](http://www.ruby-doc.org/stdlib-2.0.0/libdoc/rake/rdoc/FileUtils.html).
module Fake.FileUtils

open System.IO

/// Deletes a file if it exists
let rm fileName = DeleteFile fileName

/// Like "rm -rf" in a shell. Removes files recursively, ignoring nonexisting files
let rm_rf f = 
    if Directory.Exists f then DeleteDir f
    else DeleteFile f

/// Creates a directory if it doesn't exist.
let mkdir path = CreateDir path

/// <summary>
/// Like "cp -r" in a shell. Copies a file or directory recursively.
/// </summary>
/// <param name="src">The source</param>
/// <param name="dest">The destination</param>
let cp_r src dest = 
    if Directory.Exists src then CopyDir dest src allFiles
    else CopyFile dest src

/// Like "cp" in a shell. Copies a single file.
/// <param name="src">The source</param>
/// <param name="dest">The destination</param>
let cp src dest = CopyFile dest src

/// Changes working directory
let chdir path = Directory.SetCurrentDirectory path

/// Changes working directory
let cd path = chdir path

/// Gets working directory
let pwd = Directory.GetCurrentDirectory

/// The stack of directories operated on by pushd and popd
let dirStack = new System.Collections.Generic.Stack<string>()

/// Store the current directory in the directory stack before changing to a new one
let pushd path = 
    dirStack.Push(pwd())
    cd path

/// Restore the previous directory stored in the stack
let popd () = 
    cd <| dirStack.Pop()

/// Like "mv" in a shell. Moves/renames a file
/// <param name="src">The source</param>
/// <param name="dest">The destination</param>
let mv src dest = MoveFile src dest
