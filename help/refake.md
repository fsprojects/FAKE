# ReFake - a DSL for Specifying Incremental Builds in FAKE

ReFake is a domain-specific language (DSL) for telling FAKE how to do
incremental builds natively, without using any other build tool (like
MSBuild or `xbuild`).

ReFake will build only those parts of the project that are absolutely
necessary. All the logic for this is built into the FAKE library, so you
can specify _everything_ in F# instead of XML configuration files or
whatever.

The idea behind ReFake is exactly the same as the one behind `make`,
with which you're probably already familiar. To illustrate, let me use a
simple example 'makefile':

    myapp.dll: myapp.fs
      fsc -o myapp.dll -a myapp.fs

    myapp.exe: myapp.dll main.fs
      fsc -o myapp.exe -r myapp.dll main.fs

Here's a direct translation of this simple makefile into ReFake format:

    #r "/path/to/FakeLib.dll"
    open Fake.FscHelper
    open Fake.ReFake

    let myapp_dll =
      rx' "myapp.dll" [fx "myapp.fs"] (fun name dependencies ->
        fxs dependencies // A list of source files.
        // Pipeline into the 'fsc' function with the following
        // parameters.
        |> fsc (fun parameters ->
          { parameters with Output = name
                            FscTarget = Library }))

    let myapp_exe =
      fx' "myapp.exe" [myapp_dll; fx "main.fs"] (fun name deps ->
        fxs deps // A list of source files.
        // Pipeline into the 'fsc' function with the following
        // parameters.
        |> fsc (fun ps ->
          { ps with References = rxs deps // A list of references.
                    Output = name }))

    reRunAndExit myapp_exe

## Explanation

Here's an explanation of each important part of the ReFake script:

  - The script starts with a reference to the FAKE library, as usual. It
    then uses two modules designed to work together to implement the
    ReFake format.

  - In the makefile, each 'paragraph' is dedicated to building one
    artifact, a single file (i.e. `myapp.dll` or `myapp.exe`). In the
    ReFake script, each paragraph serves the same purpose, but we
    explicitly define what we call 'targets' and capture their
    definitions in the names `myapp_dll` and `myapp_exe`. The names are
    arbitrary; you can have whatever you like according to standard F#
    naming rules.

  - The targets are defined using helper functions. The first one,
    `myapp_dll`, is defined using the `rx'` function. The second one
    uses the `fx'` function. These functions allow us to define targets
    while also specifying how they should be built. The difference
    between them is, the `rx'` function defines a 'reference' target,
    that is a target that will be used as a reference by the compiler
    (i.e., passed in to another compile with the `--reference` switch).
    The `fx'` function defines a 'file' target, which will simply be an
    output file, like an executable.

  - There are two other corresponding helper functions, `fx` and `rx`,
    which let you create targets that _don't_ have any dependencies, and
    _don't_ specify how to build the files. In other words, you use them
    when you want to say that those files are not outputs of some build
    process, but rather _inputs_ to a build process. For example, you'd
    use `fx` to wrap up an F# source file as a target (as done above),
    and `rx` to wrap up a third-party library which you don't compile,
    but just download and use in your project.

  - Each of the helper functions `rx'`, `fx'`, and `vx` (which I'll
    discuss later) allows us to specify three things: (1) the name of
    the target; (2) the dependencies of the target; and (3) how to
    actually go about building the target. You'll notice this is exactly
    the same way a makefile works.

  - For the `rx'`, `fx'`, `rx` and `fx` functions, the name should be
    the name of the target file. For the `vx` function, the name is
    simply descriptive.

  - The dependencies of the target are a list of previously-defined
    targets, or simple source files wrapped in the `fx` function (which
    turns them into targets, as described above).

  - The target builder function (`fun name deps -> ...`) takes the
    output name and the list of dependencies and lets you specify what
    steps to take to actually build the output. It also needs you to
    return an integer status code in the style of a command-line exit
    status code: 0 for success, anything else for failure. Usually
    you'll be calling the `fsc` function inside the builder function.
    `fsc` will return a status code.

  - The `fsc` function takes two arguments: (1) a function that lets you
    specify parameters for the compile, and (2) a list of input source
    files to compile. ReFake provides you with helper functions to
    easily pass along input source files and reference files to the
    `fsc` function. Since you define input files and references using
    the `fx`, `fx'`, `rx`, and `rx'` functions, you can use the
    corresponding `fxs` and `rxs` functions to extract a list of input
    files and reference files from the dependency list.

    The `fsc` function lets you specify all F# compile parameters. To
    learn more, look up the [tutorial](./fsc.html) and the [API
    reference](apidocs/fake-fschelper.html).

  - Finally, the `reRunAndExit` function lets you run any target (i.e.,
    incrementally build the target) and immediately after finishing the
    run, exits the script with the exit code returned by the run. If
    there are any compile errors, the exit code will be set properly.
    You can also use the `reRun` function if you want to run a target
    without immediately exiting the script.

## Virtual Targets

I haven't talked much about _virtual_ targets so far, but you can
probably guess what they are:

    let build = vx "Build" [myapp_exe] nop
    reRunAndExit build

A virtual target is one that doesn't specify any output file, but still
depends on other targets. The unique feature of a virtual target is that
it always requires its dependencies to be built. If all its dependencies
are up-to-date (if the `myapp.exe` file is up-to-date in the above
example), then nothing further will happen. But if it's out of date, it
will be rebuilt.

The `nop` function is a 'no-op', that is it successfully returns without
doing anything. If you pass in your own defined function with tasks
instead of the `nop` function, you can use virtual targets to clean your
project directory, deploy a website, set configuration parameters for a
build, and do many other useful tasks.

## Conclusion

With the F# compile task and an incremental build script written in
ReFake, you can start developing F# straightaway, without the need for
any IDE, build tool, or complex XML build specs. Everything stays in F#.

