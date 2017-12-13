namespace Fake.Net

open System.IO

module FilePath =  
    type FilePath = FilePath of string

    let create (filePath:string) = 
        try
            let fullPath = FilePath (Path.GetFullPath(filePath))
            Ok (fullPath)
        with
        | ex -> 
            let err = sprintf "[%s] %A" filePath ex.Message
            Error [err ]
        
    let value (FilePath e) = e