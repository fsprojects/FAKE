/// Contains tasks to run [expecto](https://github.com/haf/expecto) v2 unit tests.
module Fake.Testing.Expecto

open System
open System.IO
open System.Text
open Fake

/// CLI parameters available if you use Tests.runTestsInAssembly defaultConfig argv in your code:
type ExpectoParams =
    {
      /// Extra verbose output for your tests
      Debug : bool
      /// Run all tests in parallel. Default is true
      Parallel : bool
      /// Filter a specific hierarchy to run
      Filter : string
      /// Allows to print a nice summary at the end of the test suite.
      Summary : bool
      /// Fails the build if focused tests exist. Default is true
      FailOnFocusedTests : bool
      /// Filter a specific test case to run.
      FilterTestCase : string
      /// Filter a specific test list to run.
      FilterTestList : string
      /// Run only provided tests.
      Run : string list
      /// Doesn't run tests, print out list of tests instead.
      ListTests : bool
      /// Working directory
      WorkingDirectory : string
    }
    override this.ToString() =
        let append (s: string) (sb: StringBuilder) = sb.Append s
        let appendIfTrue p s sb =
            if p then append s sb else sb
        let appendIfNotNullOrWhiteSpace p s (sb: StringBuilder) =
            if String.IsNullOrWhiteSpace p |> not
            then sprintf "%s%s " s p |> sb.Append
            else sb
        let appendList list s (sb: StringBuilder) =
            let filtered = list |> List.filter (String.IsNullOrWhiteSpace >> not)
            if List.isEmpty filtered then sb
            else
                filtered |> separated " " |> sprintf "%s%s " s |> sb.Append
        StringBuilder()
        |> appendIfTrue this.Debug "--debug "
        |> appendIfTrue this.Parallel "--parallel "
        |> appendIfTrue this.FailOnFocusedTests "--fail-on-focused-tests "
        |> appendIfTrue this.Summary "--summary "
        |> appendIfTrue (not this.Parallel) "--sequential "
        |> appendIfNotNullOrWhiteSpace this.Filter "--filter "
        |> appendIfNotNullOrWhiteSpace this.FilterTestCase "--filter-test-case "
        |> appendIfNotNullOrWhiteSpace this.FilterTestList "--filter-test-list "
        |> appendList this.Run "--run "
        |> toText

    static member DefaultParams =
        {
            Debug = false
            Parallel = true
            Filter = ""
            FilterTestCase = ""
            FailOnFocusedTests = true
            FilterTestList = ""
            Run = []
            ListTests = false
            Summary = true
            WorkingDirectory = ""
        }

let Expecto (setParams : ExpectoParams -> ExpectoParams) (assemblies : string seq) =
    let args = setParams ExpectoParams.DefaultParams
    use __ = assemblies |> separated ", " |> traceStartTaskUsing "Expecto"
    let argsString = string args
    let runAssembly testAssembly =
        let processTimeout = TimeSpan.MaxValue // Don't set a process timeout.  The timeout is per test.
        let workingDir =
            if isNotNullOrEmpty args.WorkingDirectory
            then args.WorkingDirectory else DirectoryName testAssembly
        let exitCode =
            ExecProcess(fun info ->
                info.FileName <- testAssembly
                info.WorkingDirectory <- workingDir
                info.Arguments <- argsString
            ) processTimeout
        testAssembly, exitCode
    let res =
        assemblies
        |> Seq.map runAssembly
        |> Seq.filter( snd >> (<>) 0)
        |> Seq.toList
    match res with
    | [] -> ()
    | failedAssemblies ->
        failedAssemblies
        |> List.map (fun (testAssembly,exitCode) -> sprintf "Expecto test of assembly '%s' failed. Process finished with exit code %d." testAssembly exitCode)
        |> String.concat System.Environment.NewLine
        |> FailedTestsException |> raise