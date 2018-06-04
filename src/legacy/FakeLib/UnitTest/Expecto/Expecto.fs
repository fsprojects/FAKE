/// Contains tasks to run [expecto](https://github.com/haf/expecto) v2 unit tests.
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Fake.Testing.Expecto

open System
open System.IO
open System.Text
open Fake
open System.Diagnostics

/// CLI parameters available if you use Tests.runTestsInAssembly defaultConfig argv in your code:
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type ExpectoParams =
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
      CustomArgs: string list
      /// Prints the version on startup. Default is true
      PrintVersion : bool
      /// Working directory
      WorkingDirectory : string
    }

    override this.ToString() =
        let append (s: string) (sb: StringBuilder) = sb.Append s
        let appendIfTrue value s sb =
            if value then append s sb else sb
        let appendIfNotNullOrWhiteSpace value s (sb: StringBuilder) =
            if String.IsNullOrWhiteSpace value |> not
            then sprintf "%s%s " s value |> sb.Append
            else sb
        let appendIfNotEqual other value s (sb: StringBuilder) =
            if other = value
            then sprintf "%s%A " s value |> sb.Append
            else sb
        let appendList list s (sb: StringBuilder) =
            let filtered = list |> List.filter (String.IsNullOrWhiteSpace >> not)
            if List.isEmpty filtered then sb
            else
                filtered |> separated " " |> sprintf "%s%s " s |> sb.Append
        StringBuilder()
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
        |> toText
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    static member DefaultParams =
        {
            Debug = false
            Parallel = true
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
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
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
            let info = ProcessStartInfo(testAssembly)
            info.WorkingDirectory <- workingDir
            info.Arguments <- argsString
            info.UseShellExecute <- false
            // Pass environment variables to the expecto console process in order to let it detect it's running on TeamCity
            // (it checks TEAMCITY_PROJECT_NAME <> null specifically).
            for name, value in environVars EnvironmentVariableTarget.Process do
                info.EnvironmentVariables.[string name] <- string value
            use proc = Process.Start(info)
            proc.WaitForExit() // Don't set a process timeout. The timeout is per test.
            proc.ExitCode
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
