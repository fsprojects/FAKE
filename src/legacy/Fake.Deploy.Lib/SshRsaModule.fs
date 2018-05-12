[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Fake.SshRsaModule

open System
open System.IO
open Renci.SshNet
open Renci.SshNet.Security
open System.IO
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]

type PublicKey = 
    { Name : string
      PublicKey : byte []
      UserId : string }

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type PrivateKey(privateKey : HostAlgorithm) = 
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member this.Sign data = privateKey.Sign(data)
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member this.Verify data signature = privateKey.VerifySignature(data, signature)

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let loadPrivateKey (pathToKeyFile : string) (password : string) = 
    let pk = new PrivateKeyFile(pathToKeyFile, password)
    PrivateKey(pk.HostKey)

// Format:
// ssh-rsa base_64_encoded_key username
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let parsePublicKey (publicKey : string) = 
    let cols = publicKey.Split([| ' ' |])
    { Name = cols.[0]
      PublicKey = Convert.FromBase64String(cols.[1])
      UserId = cols.[2] }

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let loadPublicKeys fileName = 
    let lines = File.ReadAllLines fileName
    lines |> Array.map parsePublicKey
    
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let verifySignature data signature (publicKey : PublicKey) = 
    let pk = new KeyHostAlgorithm(publicKey.Name, new RsaKey(), publicKey.PublicKey)
    pk.VerifySignature(data, signature)
