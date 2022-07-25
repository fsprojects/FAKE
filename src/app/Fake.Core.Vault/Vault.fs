namespace Fake.Core

#nowarn "44"

/// Provides a encrypted store of variables to prevent accidental leakage 
/// Please read the [documentation](/core-vault.html)
[<RequireQualifiedAccess>]
module Vault =
    open System
    open System.IO
    open System.Security.Cryptography
    open System.Collections.Generic
    open Newtonsoft.Json

    let private aesCtrTransform(key:byte[], salt:byte[], inputStream:Stream, outputStream:Stream) =
        // https://stackoverflow.com/a/51188472/1269722
        let aes = new AesManaged(Mode = CipherMode.ECB, Padding = PaddingMode.None)
        let blockSize = aes.BlockSize / 8

        if (salt.Length <> blockSize) then
            raise <|
                ArgumentException(
                    String.Format(
                        "Salt size must be same as block size (actual: {0}, expected: {1})",
                        salt.Length, blockSize))

        let counter = salt.Clone() :?> byte[]
        let increaseCounter () =
            // https://security.stackexchange.com/questions/4606/is-this-how-to-implement-ctr-around-a-system-that-only-implements-cbc-cfb-cts
            seq {
                for i2 in counter.Length - 1 .. -1 .. 0  do
                    counter[i2] <- counter[i2] + 1uy
                    if (counter[i2] <> 0uy) then
                        yield ()
            } |> Seq.tryItem 0 |> ignore

        let xorMask = Queue<byte>()

        let zeroIv : byte[] = Array.zeroCreate blockSize
        let counterEncryptor = aes.CreateEncryptor(key, zeroIv)

        let mutable b = 1;
        while ((b <- inputStream.ReadByte(); b) <> -1) do
            if (xorMask.Count = 0) then
                let counterModeBlock : byte[] = Array.zeroCreate blockSize

                counterEncryptor.TransformBlock(
                    counter, 0, counter.Length, counterModeBlock, 0)
                    |> ignore

                increaseCounter()
                for b2 in counterModeBlock do
                    xorMask.Enqueue(b2)

            let mask = xorMask.Dequeue()
            outputStream.WriteByte(byte ((byte b) ^^^ mask))

    let private aesCtrTransformBytes (key:byte[], iv:byte[], inputBytes:byte[]) : byte[] =
        use mem1 = new MemoryStream(inputBytes)
        use mem2 = new MemoryStream()
        aesCtrTransform(key, iv, mem1, mem2)
        mem2.ToArray()

    /// The key used to decrypt/encrypt variables
    type KeyInfo =
        { /// The file with the key (32 byte as base64 encoded string saved in utf-8)
          KeyFile : string
          /// The IV-Bytes (16 byte as base64 encoded string)
          Iv : string }

    /// decrypt a given base64 encoded string and return the utf-8 string of the result
    ///
    /// ## Parameters
    ///  - `key` - The file with the key
    ///  - `base64Val` - The base64 encoded value to decrypt 
    let decryptVariable (key:KeyInfo) (base64Val:string) =
        let keyBytes = Convert.FromBase64String(File.ReadAllText(key.KeyFile))
        let ivBytes = Convert.FromBase64String(key.Iv)
        let exampleBytes = Convert.FromBase64String(base64Val)
        System.Text.Encoding.UTF8.GetString(aesCtrTransformBytes(keyBytes, ivBytes, exampleBytes))

    /// encrypt the given utf-8 string and return the base64 encoded result
    ///
    /// ## Parameters
    ///  - `key` - The file with the key
    ///  - `value` - The utf-8 value to encrypt 
    let encryptVariable (key:KeyInfo) (value:string) =
        let keyBytes = Convert.FromBase64String(File.ReadAllText(key.KeyFile))
        let ivBytes = Convert.FromBase64String(key.Iv)
        let exampleBytes = System.Text.Encoding.UTF8.GetBytes(value)
        Convert.ToBase64String(aesCtrTransformBytes(keyBytes, ivBytes, exampleBytes))

    /// The vault variable type
    type Variable = 
        {
          /// Mark variable as being a secret or not
          Secret : bool
          
          /// The variable value
          Value : string
          
          /// The variable name
          Name : string }

    /// Variable group type
    [<Obsolete "Need to be public because of Newtonsoft.Json, don't use">]
    type Variables =
        {
          /// The key file to use for encryption/decryption operations of variable group
          keyFile : String
          
          /// The IV-Bytes (16 byte as base64 encoded string)
          iv : string
          
          /// The variables info in variable group
          values : Variable[] }

    /// The vault which stores the encrypts values
    type Vault =
        internal { 
          Key : KeyInfo
          Variables : Map<string, Variable> }

    /// Read a vault from the given encrypted variables
    ///
    /// ## Parameters
    ///  - `key` - The encryption/decryption key
    ///  - `vars` - The vault variables
    let fromEncryptedVariables key vars =
        let variables =
            vars
            |> Seq.map (fun variable -> variable.Name, variable)
            |> Map.ofSeq
        { Key = key; Variables = variables }

    /// Create a new key with the given path to the secret file (or Path.GetTempFileName() otherwise)
    ///
    /// ## Parameters
    ///  - `file` - The secret file to use to create the key
    let createKey (file : string option) =
        let rnd = new RNGCryptoServiceProvider()
        let iv = Array.zeroCreate 16
        rnd.GetBytes(iv)
        let key = Array.zeroCreate 32
        rnd.GetBytes(key)
        let keyFile = 
            match file with
            | Some f -> f
            | None -> Path.GetTempFileName()
        File.WriteAllText(keyFile, Convert.ToBase64String(key))
        { KeyFile = keyFile; Iv = Convert.ToBase64String(iv) }

    /// An empty vault without any variables
    let empty = { Key = { KeyFile = null; Iv = null }; Variables = Map.empty }

    /// Read in a vault from a given json string, make sure to delete the source of the json after using this API
    ///
    /// ## Parameters
    ///  - `str` - The JSON string of the vault to read
    let fromJson str =
        let vars = JsonConvert.DeserializeObject<Variables>(str)
        fromEncryptedVariables { KeyFile = vars.keyFile; Iv = vars.iv } vars.values

    /// Read a vault from an environment variable.
    ///
    /// ## Parameters
    ///  - `envVar` - The environment variable to use
    let fromEnvironmentVariable envVar =
        let result = Environment.GetEnvironmentVariable(envVar)
        if String.IsNullOrEmpty result then
            empty
        else        
            let vault = fromJson result
            Environment.SetEnvironmentVariable(envVar, null)
            vault

    /// Read a vault from an environment variable or return None
    ///
    /// ## Parameters
    ///  - `envVar` - The environment variable to use
    let fromEnvironmentVariableOrNone envVar =
        let vars = Environment.GetEnvironmentVariable envVar
        if String.IsNullOrEmpty vars then
            None
        else Some (fromEnvironmentVariable envVar)

    /// Read a vault from `FAKE_VAULT_VARIABLES`
    let fromFakeEnvironmentOrNone() =
        fromEnvironmentVariableOrNone "FAKE_VAULT_VARIABLES"

    /// Read a vault from `FAKE_VAULT_VARIABLES`
    let fromFakeEnvironmentVariable() =
        fromEnvironmentVariable "FAKE_VAULT_VARIABLES"
    
    /// Retrieve the value from a given variable
    let private fromVariable (key:KeyInfo) (v:Variable) =
        if v.Secret then
            decryptVariable key v.Value
        else v.Value       

    /// try to retrieve the variable with the given name from the vault (decrypts the variable if needed)
    ///
    /// ## Parameters
    ///  - `name` - The variable name to retrieve
    ///  - `v` - The vault to check
    let tryGet name (v:Vault) =
        v.Variables
        |> Map.tryFind name
        |> Option.map (fromVariable v.Key)

    /// similar to tryGet but throws an exception if the variable with the given name is not available in the vault
    ///
    /// ## Parameters
    ///  - `name` - The variable name to retrieve
    ///  - `v` - The vault to check
    let get name (v:Vault) =
        match tryGet name v with
        | Some s -> s
        | None -> failwithf "Variable '%s' was not found in the vault!" name   

[<AutoOpen>]
module VaultExt =
    type Vault.Vault with
        /// try to retrieve the variable with the given name from the vault (decrypts the variable if needed)
        member x.TryGet name =
            Vault.tryGet name x
    
        /// similar to tryGet but throws an exception if the variable with the given name is not available in the vault    
        member x.Get name =
            Vault.get name x
