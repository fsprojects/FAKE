module Fake.Core.ProcessTests

open System
open Fake.Core
open Expecto
open FsCheck

let testCaseWithProcessTracing name test = 
    testCase name <| fun arg ->
        Fake.ContextHelper.withFakeContext name (fun() ->
            Process.setEnableProcessTracing true
            test arg)

let getRawCommandLine (c:CreateProcess<'a>) =
    let mutable result = None
    let starter =
        { new IProcessStarter with
            member x.Start (r: RawCreateProcess) =
                async {
                    let si = r.ToStartInfo
                    result <- Some si.Arguments
                    return System.Threading.Tasks.Task.FromResult { RawExitCode = 0 }
                } }
        
    Process.Proc.startRaw starter c
    match result with
    | Some args -> args
    | None -> failwithf "Expected to retrieve arguments"

let fsCheckConfig = { FsCheckConfig.defaultConfig with maxTest = 1000  }

[<Tests>]
let tests = 
  testList "Fake.Core.Process.Tests" [
    yield testPropertyWithConfig fsCheckConfig "toWindowsCommandLine is the inverse of fromWindowsCommandLine" <|
        fun (x: NonNull<string> list) ->
            let input = x |> List.map (fun (NonNull s) -> s)
            let escaped = Args.toWindowsCommandLine input
            let backAgain = Args.fromWindowsCommandLine escaped
            Expect.sequenceEqual backAgain input (sprintf "Expect argument lists to be equal, intermediate was '%s'" escaped)

    yield testCaseWithProcessTracing "Test that we have a nice error message when a file doesn't exist" <| fun _ ->
        try
            Process.start(fun proc ->
                { proc with
                    FileName = "FileDoesntExist.exe"
                    Arguments = "arg1 arg2" })
                |> ignore
            Expect.isTrue false "Expected an exception"
        with e ->
            let s = e.Message.Contains "FileDoesntExist.exe"
            Expect.isTrue s ("Expected file-path as part of the message '" + e.Message + "'. Error was: " + string e)

    yield testCase "Test that CreateProcess.ofStartInfo works (1)" <| fun _ ->
        let shell, command = "cmd", "/C \"echo 1&& echo 2\""
        let cb = Process.getProcI (fun proc ->
                    { proc with
                        FileName = shell
                        Arguments = command })
        let file, args =
            match cb.Command with
            | ShellCommand cmd -> failwithf "Expected RawCommand"
            | RawCommand (f, a) -> f, a
        Expect.equal file "cmd" "Expected correct command"
        Expect.sequenceEqual (args |> Arguments.toList) ["/C"; "echo 1&& echo 2"] "Expected correct args"
        Expect.equal args.ToStartInfo command "Expect proper command (cmd is strange with regards to escaping)"

    yield testCaseWithProcessTracing "Test that we can read messages correctly" <| fun _ ->
        let shell, command =
            if Environment.isWindows then
                "cmd", "/C \"echo 1&& echo 2\""
            else
                "sh", "-c \"echo '1'; echo '2'\""
        let result =
            Process.execWithResult(fun proc ->
                    { proc with
                        FileName = shell
                        Arguments = command }) (TimeSpan.FromMinutes 1.)
      
        Expect.equal result.Messages ["1"; "2"] 
            (sprintf "Messages are not read correctly.\n%s"
                result.ReportString)

    yield testCase "Test that Arguments.withPrefix works" <| fun _ ->
        let args = Arguments.ofList [ "Some" ]
        let newArgs = Arguments.withPrefix ["--debug"; "test.exe" ] args
        Expect.sequenceEqual (newArgs |> Arguments.toList) [ "--debug"; "test.exe"; "Some"] "expected lists to be equal"

    yield testCase "Test we can workaround #2197" <| fun _ ->
        let original = """-source:iisapp="C:\some\path\"""
        let actual =
            original
            |> CreateProcess.fromRawCommandLine "./folder/mytool.exe"
            |> getRawCommandLine
        Expect.equal actual original "Expected to retrieve exact match"

    yield testCase "Test CreateProcess.ofStartInfo with different streams - reported on gitter" <| fun _ ->
        let isRedirected s =
            match s with
            | StreamSpecification.Inherit -> false
            | _ -> true
        
        let si = System.Diagnostics.ProcessStartInfo()
        let actual =
            si
            |> CreateProcess.ofStartInfo
        Expect.isFalse (isRedirected actual.Streams.StandardInput) "Expect Std Input to be NOT redirected"
        Expect.isFalse (isRedirected actual.Streams.StandardOutput) "Expect Std Output to be NOT redirected"
        Expect.isFalse (isRedirected actual.Streams.StandardError) "Expect Std Error to be NOT redirected"
        
        let si = System.Diagnostics.ProcessStartInfo()
        si.RedirectStandardInput <- true
        let actual =
            si
            |> CreateProcess.ofStartInfo
        Expect.isTrue (isRedirected actual.Streams.StandardInput) "Expect Std Input to be redirected"
        Expect.isFalse (isRedirected actual.Streams.StandardOutput) "Expect Std Output to be NOT redirected"
        Expect.isFalse (isRedirected actual.Streams.StandardError) "Expect Std Error to be NOT redirected"
        
        let si = System.Diagnostics.ProcessStartInfo()
        si.RedirectStandardOutput <- true
        let actual =
            si
            |> CreateProcess.ofStartInfo
        Expect.isFalse (isRedirected actual.Streams.StandardInput) "Expect Std Input to be NOT redirected"
        Expect.isTrue (isRedirected actual.Streams.StandardOutput) "Expect Std Output to be redirected"
        Expect.isFalse (isRedirected actual.Streams.StandardError) "Expect Std Error to be NOT redirected"

        let si = System.Diagnostics.ProcessStartInfo()
        si.RedirectStandardError <- true
        let actual =
            si
            |> CreateProcess.ofStartInfo
        Expect.isFalse (isRedirected actual.Streams.StandardInput) "Expect Std Input to be NOT redirected"
        Expect.isFalse (isRedirected actual.Streams.StandardOutput) "Expect Std Output to be NOT redirected"
        Expect.isTrue (isRedirected actual.Streams.StandardError) "Expect Std Error to be redirected"
  ]
