[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module Fake.SshRsaModule

open System
open System.IO
open Renci.SshNet
open Renci.SshNet.Security
open System.IO
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]

type PublicKey = 
    { Name : string
      PublicKey : byte []
      UserId : string }

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type PrivateKey(privateKey : HostAlgorithm) = 
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member this.Sign data = privateKey.Sign(data)
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member this.Verify data signature = privateKey.VerifySignature(data, signature)

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let loadPrivateKey (pathToKeyFile : string) (password : string) = 
    let pk = new PrivateKeyFile(pathToKeyFile, password)
    PrivateKey(pk.HostKey)

// Format:
// ssh-rsa base_64_encoded_key username
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let parsePublicKey (publicKey : string) = 
    let cols = publicKey.Split([| ' ' |])
    { Name = cols.[0]
      PublicKey = Convert.FromBase64String(cols.[1])
      UserId = cols.[2] }

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let loadPublicKeys fileName = 
    let lines = File.ReadAllLines fileName
    lines |> Array.map parsePublicKey
    
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let verifySignature data signature (publicKey : PublicKey) = 
    let pk = new KeyHostAlgorithm(publicKey.Name, new RsaKey(), publicKey.PublicKey)
    pk.VerifySignature(data, signature)
