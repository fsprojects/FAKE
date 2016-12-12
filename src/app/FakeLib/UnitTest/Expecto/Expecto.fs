[<AutoOpen>]
/// Contains tasks to run [xUnit](https://github.com/haf/expecto) v2 unit tests.
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
    member this.ToString() =
        StringBuilder()
        |> appendIfTrue this.Debug " --debug"
        |> appendIfTrue this.Parallel " --parallel"
        |> appendIfFalse this.Parallel " --sequential"
        |> appendIfNotNullOrEmpty this.Filter " --filter "
        |> appendIfNotNullOrEmpty this.FilterTestCase " --filter-test-case "
        |> appendIfNotNullOrEmpty this.FilterTestList " --filter-test-list "
        |> appendIfTrue
                (this.Run |> List.exists isNotNullOrEmpty)
                (this.Run |> List.filter isNotNullOrEmpty |> String.concat " " |> sprintf " --run %s")
        |> toText

    static member defaultParams =
        {
            Debug = false
            Parallel = true
            Filter = ""
            FilterTestCase = ""
            FilterTestList = ""
            Run = []
            ListTests = false
            WorkingDirectory = ""
        }

let Expecto (setParams : ExpectoParams -> ExpectoParams) (assemblies : string seq) =
    let args = setParams ExpectoParams.defaultParams
    let argsString = string args
    let runAssembly testAssembly =
        let processTimeout = TimeSpan.MaxValue // Don't set a process timeout.  The timeout is per test.
        let workingDir =
            if isNotNullOrEmpty args.WorkingDirectory
            then args.WorkingDirectory else DirectoryName testAssembly
        let processResult =
            ExecProcess(fun info ->
                info.FileName <- testAssembly
                info.WorkingDirectory <- workingDir
                info.Arguments <- argsString
            ) processTimeout
        testAssembly,processResult
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