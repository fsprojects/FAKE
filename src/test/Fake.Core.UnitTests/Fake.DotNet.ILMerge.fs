module Fake.DotNet.ILMerge

open System
open System.Globalization
open Fake.Core
open Fake.DotNet
open Expecto

let testCases =
    if Environment.isWindows then
        [ testCase "Test that default arguments are processed as expected" <| fun _ ->
              let p = ILMerge.Params.Create()
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              let args = ILMerge.getArguments dummy dummy2 p
              Expect.isTrue (p.DebugInfo)
                  "A field should have non-default value for a bool"
              Expect.equal args [ "/out:" + dummy
                                  "/target:library"
                                  dummy2 ] "The defaults should be simple"
          testCase "Test that process is created as expected" <| fun _ ->
              let toolPath = Guid.NewGuid().ToString()
              let p = { ILMerge.Params.Create() with ToolPath = toolPath }
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              let proc = ILMerge.createProcess p dummy dummy2
              Expect.equal proc.CommandLine (String.Join(" ",
                                                         [ toolPath
                                                           "/out:" + dummy
                                                           "/target:library"
                                                           dummy2 ]))
                  "The defaults should be simple"
          testCase "Test that version can be set" <| fun _ ->
              let vdummy = Guid.NewGuid().ToByteArray()
              let v =
                  System.Version
                      (int vdummy.[0], int vdummy.[4], int vdummy.[8], int vdummy.[12])
              let p = { ILMerge.Params.Create() with Version = Some v }
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              let args = ILMerge.getArguments dummy dummy2 p
              Expect.equal args [ "/out:" + dummy
                                  "/ver:" + v.ToString()
                                  "/target:library"
                                  dummy2 ] "Version should be as given"
          testCase "Test that duplicate types can be allowed" <| fun _ ->
              let p =
                  { ILMerge.Params.Create() with AllowDuplicateTypes =
                                                     ILMerge.AllPublicTypes }
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              let args = ILMerge.getArguments dummy dummy2 p
              Expect.equal args [ "/out:" + dummy
                                  "/target:library"
                                  "/allowDup"
                                  dummy2 ] "Duplicates should be allowed"
          testCase "Test that assembly attributes may be multiplied" <| fun _ ->
              let p =
                  { ILMerge.Params.Create() with AllowMultipleAssemblyLevelAttributes =
                                                     true }
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              let args = ILMerge.getArguments dummy dummy2 p
              Expect.equal args [ "/out:" + dummy
                                  "/target:library"
                                  "/allowMultiple"
                                  dummy2 ] "Multiples should be allowed"
          testCase "Test that wild cards may be allowed" <| fun _ ->
              let p = { ILMerge.Params.Create() with AllowWildcards = true }
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              let args = ILMerge.getArguments dummy dummy2 p
              Expect.equal args [ "/out:" + dummy
                                  "/target:library"
                                  "/wildcards"
                                  dummy2 ] "Wild cards should be allowed"
          testCase "Test that zero PE Kind may be allowed" <| fun _ ->
              let p = { ILMerge.Params.Create() with AllowZeroPeKind = true }
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              let args = ILMerge.getArguments dummy dummy2 p
              Expect.equal args [ "/out:" + dummy
                                  "/target:library"
                                  "/zeroPeKind"
                                  dummy2 ] "Wild cards should be allowed"
          testCase "Test that closure may be allowed" <| fun _ ->
              let p = { ILMerge.Params.Create() with Closed = true }
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              let args = ILMerge.getArguments dummy dummy2 p
              Expect.equal args [ "/out:" + dummy
                                  "/target:library"
                                  "/closed"
                                  dummy2 ] "Closure should be allowed"
          testCase "Test that attributes may be copied" <| fun _ ->
              let p = { ILMerge.Params.Create() with CopyAttributes = true }
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              let args = ILMerge.getArguments dummy dummy2 p
              Expect.equal args [ "/out:" + dummy
                                  "/target:library"
                                  "/copyattrs"
                                  dummy2 ] "Attribute copying should be allowed"
          testCase "Test that types may be made internal" <| fun _ ->
              let p = { ILMerge.Params.Create() with Internalize = ILMerge.Internalize }
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              let args = ILMerge.getArguments dummy dummy2 p
              Expect.equal args [ "/out:" + dummy
                                  "/target:library"
                                  "/internalize"
                                  dummy2 ] "Internalization should be allowed"
          testCase "Test that files may be aligned" <| fun _ ->
              let a = DateTime.Now.Second
              let p = { ILMerge.Params.Create() with FileAlignment = Some a }
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              let args = ILMerge.getArguments dummy dummy2 p
              Expect.equal args [ "/out:" + dummy
                                  "/target:library"
                                  "/align:" + a.ToString(CultureInfo.InvariantCulture)
                                  dummy2 ] "Alignment should be allowed"
          testCase "Test that debug info may be excluded" <| fun _ ->
              let p = { ILMerge.Params.Create() with DebugInfo = false }
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              let args = ILMerge.getArguments dummy dummy2 p
              Expect.equal args [ "/out:" + dummy
                                  "/target:library"
                                  "/ndebug"
                                  dummy2 ] "No-debug should be allowed"
          testCase "Test that type merging may be allowed" <| fun _ ->
              let p = { ILMerge.Params.Create() with UnionMerge = true }
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              let args = ILMerge.getArguments dummy dummy2 p
              Expect.equal args [ "/out:" + dummy
                                  "/target:library"
                                  "/union"
                                  dummy2 ] "Merging types should be allowed"
          testCase "Test that xml documentation merge may be allowed" <| fun _ ->
              let p = { ILMerge.Params.Create() with XmlDocs = true }
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              let args = ILMerge.getArguments dummy dummy2 p
              Expect.equal args [ "/out:" + dummy
                                  "/target:library"
                                  "/xmldocs"
                                  dummy2 ] "Merging XML docs should be allowed"
          testCase "Test that EXE merge may be allowed" <| fun _ ->
              let p = { ILMerge.Params.Create() with TargetKind = ILMerge.Exe }
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              let args = ILMerge.getArguments dummy dummy2 p
              Expect.equal args [ "/out:" + dummy
                                  "/target:exe"
                                  dummy2 ] "Merging EXEs should be allowed"
          testCase "Test that WinEXE merge may be allowed" <| fun _ ->
              let p = { ILMerge.Params.Create() with TargetKind = ILMerge.WinExe }
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              let args = ILMerge.getArguments dummy dummy2 p
              Expect.equal args [ "/out:" + dummy
                                  "/target:winexe"
                                  dummy2 ] "Merging WinEXEs should be allowed"
          testCase "Test that string arguments are processed as expected" <| fun _ ->
              let libs =
                  [ Guid.NewGuid().ToString()
                    Guid.NewGuid().ToString() ]
              let dups =
                  [ Guid.NewGuid().ToString()
                    Guid.NewGuid().ToString() ]
              let attr = Guid.NewGuid().ToString()
              let except = Guid.NewGuid().ToString()
              let key = Guid.NewGuid().ToString()
              let log = Guid.NewGuid().ToString()
              let search =
                  [ Guid.NewGuid().ToString()
                    Guid.NewGuid().ToString() ]
              let platform = Guid.NewGuid().ToString()
              let p =
                  { ILMerge.Params.Create() with Libraries = libs
                                                 AllowDuplicateTypes =
                                                     ILMerge.DuplicateTypes dups
                                                 AttributeFile = attr
                                                 Internalize =
                                                     ILMerge.InternalizeExcept except
                                                 KeyFile = key
                                                 LogFile = log
                                                 SearchDirectories = search
                                                 TargetPlatform = platform }
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              let args = ILMerge.getArguments dummy dummy2 p
              Expect.equal args [ "/out:" + dummy
                                  "/attr:" + attr
                                  "/keyfile:" + key
                                  "/log:" + log
                                  "/target:library"
                                  "/targetplatform:" + platform
                                  "/internalize:" + except
                                  "/allowDup:" + (dups |> Seq.head)
                                  "/allowDup:" + (dups |> Seq.last)
                                  "/lib:" + (search |> Seq.head)
                                  "/lib:" + (search |> Seq.last)
                                  dummy2
                                  libs |> Seq.head
                                  libs |> Seq.last ]
                  "Strings should be assigned as expected" ]
    else
        [ testCase "Test failure on non-Windows platforms" <| fun _ ->
              let p = ILMerge.Params.Create()
              let dummy = Guid.NewGuid().ToString()
              let dummy2 = Guid.NewGuid().ToString()
              Expect.throwsC (fun () -> ILMerge.run p dummy dummy2)
                  (fun ex ->
                  Expect.equal (ex.GetType()) typeof<NotSupportedException>
                      "Exception type should be as expected"
                  Expect.equal ex.Message "ILMerge is currently not supported on mono"
                      "Exception message should be as expected") ]

[<Tests>]
let tests = testList "Fake.DotNet.ILMerge.Tests" testCases