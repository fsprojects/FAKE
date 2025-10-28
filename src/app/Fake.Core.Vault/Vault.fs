namespace Fake.Core

#nowarn "44"

/// <summary>
/// Provides a encrypted store of variables to prevent accidental leakage
/// Please read the <a href="articles/core-vault.html">documentation</a>
/// </summary>
[<RequireQualifiedAccess>]
module Vault =
    open System
    open System.IO
    open System.Security.Cryptography
    open System.Collections.Generic
    open Newtonsoft.Json

    let private aesCtrTransform (key: byte[], salt: byte[], inputStream: Stream, outputStream: Stream) =
        // https://stackoverflow.com/a/51188472/1269722
        // Use Aes.Create() instead of deprecated AesManaged
        use aes = Aes.Create(Mode = CipherMode.ECB, Padding = PaddingMode.None)
        let blockSize = aes.BlockSize / 8

        if (salt.Length <> blockSize) then
            raise
            <| ArgumentException(
                String.Format(
                    "Salt size must be same as block size (actual: {0}, expected: {1})",
                    salt.Length,
                    blockSize
                )
            )

        let counter = salt.Clone() :?> byte[]

        let increaseCounter () =
            // https://security.stackexchange.com/questions/4606/is-this-how-to-implement-ctr-around-a-system-that-only-implements-cbc-cfb-cts
            seq {
                for i2 in counter.Length - 1 .. -1 .. 0 do
                    counter[i2] <- counter[i2] + 1uy

                    if (counter[i2] <> 0uy) then
                        yield ()
            }
            |> Seq.tryItem 0
            |> ignore

        let xorMask = Queue<byte>()

        let zeroIv: byte[] = Array.zeroCreate blockSize
        let counterEncryptor = aes.CreateEncryptor(key, zeroIv)

        let mutable b = 1

        while ((b <- inputStream.ReadByte()
                b)
               <> -1) do
            if (xorMask.Count = 0) then
                let counterModeBlock: byte[] = Array.zeroCreate blockSize

                counterEncryptor.TransformBlock(counter, 0, counter.Length, counterModeBlock, 0)
                |> ignore

                increaseCounter ()

                for b2 in counterModeBlock do
                    xorMask.Enqueue(b2)

            let mask = xorMask.Dequeue()
            outputStream.WriteByte(byte ((byte b) ^^^ mask))

    let private aesCtrTransformBytes (key: byte[], iv: byte[], inputBytes: byte[]) : byte[] =
        use mem1 = new MemoryStream(inputBytes)
        use mem2 = new MemoryStream()
        aesCtrTransform (key, iv, mem1, mem2)
        mem2.ToArray()

    /// <summary>
    /// The key used to decrypt/encrypt variables
    /// </summary>
    type KeyInfo =
        {
            /// The file with the key (32 byte as base64 encoded string saved in utf-8)
            KeyFile: string
            /// The IV-Bytes (16 byte as base64 encoded string)
            Iv: string
        }

    /// <summary>
    /// Decrypt a given base64 encoded string and return the utf-8 string of the result
    /// </summary>
    ///
    /// <param name="key">The file with the key</param>
    /// <param name="base64Val">The base64 encoded value to decrypt </param>
    let decryptVariable (key: KeyInfo) (base64Val: string) =
        let keyBytes = Convert.FromBase64String(File.ReadAllText(key.KeyFile))
        let ivBytes = Convert.FromBase64String(key.Iv)
        let exampleBytes = Convert.FromBase64String(base64Val)
        System.Text.Encoding.UTF8.GetString(aesCtrTransformBytes (keyBytes, ivBytes, exampleBytes))

    /// <summary>
    /// Encrypt the given utf-8 string and return the base64 encoded result
    /// </summary>
    ///
    /// <param name="key">The file with the key</param>
    /// <param name="value">The utf-8 value to encrypt </param>
    let encryptVariable (key: KeyInfo) (value: string) =
        let keyBytes = Convert.FromBase64String(File.ReadAllText(key.KeyFile))
        let ivBytes = Convert.FromBase64String(key.Iv)
        let exampleBytes = System.Text.Encoding.UTF8.GetBytes(value)
        Convert.ToBase64String(aesCtrTransformBytes (keyBytes, ivBytes, exampleBytes))

    /// <summary>
    /// The vault variable type
    /// </summary>
    type Variable =
        {
            /// Mark variable as being a secret or not
            Secret: bool

            /// The variable value
            Value: string

            /// The variable name
            Name: string
        }

    /// <summary>
    /// Variable group type
    /// </summary>
    [<Obsolete "Need to be public because of Newtonsoft.Json, don't use">]
    type Variables =
        {
            /// The key file to use for encryption/decryption operations of variable group
            keyFile: String

            /// The IV-Bytes (16 byte as base64 encoded string)
            iv: string

            /// The variables info in variable group
            values: Variable[]
        }

    /// <summary>
    /// The vault which stores the encrypts values
    /// </summary>
    type Vault =
        internal
            { Key: KeyInfo
              Variables: Map<string, Variable> }

    /// <summary>
    /// Read a vault from the given encrypted variables
    /// </summary>
    ///
    /// <param name="key">The encryption/decryption key</param>
    /// <param name="vars">The vault variables</param>
    let fromEncryptedVariables key vars =
        let variables =
            vars |> Seq.map (fun variable -> variable.Name, variable) |> Map.ofSeq

        { Key = key; Variables = variables }

    /// <summary>
    /// Create a new key with the given path to the secret file (or <c>Path.GetTempFileName()</c> otherwise)
    /// </summary>
    ///
    /// <param name="file">The secret file to use to create the key</param>
    let createKey (file: string option) =
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

        { KeyFile = keyFile
          Iv = Convert.ToBase64String(iv) }

    /// An empty vault without any variables
    let empty =
        { Key = { KeyFile = null; Iv = null }
          Variables = Map.empty }

    /// <summary>
    /// Read a vault from a JSON string
    /// </summary>
    ///
    /// <param name="str">The JSON string of the vault to read</param>
    let fromJson str =
        // Secure JSON deserialization: disable type name handling to prevent deserialization attacks
        let settings = JsonSerializerSettings(TypeNameHandling = TypeNameHandling.None)
        let vars = JsonConvert.DeserializeObject<Variables>(str, settings)
        fromEncryptedVariables { KeyFile = vars.keyFile; Iv = vars.iv } vars.values

    /// <summary>
    /// Read a vault from an environment variable.
    /// </summary>
    ///
    /// <param name="envVar">The environment variable to use</param>
    let fromEnvironmentVariable envVar =
        let result = Environment.GetEnvironmentVariable(envVar)

        if String.IsNullOrEmpty result then
            empty
        else
            let vault = fromJson result
            Environment.SetEnvironmentVariable(envVar, null)
            vault

    /// <summary>
    /// Read a vault from an environment variable or return None
    /// </summary>
    ///
    /// <param name="envVar">The environment variable to use</param>
    let fromEnvironmentVariableOrNone envVar =
        let vars = Environment.GetEnvironmentVariable envVar

        if String.IsNullOrEmpty vars then
            None
        else
            Some(fromEnvironmentVariable envVar)

    /// <summary>
    /// Read a vault from <c>FAKE_VAULT_VARIABLES</c>
    /// </summary>
    let fromFakeEnvironmentOrNone () =
        fromEnvironmentVariableOrNone "FAKE_VAULT_VARIABLES"

    /// <summary>
    /// Read a vault from <c>FAKE_VAULT_VARIABLES</c>
    /// </summary>
    let fromFakeEnvironmentVariable () =
        fromEnvironmentVariable "FAKE_VAULT_VARIABLES"

    /// <summary>
    /// Retrieve the value from a given variable
    /// </summary>
    ///
    /// <param name="key">The variable info</param>
    /// <param name="v">The variable</param>
    let private fromVariable (key: KeyInfo) (v: Variable) =
        if v.Secret then decryptVariable key v.Value else v.Value

    /// <summary>
    /// Try to retrieve the variable with the given name from the vault (decrypts the variable if needed)
    /// </summary>
    ///
    /// <param name="name">The variable name to retrieve</param>
    /// <param name="v">The vault to check</param>
    let tryGet name (v: Vault) =
        v.Variables |> Map.tryFind name |> Option.map (fromVariable v.Key)

    /// similar to tryGet but throws an exception if the variable with the given name is not available in the vault
    ///
    /// <param name="name">The variable name to retrieve
    /// <param name="v">The vault to check
    let get name (v: Vault) =
        match tryGet name v with
        | Some s -> s
        | None -> failwithf "Variable '%s' was not found in the vault!" name

/// Summary>
/// An extenstion type for Vault module.
/// </summary>
[<AutoOpen>]
module VaultExt =
    type Vault.Vault with

        /// try to retrieve the variable with the given name from the vault (decrypts the variable if needed)
        member x.TryGet name = Vault.tryGet name x

        /// similar to tryGet but throws an exception if the variable with the given name is not available in the vault
        member x.Get name = Vault.get name x
