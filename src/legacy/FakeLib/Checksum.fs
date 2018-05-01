namespace Fake

open System
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]

type ChecksumHashAlgorithm =
| MD5
| SHA1
| SHA256
| SHA384
| SHA512

/// Allow to calculate checksum
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type Checksum =

    /// Calculate the hash of a file. Default hash algorithm used: SHA256
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    static member CalculateFileHash (filePath, ?hashAlgorithm) =
        let hashAlgorithm = defaultArg hashAlgorithm ChecksumHashAlgorithm.SHA256

        let hashAlgorithmToString x =
            match Microsoft.FSharp.Reflection.FSharpValue.GetUnionFields(x, typeof<ChecksumHashAlgorithm>) with
            | case, _ -> case.Name

        use hashImp = System.Security.Cryptography.HashAlgorithm.Create(hashAlgorithmToString(hashAlgorithm))
        use stream = System.IO.File.OpenRead(filePath)
        let hash = hashImp.ComputeHash(stream);
        BitConverter.ToString(hash).Replace("-", String.Empty)

    /// Check if the hash in parameter is the valid hash of the file
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    static member CheckFileHash (filepath, hash, ?hashAlgorithm) =
        let hashAlgorithm = defaultArg hashAlgorithm ChecksumHashAlgorithm.SHA256

        hash = Checksum.CalculateFileHash(filepath, hashAlgorithm)
