module Fake.FileUtils

open System.IO

// Shell-like functions. 
// Similar to Ruby's FileUtils: http://ruby-doc.org/core/classes/FileUtils.htm

let private flip f x y = f y x
let rm = DeleteFile
let rm_rf f = 
    if Directory.Exists f
        then DeleteDir f
        else DeleteFile f
let mkdir = CreateDir
let cp_r src dest = 
    if Directory.Exists src
        then CopyDir dest src allFiles
        else CopyFile dest src
let cp = flip CopyFile
let chdir = Directory.SetCurrentDirectory
let cd = chdir
let pwd = Directory.GetCurrentDirectory

(*
TODO: 
 * mv
 * touch
 * symlink: http://community.bartdesmet.net/blogs/bart/archive/2006/10/24/Windows-Vista-_2D00_-Creating-symbolic-links-with-C_2300_.aspx
 * hardlink (junction): http://www.codeproject.com/KB/files/JunctionPointsNet.aspx
 * change attributes
 * change permissions
*)