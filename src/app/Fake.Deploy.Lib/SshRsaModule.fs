module Fake.SshRsaModule

open System
open System.IO
open Renci.SshNet
open Renci.SshNet.Security
open System.IO

type PublicKey = 
    { Name : string
      PublicKey : byte []
      UserId : string }

type PrivateKey(privateKey : HostAlgorithm) = 
    member this.Sign data = privateKey.Sign(data)
    member this.Verify data signature = privateKey.VerifySignature(data, signature)

let loadPrivateKey (pathToKeyFile : string) (password : string) = 
    let pk = new PrivateKeyFile(pathToKeyFile, password)
    PrivateKey(pk.HostKey)

// Format:
// ssh-rsa base_64_encoded_key username
let parsePublicKey (publicKey : string) = 
    let cols = publicKey.Split([| ' ' |])
    { Name = cols.[0]
      PublicKey = Convert.FromBase64String(cols.[1])
      UserId = cols.[2] }

let loadPublicKeys fileName = 
    let lines = File.ReadAllLines fileName
    lines |> Array.map parsePublicKey

let verifySignature data signature (publicKey : PublicKey) = 
    let pk = new KeyHostAlgorithm(publicKey.Name, new RsaKey(), publicKey.PublicKey)
    pk.VerifySignature(data, signature)
