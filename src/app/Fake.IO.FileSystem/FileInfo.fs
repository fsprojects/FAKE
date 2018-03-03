namespace Fake.IO

open System.IO

[<RequireQualifiedAccess>]
module FileInfo =
    /// Creates a FileInfo for the given path.
    let inline ofPath path = new FileInfo(path)
    
    /// Active Pattern for determining file name.
    let (|FullName|) (f : FileInfo) = f.FullName

    /// Active Pattern for determining FileInfoNameSections.
    let (|NameSections|) (f : FileInfo) = (f.Name, f.Extension, f.FullName)
    
    /// Checks if the two files are byte-to-byte equal.
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

    