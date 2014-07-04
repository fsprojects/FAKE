# Compiling F# Sources

FAKE can be used to build F# source files and output libraries, modules,
and executables. It does this by making use of the
`FSharp.Compiler.Service` library that it comes with, and making
available three FAKE tasks for compiling in different ways and with
different return values. In this tutorial we will look at each compile
task.

## `Fsc`

The first task can be used in standard FAKE targets:

    #r "/path/to/FakeLib.dll"
    open Fake
    open Fake.FscHelper

    Target "Something.dll" (fun _ ->
      ["Something.fs"]
      |> Fsc (fun parameters ->
        { parameters with Output = "Something.dll"
                          FscTarget = Library }))

    Target "Otherthing.dll" (fun _ ->
      ["Otherthing.fs"]
      |> Fsc (fun parameters ->
        { parameters with FscTarget = Library }))

    Target "Main.exe" (fun _ ->
      ["Main.fs"]
      |> Fsc (fun parameters ->
        { parameters with References =
                            [ "Something.dll"
                              "Otherthing.dll" ] }))

    "Something.dll"
      ==> "Otherthing.dll"
      ==> "Main.exe"
    RunTargetOrDefault "Main.exe"

The `Fsc` task takes two arguments: (1) a function which overrides the
default compile parameters, and (2) a list of source files. We start
with the list of source files, and send it into the `Fsc` task using the
`|>` ('pipe') operator. We put the override function last. The override
function takes the default compile parameters and needs to return the
parameters with any, all, or no parameters overridden.

In the above examples, notice that I don't always override the output
file name. By default `Fsc` will behave exactly the same way as
`fsc.exe` if you don't specify an output file: it will use the name of
the first input file, and the appropriate filename extension.

The `FscTarget` slot also behaves in the same way as the `fsc.exe`
`--target:` switch: if you don't override it, it defaults to an
executable output type.

You can override all `fsc.exe` compile parameters using the override
function. Several of them, like `Output`, `References`, etc., are made
directly available; the others can all be set inside the `OtherParams`
slot as a list of strings:

    ["Something.fs"]
    |> Fsc (fun parameters ->
      { parameters with Output = "Something.dll"
                        FscTarget = Library
                        OtherParams =
                          [ "--nooptimizationdata"
                            "--checked+" ] })

See the [API docs for Fsc](apidocs/fake-fschelper.html) for details of
the available parameters.

The `Fsc` task will print any compile warnings or errors. If there's any
compile error, it will notify you and immediately quit.

## `fsc`

The next task that can compile F# sources starts with a lowercase 'f'.
It takes exactly the same arguments and can be called in exactly the
same way as the `Fsc` task. The only difference is that `fsc` _doesn't
raise any error_--instead, it returns the exit status of the compile
process. It still does print warnings and errors, though.

Having an exit status code returned can be useful when you're trying to
integrate FAKE with other build management tools, e.g. your own CI
server or a test runner.

## `fscList`

This task is lower level than the previous two. It takes a list of
source files and a list of strings which contains the same arguments
you'd pass in to the `fsc.exe` command-line tool. Think of it as exactly
like the `OtherParams` slot shown above, only here it's used to specify
_all_ the parameters. It too prints warnings and errors, and returns a
compile exit status code. E.g.:

    Target "Something.dll" (fun _ ->
      ["Something.fs"]
      |> fscList ["-o"; "Something.dll"; "--target:library"]
      ())

This task may be useful when you already have compile options in string
format and just need to pass them in to your build tool. You'd just need
to split the string on whitespace, and pass the resulting list into
`fscList`.

