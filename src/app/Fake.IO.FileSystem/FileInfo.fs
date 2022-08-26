namespace Fake.IO

open System.IO

/// <summary>
/// Contains tasks to interact with <c>FileInfo</c>
/// </summary>
[<RequireQualifiedAccess>]
module FileInfo =
    /// <summary>
    /// Creates a FileInfo for the given path.
    /// </summary>
    /// 
    /// <param name="path">Create an instance of `FileInfo` from given path</param>
    let inline ofPath path = FileInfo(path)
    
    /// <summary>
    /// Active Pattern for determining file name.
    /// </summary>
    /// 
    /// <param name="f"><c>FileInfo</c> to operate on</param>
    let (|FullName|) (f : FileInfo) = f.FullName

    /// <summary>
    /// Active Pattern for determining FileInfoNameSections.
    /// </summary>
    /// 
    /// <param name="f"><c>FileInfo</c> to operate on</param>
    let (|NameSections|) (f : FileInfo) = (f.Name, f.Extension, f.FullName)
    
    /// <summary>
    /// Checks if the two files are byte-to-byte equal.
    /// </summary>
    /// 
    /// <param name="first">First <c>FileInfo</c> to operate on</param>
    /// <param name="second">Second <c>FileInfo</c> to operate on</param>
    let contentIsEqualTo (first : FileInfo) (second : FileInfo) = 
        if first.Length <> second.Length then false
        else 
            let BYTES_TO_READ = 32768
            use fs1 = first.OpenRead()
            use fs2 = second.OpenRead()
            let one = Array.create BYTES_TO_READ (byte 0)
            let two = Array.create BYTES_TO_READ (byte 0)
            let mutable eq = true
            while eq && fs1.Read(one, 0, BYTES_TO_READ) <> 0 && fs2.Read(two, 0, BYTES_TO_READ) <> 0 do
                if one <> two then eq <- false
            eq

    
