module Fake.BuildServer.GitLab

open Expecto
open Fake.Core
open Fake.BuildServer

let testCases = [
    test "can emit correct collapsed output" {
        let mutable messages = []
        let writer _ _ newLine message = 
            let message = if newLine then message + @"\n" else message
            messages <- message :: messages
        let sameTicks _ = 1L
        let sameColorMapper _ = System.ConsoleColor.White
        let listener = GitLab.GitLabTraceListener(writer, sameColorMapper, sameTicks) :> Fake.Core.ITraceListener

        let tag = KnownTags.Target("my_first_section")
        let traceMessages = [
            TraceData.OpenTag(tag, Some "Header of the 1st collapsible section")
            TraceData.LogMessage("this line should be hidden when collapsed", true)
            TraceData.CloseTag(tag, System.TimeSpan.FromSeconds 1., TagStatus.Success)
        ]
        for trace in traceMessages do
            listener.Write trace

        let inOrder = List.rev messages
        let expected = [
            @"section_start:1:target_my_first_section\r\e[0KHeader of the 1st collapsible section\n"
            @"this line should be hidden when collapsed\n"
            @"section_end:1:target_my_first_section\r\e[0K\n"
        ]
        Expect.equal inOrder expected "should have output the correct collapsible trace"
    }
]


[<Tests>]
let tests = testList "Fake.BuildeServer.GitLab.Tests" testCases
