/// Contains tasks to run [Expecto](https://github.com/haf/expecto) unit tests.
[<RequireQualifiedAccess>]
module Fake.DotNet.Testing.Expecto

open Fake.Core
open Fake.Testing.Common
open System

/// CLI parameters available if you use Tests.runTestsInAssembly defaultConfig argv in your code:
type Params =
    {
      /// Extra verbose output for your tests.
      Debug : bool
      /// Run all tests in parallel. Default is true.
      Parallel : bool
      /// Number of parallel workers (defaults to the number of logical processors).
      ParallelWorkers : int
      /// Prints out summary after all tests are finished.
      Summary : bool
      /// Prints out summary after all tests are finished including their source code location.
      SummaryLocation : bool
      /// Fails the build if focused tests exist. Default is true
      FailOnFocusedTests : bool
      /// Filter a specific hierarchy to run.
      Filter : string
      /// Filter a specific test case to run.
      FilterTestCase : string
      /// Filter a specific test list to run.
      FilterTestList : string
      /// Run only provided tests.
      Run : string list
      /// Doesn't run tests, print out list of tests instead.
      ListTests : bool
      /// Custom arguments to use in the case the helper not yet supports them
      CustomArgs : string list
      /// Prints the version on startup. Default is true
      PrintVersion : bool
      /// Working directory
      WorkingDirectory : string
    }

    override this.ToString() =
        let append (s: string) (sb: StringBuilder.StringBuilder) = sb.Append s
        let appendIfTrue value s sb =
            if value then append s sb else sb
        let appendIfNotNullOrWhiteSpace value s (sb: StringBuilder.StringBuilder) =
            if String.IsNullOrWhiteSpace value |> not
            then sprintf "%s%s " s value |> sb.Append
            else sb
        let appendIfNotEqual other value s (sb: StringBuilder.StringBuilder) =
            if other <> value
            then sprintf "%s%A " s value |> sb.Append
            else sb
        let appendList list s (sb: StringBuilder.StringBuilder) =
            let filtered = list |> List.filter (String.IsNullOrWhiteSpace >> not)
            if List.isEmpty filtered then sb
            else
                filtered |> String.separated " " |> sprintf "%s%s " s |> sb.Append
        StringBuilder.StringBuilder()
        |> appendIfTrue this.Debug "--debug "
        |> appendIfTrue this.Parallel "--parallel "
        |> appendIfNotEqual 0 this.ParallelWorkers "--parallel-workers "
        |> appendIfTrue this.FailOnFocusedTests "--fail-on-focused-tests "
        |> appendIfTrue this.Summary "--summary "
        |> appendIfTrue this.SummaryLocation "--summary-location "
        |> appendIfTrue (not this.Parallel) "--sequenced "
        |> appendIfTrue this.PrintVersion "--version "
        |> appendIfTrue this.ListTests "--list-tests "
        |> appendIfNotNullOrWhiteSpace this.Filter "--filter "
        |> appendIfNotNullOrWhiteSpace this.FilterTestCase "--filter-test-case "
        |> appendIfNotNullOrWhiteSpace this.FilterTestList "--filter-test-list "
        |> appendList this.Run "--run "
        |> appendList this.CustomArgs ""
        |> StringBuilder.toText

    static member DefaultParams =
        {
            Debug = false
            Parallel = false
            ParallelWorkers = 0
            Filter = ""
            FilterTestCase = ""
            FailOnFocusedTests = true
            FilterTestList = ""
            PrintVersion = true
            Run = []
            ListTests = false
            // Summary = true somehow breakes Expecto TeamCity printer
            Summary = false
            SummaryLocation = false
            CustomArgs = []
            WorkingDirectory = ""
        }

type private RunMode = | Direct | DotNetCli

let private getRunMode (assembly: string) =
    match System.IO.Path.GetExtension(assembly).ToLowerInvariant() with
    | ".dll" -> DotNetCli
    | ".exe" -> Direct
    | ext ->
        failwithf "Unable to find a way to run expecto test executable with extension %s" ext

let private runAssembly expectoParams testAssembly =
    let fakeStartInfo  =
        let runMode = getRunMode testAssembly
        let workingDir =
            if String.isNotNullOrEmpty expectoParams.WorkingDirectory
            then expectoParams.WorkingDirectory else Fake.IO.Path.getDirectory testAssembly
        let fileName, argsString =
            match runMode with
            | DotNetCli ->
                "dotnet", sprintf "\"%s\" %O" testAssembly expectoParams
            | Direct ->
                testAssembly, string expectoParams
        (fun (info: ProcStartInfo) ->
            { info with
                FileName = fileName
                Arguments = argsString
                WorkingDirectory = workingDir } )

    let exitCode = Process.execSimple fakeStartInfo TimeSpan.MaxValue
    testAssembly, exitCode

let run (setParams : Params -> Params) (assemblies : string seq) =
    let details = assemblies |> String.separated ", "
    use __ = Trace.traceTask "Expecto" details

    let expectoParams = setParams Params.DefaultParams

    let res =
        assemblies
        |> Seq.map (runAssembly expectoParams)
        |> Seq.filter( snd >> (<>) 0)
        |> Seq.toList

    match res with
    | [] -> ()
    | failedAssemblies ->
        failedAssemblies
        |> List.map (fun (testAssembly,exitCode) ->
            sprintf "Expecto test of assembly '%s' failed. Process finished with exit code %d." testAssembly exitCode )
        |> String.concat System.Environment.NewLine
        |> FailedTestsException |> raise
    __.MarkSuccess()
