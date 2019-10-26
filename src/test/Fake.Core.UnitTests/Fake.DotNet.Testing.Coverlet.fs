﻿module Fake.DotNet.Testing.CoverletTests

open Fake.DotNet
open Fake.DotNet.Testing
open Expecto

let getProps options =
    let args =
        MSBuild.CliArguments.Create()
        |> Coverlet.withMSBuildArguments options
    args.Properties

[<Tests>]
let tests =
    testList "Fake.DotNet.Testing.Coverlet.Tests" [
        testCase "Test that full properties are converted" <| fun _ ->
            let props = getProps (fun p ->
                {
                    OutputFormat = Coverlet.OutputFormat.Cobertura
                    Output = "coverage.json"
                    Include = [
                        "Incl.Asm1", "Incl.Ns1"
                        "Incl.Asm2", "Incl.Ns2"
                    ]
                    Exclude = [
                        "Excl.Asm1", "Excl.Ns1"
                        "Excl.Asm2", "Excl.Ns2"
                    ]
                    ExcludeByAttribute = ["Attr1"; "Attr2"]
                    ExcludeByFile = ["file1.cs"; "file2.cs"]
                    MergeWith = Some "prev-coverage.json"
                    Threshold = Some 80
                    ThresholdType = Coverlet.ThresholdType.Branch
                    ThresholdStat = Coverlet.ThresholdStat.Total
                    UseSourceLink = true
                })
            Expect.containsAll [
                "CollectCoverage", "true"
                "CoverletOutput", "coverage.json"
                "OutputFormat", "cobertura"
                "Include", "[Incl.Asm1]Incl.Ns1,[Incl.Asm2]Incl.Ns2"
                "Exclude", "[Excl.Asm1]Excl.Ns1,[Excl.Asm2]Excl.Ns2"
                "ExcludeByAttribute", "Attr1,Attr2"
                "ExcludeByFile", "file1.cs,file2.cs"
                "MergeWith", "prev-coverage.json"
                "Threshold", "80"
                "ThresholdType", "branch"
                "ThresholdStat", "total"
                "UseSourceLink", "true"
            ] props "expected proper MSBuild properties"

        testCase "Test that default properties are converted" <| fun _ ->
            let props = getProps id
            Expect.containsAll [
                "CollectCoverage", "true"
                "CoverletOutput", "./"
                "OutputFormat", "json"
            ] props "expected proper MSBuild properties"
    ]