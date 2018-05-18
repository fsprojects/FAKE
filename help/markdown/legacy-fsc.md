# Compiling F# Sources

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE.exe before version 5 (or the non-netcore version). The documentation for FAKE 5 can be found <a href="/apidocs/v5/fake-dotnet-fsc.html">here </a></p>
</div>

The [Fsc task set](apidocs/v5/legacy/fake-fschelper.html) in FAKE can be used to build F# source files and output libraries, modules,
and executables by using the bundled
[FSharp.Compiler.Service](https://github.com/fsharp/FSharp.Compiler.Service). 
In this tutorial we will look at these compile tasks.

## The Fsc task

The `Fsc` task can be used in standard FAKE targets:

    #r "/path/to/FakeLib.dll"
    open Fake

    Target "Otherthing.dll" (fun _ ->
        ["Otherthing.fs"; "Otherthing2.fs"]
        |> FscHelper.compile [
            FscHelper.Target FscHelper.TargetType.Library
        ]
        |> function 0 -> () | c -> failwithf "F# compiler return code: %i" c
    )

    Target "Main.exe" (fun _ ->
        ["Main.fs"]
        |> FscHelper.compile [
            FscHelper.References ["Something.dll"; "Otherthing.dll"]
        ]
        |> function 0 -> () | c -> failwithf "F# compiler return code: %i" c
    )

The `FscHelper.compile` task takes two arguments: 

  1. a list of compile parameters (`FscParam`), and
  2. a list of source files.

We start with the list of source files, and send it into the `FscHelper.compile` task using F#'s
`|>` operator. The list of parameters included in the first argument will override the
default parameters.

In the above examples, notice that we don't always override the output
file name. By default `FscHelper.compile` will behave exactly the same way as
`fsc.exe`. If you don't specify an output file: it will use the name of
the first input file, and the appropriate filename extension.

`FscParam.Target` also behaves in the same way as the `fsc.exe`
`--target:` switch: if you don't override it, it defaults to an
executable output type.

You can override all `fsc.exe` default compile parameters by explicitly passing the values
you want to use. All F# compiler parameters are available as `FscParam` union cases:

    Target "Something.dll" (fun _ ->
        ["Something.fs"]
        |> FscHelper.compile [
            FscHelper.Out "Something.dll"
            FscHelper.Target FscHelper.TargetType.Library
            FscHelper.NoOptimizationData
            FscHelper.Checked true
        ]
        |> function 0 -> () | c -> failwithf "F# compiler return code: %i" c
    )

See the [API docs for FscHelper](apidocs/v5/legacy/fake-fschelper.html) for details of
the available parameters.

The `FscHelper.compile` task will print any compile warnings or errors. If there's any
compile error, it won't raise any error to interrupt the build process,
instead, it returns the exit status of the compile process.

Having an exit status code returned can be useful when you're trying to
integrate FAKE with other build management tools, e.g. your own CI
server or a test runner.

## FscHelper.compileFiles

This task is lower level than the previous one. It takes a list of
source files and a list of strings which contains the same arguments
you'd pass in to the `fsc.exe` command-line tool. It too prints warnings
and errors, and returns a compile exit status code. E.g.:

  Target "Something.dll" (fun _ ->
      ["Something.fs"]
      |> FscHelper.compileFiles ["-o"; "Something.dll"; "--target:library"]
      |> ignore
  )

This task may be useful when you already have compile options in string
format and just need to pass them in to your build tool. You'd just need
to split the string on whitespace, and pass the resulting list into
`FscHelper.compileFiles`.

