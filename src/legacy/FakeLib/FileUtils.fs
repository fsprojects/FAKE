/// Shell-like functions. Similar to [Ruby's FileUtils](http://www.ruby-doc.org/stdlib-2.0.0/libdoc/rake/rdoc/FileUtils.html).

[<System.Obsolete("FAKE0001 Use `open Fake.IO` and `FileSystem.Shell`")>]
module Fake.FileUtils

open System.IO

/// Deletes a file if it exists
[<System.Obsolete("FAKE0001 Use `open Fake.IO` and `Shell.rm`")>]
let rm fileName = DeleteFile fileName

/// Like "rm -rf" in a shell. Removes files recursively, ignoring nonexisting files
[<System.Obsolete("FAKE0001 Use `open Fake.IO` and `Shell.rm_rf`")>]
let rm_rf f = 
    if Directory.Exists f then DeleteDir f
    else DeleteFile f

/// Creates a directory if it doesn't exist.
[<System.Obsolete("FAKE0001 Use `open Fake.IO` and `Shell.mkdir`")>]
let mkdir path = CreateDir path

/// <summary>
/// Like "cp -r" in a shell. Copies a file or directory recursively.
/// </summary>
/// <param name="src">The source</param>
/// <param name="dest">The destination</param>
[<System.Obsolete("FAKE0001 Use `open Fake.IO` and `Shell.cp_r`")>]
let cp_r src dest = 
    if Directory.Exists src then CopyDir dest src allFiles
    else CopyFile dest src

/// Like "cp" in a shell. Copies a single file.
/// <param name="src">The source</param>
/// <param name="dest">The destination</param>
[<System.Obsolete("FAKE0001 Use `open Fake.IO` and `Shell.cp`")>]
let cp src dest = CopyFile dest src

/// Changes working directory
[<System.Obsolete("FAKE0001 Use `open Fake.IO` and `Shell.chdir`")>]
let chdir path = Directory.SetCurrentDirectory path

/// Changes working directory
[<System.Obsolete("FAKE0001 Use `open Fake.IO` and `Shell.cd`")>]
let cd path = chdir path

/// Gets working directory
[<System.Obsolete("FAKE0001 Use `open Fake.IO` and `Shell.pwd`")>]
let pwd = Directory.GetCurrentDirectory

/// The stack of directories operated on by pushd and popd
[<System.Obsolete("FAKE0003 Please open an issue if you used this API")>]
let dirStack = new System.Collections.Generic.Stack<string>()

/// Store the current directory in the directory stack before changing to a new one
[<System.Obsolete("FAKE0001 Use `open Fake.IO` and `Shell.pushd`")>]
let pushd path = 
    dirStack.Push(pwd())
    cd path

/// Restore the previous directory stored in the stack
[<System.Obsolete("FAKE0001 Use `open Fake.IO` and `Shell.popd`")>]
let popd () = 
    cd <| dirStack.Pop()

/// Like "mv" in a shell. Moves/renames a file
/// <param name="src">The source</param>
/// <param name="dest">The destination</param>
[<System.Obsolete("FAKE0001 Use `open Fake.IO` and `Shell.mv`")>]
let mv src dest = MoveFile src dest
