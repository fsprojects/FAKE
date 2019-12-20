module Fake.Tools.SignTool.Tests

open System
open System.IO
open Expecto
open Fake.Tools
open FsCheck


let testRunner (signtoolPath: string) (signtoolArgs: string) (signtoolWorkingDir: string) (signtoolTimeout: TimeSpan) =
    (signtoolPath, signtoolArgs, signtoolWorkingDir, signtoolTimeout)

let testSigntoolexePath =
    "test/path/to/signtool.exe"
let testSigntoolexeLocator () =
    Some testSigntoolexePath

let getDefaultWorkingDir =
    Directory.GetCurrentDirectory()


let genFiles =
    Gen.elements ["file1"; "subfolder"; "path"; "file2"; "to"; "file3"; ".ext"]
    |> Gen.nonEmptyListOf
    |> Gen.sample 3 5
    |> List.map (fun l -> String.Join("/", l))
let getExpectedToolOptions topts filesCount =
    let filesTimeout = TimeSpan.FromSeconds (10.0 * float filesCount)
    match topts with
    | Some t ->
        let toolPath = t.ToolPath |> Option.defaultValue testSigntoolexePath
        let workingDir = t.WorkingDir |> Option.defaultValue getDefaultWorkingDir
        let timeout = t.Timeout |> Option.defaultValue filesTimeout
        (toolPath, workingDir, timeout)
    | None -> testSigntoolexePath, getDefaultWorkingDir, filesTimeout

let expectIfOption c (o: bool option) m args =
    if o.IsSome && o.Value then
        Expect.stringContains (args + " ") (sprintf " %s " c) (sprintf "Option '%s' was set but arguments do not contain '%s'" m c)
    args
let expectIfString c (o: string option) m args =
    if o.IsSome then
        Expect.stringContains (args + " ") (sprintf " %s \"%s\" " c o.Value) (sprintf "Option '%s' was set but arguments do not contain '%s \"%s\"'" m c o.Value)
    args
let expectIfInt c (o: int option) m args =
    if o.IsSome then
        Expect.stringContains (args + " ") (sprintf " %s %i " c o.Value) (sprintf "Option '%s' was set but arguments do not contain '%s %i'" m c o.Value)
    args
let expectIfEnum c (o: 'a option) (v: 'a) m args =
    if o.IsSome && o.Value = v then
        Expect.stringContains (args + " ") (sprintf " %s " c) (sprintf "Option '%s' was set but arguments do not contain '%s'" m c)
    args


let quoteAndJoinWithSpace xs =
    String.Join(" ", List.map (sprintf "\"%s\"") xs)


[<Tests>]
let tests =
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
            Expect.equal actualSigntoolTimeout (TimeSpan.FromSeconds 20.0) "Expected correct timeout"

        testProperty "sign options" <| fun (signOptions: SignTool.SignOptions) ->
            let signFiles = genFiles
            let expectedSigntoolPath, expectedSigntoolWorkingDir, expectedSigntoolTimeout =
                getExpectedToolOptions signOptions.ToolOptions (List.length signFiles)
            let actualSigntoolPath, actualSigntoolArgs, actualSigntoolWorkingDir, actualSigntoolTimeout =
                SignTool.signInternal testRunner testSigntoolexeLocator signOptions signFiles

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
            |> (fun args ->
                match signOptions.TimeStamp with
                | Some t ->
                    let serverUrl = t.ServerUrl |> Option.defaultValue "http://timestamp.digicert.com"
                    args
                    |> expectIfEnum (sprintf "/t \"%s\"" serverUrl) t.Algorithm SignTool.DigestAlgorithm.SHA1 "TimeStamp.Algorithm.SHA1"
                    |> expectIfEnum (sprintf "/tr \"%s\" /td \"sha256\"" serverUrl) t.Algorithm SignTool.DigestAlgorithm.SHA256 "TimeStamp.Algorithm.SHA256"
                | None -> args )
            |> expectIfString "/ac" signOptions.AdditionalCertificate "AdditionalCertificate"
            |> expectIfOption "/as" signOptions.AppendSignature "AppendSignature"
            |> expectIfString "/c" signOptions.CertificateTemplateName "CertificateTemplateName"
            |> expectIfString "/d" signOptions.Description "Description"
            |> expectIfString "/u" signOptions.EnhancedKeyUsage "EnhancedKeyUsage"
            |> expectIfOption "/uw" signOptions.EnhancedKeyUsageW "EnhancedKeyUsageW"
            |> ignore
            Expect.stringEnds actualSigntoolArgs (" " + (quoteAndJoinWithSpace signFiles)) "Expected arguments to end with a quoted space-separated list of files"

        testCase "default time stamp options" <| fun _ ->
            let timestampOptions = SignTool.TimeStampOptions.Create()
            let timestampFiles = ["file1.ext"; "file2.ext"]
            let actualSigntoolPath, actualSigntoolArgs, actualSigntoolWorkingDir, actualSigntoolTimeout =
                SignTool.timeStampInternal testRunner testSigntoolexeLocator timestampOptions timestampFiles

            Expect.equal actualSigntoolPath testSigntoolexePath "Expected correct signtool.exe path"
            Expect.equal actualSigntoolArgs "timestamp /tr \"http://timestamp.digicert.com\" /td \"sha256\" \"file1.ext\" \"file2.ext\"" "Expected correct arguments"
            Expect.equal actualSigntoolWorkingDir (Directory.GetCurrentDirectory()) "Expected correct working directory"
            Expect.equal actualSigntoolTimeout (TimeSpan.FromSeconds 20.0) "Expected correct timeout"

        testProperty "time stamp options" <| fun (timestampOptions: SignTool.TimeStampOptions) ->
            let timestampFiles = genFiles
            let expectedSigntoolPath, expectedSigntoolWorkingDir, expectedSigntoolTimeout =
                getExpectedToolOptions timestampOptions.ToolOptions (List.length timestampFiles)
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
            |> (fun args ->
                match timestampOptions.TimeStamp with
                | Some t ->
                    let serverUrl = t.ServerUrl |> Option.defaultValue "http://timestamp.digicert.com"
                    args
                    |> expectIfEnum (sprintf "/t \"%s\"" serverUrl) t.Algorithm SignTool.DigestAlgorithm.SHA1 "TimeStamp.Algorithm.SHA1"
                    |> expectIfEnum (sprintf "/tr \"%s\" /td \"sha256\"" serverUrl) t.Algorithm SignTool.DigestAlgorithm.SHA256 "TimeStamp.Algorithm.SHA256"
                | None ->
                    Expect.stringContains args " /tr \"http://timestamp.digicert.com\" /td \"sha256\" " "Unexpected default value for TimeStamp option"
                    args )
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
            Expect.equal actualSigntoolTimeout (TimeSpan.FromSeconds 20.0) "Expected correct timeout"

        testProperty "verify options" <| fun (verifyOptions: SignTool.VerifyOptions) ->
            let verifyFiles = genFiles
            let expectedSigntoolPath, expectedSigntoolWorkingDir, expectedSigntoolTimeout =
                getExpectedToolOptions verifyOptions.ToolOptions (List.length verifyFiles)
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
