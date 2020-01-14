module Fake.Tools.SignTool.Tests

open System
open System.IO
open Expecto
open Fake.Tools
open FsCheck


let private testRunner (signtoolPath: string) (signtoolArgs: string) (signtoolWorkingDir: string) (signtoolTimeout: TimeSpan option) =
    signtoolPath, signtoolArgs, signtoolWorkingDir, signtoolTimeout

let private testSigntoolexePath =
    "test/path/to/signtool.exe"
let private testSigntoolexeLocator () =
    Some testSigntoolexePath

let private getDefaultWorkingDir =
    Directory.GetCurrentDirectory()


let private getExpectedToolOptions topts =
    match topts with
    | Some t ->
        let toolPath = t.ToolPath |> Option.defaultValue testSigntoolexePath
        let workingDir = t.WorkingDir |> Option.defaultValue getDefaultWorkingDir
        toolPath, workingDir, t.Timeout
    | None ->
        testSigntoolexePath, getDefaultWorkingDir, None

let private expectIfOption c (o: bool option) m args =
    if o.IsSome && o.Value then
        Expect.stringContains (args + " ") (sprintf " %s " c) (sprintf "Option '%s' was set but arguments do not contain '%s'" m c)
    args
let private expectIfString c (o: string option) m args =
    if o.IsSome then
        Expect.stringContains (args + " ") (sprintf " %s \"%s\" " c o.Value) (sprintf "Option '%s' was set but arguments do not contain '%s \"%s\"'" m c o.Value)
    args
let private expectIfInt c (o: int option) m args =
    if o.IsSome then
        Expect.stringContains (args + " ") (sprintf " %s %i " c o.Value) (sprintf "Option '%s' was set but arguments do not contain '%s %i'" m c o.Value)
    args
let private expectIfEnum c (o: 'a option) (v: 'a) m args =
    if o.IsSome && o.Value = v then
        Expect.stringContains (args + " ") (sprintf " %s " c) (sprintf "Option '%s' was set but arguments do not contain '%s'" m c)
    args


let private quoteAndJoinWithSpace xs =
    String.Join(" ", List.map (sprintf "\"%s\"") xs)


type FilesList = string list
type SignToolArbitrary =
    static member String () =
        { new Arbitrary<String>() with
            override this.Generator = gen {
                let! c = Gen.choose (3, 23)
                let chars =
                    Arb.generate<char>
                    |> Gen.sample c c
                    |> Array.ofList
                return new string(chars) } }
    static member FilesList () =
        { new Arbitrary<FilesList>() with
            override this.Generator = gen {
                let! s = Gen.choose (1, 5)
                let! c = Gen.choose (1, 5)
                return Gen.elements ["file1"; "subfolder"; "path"; "file2"; "to"; "file3"; ".ext"]
                    |> Gen.nonEmptyListOf
                    |> Gen.sample s c
                    |> List.map (fun l -> String.Join(Path.DirectorySeparatorChar, l)) } }

let private signtoolTestConfig = { FsCheckConfig.defaultConfig with arbitrary = [ typeof<SignToolArbitrary> ] }


[<Tests>]
let tests =
    let checkSignOptions (signOptions: SignOptions) signFiles signtool additionalChecks =
        let expectedSigntoolPath, expectedSigntoolWorkingDir, expectedSigntoolTimeout =
            getExpectedToolOptions signOptions.ToolOptions
        let actualSigntoolPath, (actualSigntoolArgs: string), actualSigntoolWorkingDir, actualSigntoolTimeout = signtool ()

        Expect.equal actualSigntoolPath expectedSigntoolPath "Expected correct signtool.exe path"
        Expect.equal actualSigntoolWorkingDir expectedSigntoolWorkingDir "Expected correct working directory"
        Expect.equal actualSigntoolTimeout expectedSigntoolTimeout "Expected correct timeout"

        Expect.stringStarts actualSigntoolArgs "sign" "Expected arguments to start with 'sign'"
        Expect.isFalse (actualSigntoolArgs.EndsWith(' ')) "Expected arguments to not end with a space"
        actualSigntoolArgs
        |> expectIfOption "/debug" signOptions.Debug "Debug"
        |> expectIfEnum "/v" signOptions.Verbosity SignTool.Verbosity.Verbose "Verbosity.Verbose"
        |> expectIfEnum "/q" signOptions.Verbosity SignTool.Verbosity.Quiet "Verbosity.Quiet"
        |> (fun args ->
            match signOptions.Certificate with
            | SignTool.SignCertificate.File f ->
                Expect.stringContains args (sprintf "/f \"%s\"" f.Path) "Expected arguments to contain certificate 'Path'"
                args
                |> expectIfString "/p" f.Password "Password"
                |> expectIfString "/csp" f.CspName "CspName"
                |> expectIfString "/kc" f.PrivateKeyKey "PrivateKeyKey"
            | SignTool.SignCertificate.Store s ->
                args
                |> expectIfOption "/a" s.AutomaticallySelectCertificate "AutomaticallySelectCertificate"
                |> expectIfString "/i" s.IssuerName "IssuerName"
                |> expectIfString "/n" s.SubjectName "SubjectName"
                |> expectIfString "/r" s.RootSubjectName "RootSubjectName"
                |> expectIfString "/s" s.StoreName "StoreName"
                |> expectIfString "/sha1" s.Hash "Hash"
                |> expectIfOption "/sm" s.UseComputerStore "UseComputerStore" )
        |> expectIfEnum "/fd \"sha1\"" signOptions.DigestAlgorithm SignTool.DigestAlgorithm.SHA1 "DigestAlgorithm.SHA1"
        |> expectIfEnum "/fd \"sha256\"" signOptions.DigestAlgorithm SignTool.DigestAlgorithm.SHA256 "DigestAlgorithm.SHA256"
        |> expectIfString "/ac" signOptions.AdditionalCertificate "AdditionalCertificate"
        |> expectIfOption "/as" signOptions.AppendSignature "AppendSignature"
        |> expectIfString "/c" signOptions.CertificateTemplateName "CertificateTemplateName"
        |> expectIfString "/d" signOptions.Description "Description"
        |> expectIfString "/u" signOptions.EnhancedKeyUsage "EnhancedKeyUsage"
        |> expectIfOption "/uw" signOptions.EnhancedKeyUsageW "EnhancedKeyUsageW"
        |> ignore
        Expect.stringEnds actualSigntoolArgs (" " + (quoteAndJoinWithSpace signFiles)) "Expected arguments to end with a quoted space-separated list of files"
        additionalChecks actualSigntoolArgs

    testList "Fake.Tools.SignTool.Tests" [
        testCase "default sign options" <| fun _ ->
            let certificate = SignTool.SignCertificate.File (SignTool.CertificateFromFile.Create("path/to/certificate.pfx"))
            let signOptions = SignTool.SignOptions.Create(certificate)
            let signFiles = ["file1.ext"; "file2.ext"]
            let actualSigntoolPath, actualSigntoolArgs, actualSigntoolWorkingDir, actualSigntoolTimeout =
                SignTool.signInternal testRunner testSigntoolexeLocator signOptions signFiles

            Expect.equal actualSigntoolPath testSigntoolexePath "Expected correct signtool.exe path"
            Expect.equal actualSigntoolArgs "sign /f \"path/to/certificate.pfx\" /fd \"sha256\" \"file1.ext\" \"file2.ext\"" "Expected correct arguments"
            Expect.equal actualSigntoolWorkingDir (Directory.GetCurrentDirectory()) "Expected correct working directory"
            Expect.isNone actualSigntoolTimeout "Expected no timeout"

        testPropertyWithConfig signtoolTestConfig "sign options" <| fun (signOptions: SignTool.SignOptions) (signFiles: FilesList) ->
            checkSignOptions
                signOptions
                signFiles
                (fun () -> SignTool.signInternal testRunner testSigntoolexeLocator signOptions signFiles)
                (fun args ->
                    Expect.isFalse (args.Contains(" /t ")) "Expected arguments not to contain time stamping option /t"
                    Expect.isFalse (args.Contains(" /tr ")) "Expected arguments not to contain time stamping option /tr"
                    Expect.isFalse (args.Contains(" /td ")) "Expected arguments not to contain time stamping option /td" )

        testCase "default sign with time stamp options" <| fun _ ->
            let certificate = SignTool.SignCertificate.File (SignTool.CertificateFromFile.Create("path/to/certificate.pfx"))
            let signOptions = SignTool.SignOptions.Create(certificate)
            let timeStampOptions = SignTool.TimeStampOption.Create("http://timestamp.example-ca.com/")
            let signFiles = ["file1.ext"; "file2.ext"]
            let actualSigntoolPath, actualSigntoolArgs, actualSigntoolWorkingDir, actualSigntoolTimeout =
                SignTool.signWithTimeStampInternal testRunner testSigntoolexeLocator signOptions timeStampOptions signFiles

            Expect.equal actualSigntoolPath testSigntoolexePath "Expected correct signtool.exe path"
            Expect.equal actualSigntoolArgs "sign /f \"path/to/certificate.pfx\" /fd \"sha256\" /tr \"http://timestamp.example-ca.com/\" /td \"sha256\" \"file1.ext\" \"file2.ext\"" "Expected correct arguments"
            Expect.equal actualSigntoolWorkingDir (Directory.GetCurrentDirectory()) "Expected correct working directory"
            Expect.isNone actualSigntoolTimeout "Expected no timeout"

        testPropertyWithConfig signtoolTestConfig "sign with time stamp options" <| fun (signOptions: SignTool.SignOptions) (timeStampOptions: SignTool.TimeStampOption) (signFiles: FilesList) ->
            checkSignOptions
                signOptions
                signFiles
                (fun () -> SignTool.signWithTimeStampInternal testRunner testSigntoolexeLocator signOptions timeStampOptions signFiles)
                (fun args ->
                    args
                    |> expectIfEnum (sprintf "/t \"%s\"" timeStampOptions.ServerUrl) timeStampOptions.Algorithm SignTool.DigestAlgorithm.SHA1 "TimeStamp.Algorithm.SHA1"
                    |> expectIfEnum (sprintf "/tr \"%s\" /td \"sha256\"" timeStampOptions.ServerUrl) timeStampOptions.Algorithm SignTool.DigestAlgorithm.SHA256 "TimeStamp.Algorithm.SHA256"
                    |> ignore )

        testCase "default time stamp options" <| fun _ ->
            let timestampOptions = SignTool.TimeStampOptions.Create("http://timestamp.example-ca.com/")
            let timestampFiles = ["file1.ext"; "file2.ext"]
            let actualSigntoolPath, actualSigntoolArgs, actualSigntoolWorkingDir, actualSigntoolTimeout =
                SignTool.timeStampInternal testRunner testSigntoolexeLocator timestampOptions timestampFiles

            Expect.equal actualSigntoolPath testSigntoolexePath "Expected correct signtool.exe path"
            Expect.equal actualSigntoolArgs "timestamp /tr \"http://timestamp.example-ca.com/\" /td \"sha256\" \"file1.ext\" \"file2.ext\"" "Expected correct arguments"
            Expect.equal actualSigntoolWorkingDir (Directory.GetCurrentDirectory()) "Expected correct working directory"
            Expect.isNone actualSigntoolTimeout "Expected no timeout"

        testPropertyWithConfig signtoolTestConfig "time stamp options" <| fun (timestampOptions: SignTool.TimeStampOptions) (timestampFiles: FilesList) ->
            let expectedSigntoolPath, expectedSigntoolWorkingDir, expectedSigntoolTimeout =
                getExpectedToolOptions timestampOptions.ToolOptions
            let actualSigntoolPath, actualSigntoolArgs, actualSigntoolWorkingDir, actualSigntoolTimeout =
                SignTool.timeStampInternal testRunner testSigntoolexeLocator timestampOptions timestampFiles

            Expect.equal actualSigntoolPath expectedSigntoolPath "Expected correct signtool.exe path"
            Expect.equal actualSigntoolWorkingDir expectedSigntoolWorkingDir "Expected correct working directory"
            Expect.equal actualSigntoolTimeout expectedSigntoolTimeout "Expected correct timeout"

            Expect.stringStarts actualSigntoolArgs "timestamp" "Expected arguments to start with 'timestamp'"
            Expect.isFalse (actualSigntoolArgs.EndsWith(' ')) "Expected arguments to not end with a space"
            actualSigntoolArgs
            |> expectIfOption "/debug" timestampOptions.Debug "Debug"
            |> expectIfEnum "/v" timestampOptions.Verbosity SignTool.Verbosity.Verbose "Verbosity.Verbose"
            |> expectIfEnum "/q" timestampOptions.Verbosity SignTool.Verbosity.Quiet "Verbosity.Quiet"
            |> expectIfEnum (sprintf "/t \"%s\"" timestampOptions.ServerUrl) timestampOptions.Algorithm SignTool.DigestAlgorithm.SHA1 "TimeStamp.Algorithm.SHA1"
            |> expectIfEnum (sprintf "/tr \"%s\" /td \"sha256\"" timestampOptions.ServerUrl) timestampOptions.Algorithm SignTool.DigestAlgorithm.SHA256 "TimeStamp.Algorithm.SHA256"
            |> expectIfInt "/tp" timestampOptions.TimestampIndex "TimestampIndex"
            |> ignore
            Expect.stringEnds actualSigntoolArgs (" " + (quoteAndJoinWithSpace timestampFiles)) "Expected arguments to end with a quoted space-separated list of files"

        testCase "default verify options" <| fun _ ->
            let verifyOptions = SignTool.VerifyOptions.Create()
            let verifyFiles = ["file1.ext"; "file2.ext"]
            let actualSigntoolPath, actualSigntoolArgs, actualSigntoolWorkingDir, actualSigntoolTimeout =
                SignTool.verifyInternal testRunner testSigntoolexeLocator verifyOptions verifyFiles

            Expect.equal actualSigntoolPath testSigntoolexePath "Expected correct signtool.exe path"
            Expect.equal actualSigntoolArgs "verify \"file1.ext\" \"file2.ext\"" "Expected correct arguments"
            Expect.equal actualSigntoolWorkingDir (Directory.GetCurrentDirectory()) "Expected correct working directory"
            Expect.isNone actualSigntoolTimeout "Expected no timeout"

        testPropertyWithConfig signtoolTestConfig "verify options" <| fun (verifyOptions: SignTool.VerifyOptions) (verifyFiles: FilesList) ->
            let expectedSigntoolPath, expectedSigntoolWorkingDir, expectedSigntoolTimeout =
                getExpectedToolOptions verifyOptions.ToolOptions
            let actualSigntoolPath, actualSigntoolArgs, actualSigntoolWorkingDir, actualSigntoolTimeout =
                SignTool.verifyInternal testRunner testSigntoolexeLocator verifyOptions verifyFiles

            Expect.equal actualSigntoolPath expectedSigntoolPath "Expected correct signtool.exe path"
            Expect.equal actualSigntoolWorkingDir expectedSigntoolWorkingDir "Expected correct working directory"
            Expect.equal actualSigntoolTimeout expectedSigntoolTimeout "Expected correct timeout"

            Expect.stringStarts actualSigntoolArgs "verify" "Expected arguments to start with 'verify'"
            Expect.isFalse (actualSigntoolArgs.EndsWith(' ')) "Expected arguments to not end with a space"
            actualSigntoolArgs
            |> expectIfOption "/debug" verifyOptions.Debug "Debug"
            |> expectIfEnum "/v" verifyOptions.Verbosity SignTool.Verbosity.Verbose "Verbosity.Verbose"
            |> expectIfEnum "/q" verifyOptions.Verbosity SignTool.Verbosity.Quiet "Verbosity.Quiet"
            |> expectIfOption "/a" verifyOptions.AllMethods "AllMethods"
            |> expectIfOption "/all" verifyOptions.AllSignatures "AllSignatures"
            |> expectIfOption "/d" verifyOptions.PrintDescription "PrintDescription"
            |> expectIfInt "/ds" verifyOptions.VerifyIndex "VerifyIndex"
            |> expectIfOption "/kp" verifyOptions.UseX64KernelModeDriverSigningPolicy "UseX64KernelModeDriverSigningPolicy"
            |> expectIfOption "/ms" verifyOptions.UseMultipleVerificationSemantics "UseMultipleVerificationSemantics"
            |> expectIfString "/o" verifyOptions.VerifyByOperatingSystemVersion "VerifyByOperatingSystemVersion"
            |> expectIfOption "/pa" verifyOptions.UseDefaultAuthenticationVerificationPolicy "UseDefaultAuthenticationVerificationPolicy"
            |> expectIfString "/r" verifyOptions.RootSubjectName "RootSubjectName"
            |> expectIfOption "/tw" verifyOptions.WarnIfNotTimeStamped "WarnIfNotTimeStamped"
            |> ignore
            Expect.stringEnds actualSigntoolArgs (" " + (quoteAndJoinWithSpace verifyFiles)) "Expected arguments to end with a quoted space-separated list of files"

        testCase "TraceSecrets include password replacement" <| fun _ ->
            use execContext = Fake.Core.Context.FakeExecutionContext.Create false "build.fsx" []
            Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)

            let password = "testpassword-123"
            let certificate = SignTool.SignCertificate.File { SignTool.CertificateFromFile.Create("certificate.pfx") with Password = Some password }
            let signOptions = SignTool.SignOptions.Create(certificate)
            let _ = signInternal testRunner testSigntoolexeLocator signOptions ["testfile1"]
            let traceSecrets = Fake.Core.TraceSecrets.getAll () |> List.map (fun s -> s.Value, s.Replacement)

            Expect.contains traceSecrets (sprintf "\"%s\"" password, "\"<PASSWORD>\"") "Expected TraceSecrets to contain password replacement"
    ]
