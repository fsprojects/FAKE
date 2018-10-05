# Fake.Core.CommandLineParsing

This module is a fork from https://github.com/docopt/docopt.fs/ but with strong ordering.
The strong ordering enables you to have a nice CLI on your script or to write your own fake 5 modules with a CLI.

Example `script.fsx`:

```fsharp
#r "paket:
nuget Fake.Core.CommandLineParsing
//"

open Fake.Core

let cli = """
usage: prog [options]

options:
 -a        Add
 -r        Remote
 -m <msg>  Message
"""

// retrieve the fake 5 context information
let ctx = Context.forceFakeContext ()
// get the arguments
let args = ctx.Arguments
let parser = Docopt(cli)
let parsedArguments = parser.Parse(args)

if DocoptResult.hasFlag "-a" parsedArguments then
    printfn "Got -a"

match DocoptResult.tryGetArgument "-m" results with
| None -> printfn "Printing generic message"
| Some arg -> printfn "%s" arg
```

A more sophisticated example can be found in the fake runner: https://github.com/fsharp/FAKE/blob/64d871f5065412fe7b233025e454ccf3b89e46d7/src/app/Fake.netcore/Program.fs#L204-L259

Or the target module:

- https://github.com/fsharp/FAKE/blob/rc_1/src/app/Fake.Core.Target/Target.fs#L9-L26
- https://github.com/fsharp/FAKE/blob/64d871f5065412fe7b233025e454ccf3b89e46d7/src/app/Fake.Core.Target/Target.fs#L564-L619

You can also take a look at the test-suite:

- https://github.com/fsharp/FAKE/blob/rc_1/src/test/Fake.Core.CommandLine.UnitTests/Fake.Core.CommandLine.fs

## Differences to the python reference Docopt implementation

- The parser doesn't differentiate between arguments with and without `-` with regards to ordering. They are subject to the same rules as other arguments. The only exception is when defining multiple arguments like `[-a -b -c]`, then the ordering of the group doesn't matter.
 (So in other words: If a group `()` or `[]` only has options then the order doesn't matter)
- Uniquely identifiable prefixes like `--fsia` are not supported
- We return the arguments in the user given order in the result map (difference to `docopt.fs`)
- We parse arguments starting with `-` as positional arguments. For example consider:

  ```bash
  usage: prog (NAME | --foo NAME)
  
  options: --foo
  ```

  <div class="alert alert-info">
      <h5>INFO</h5>
      <p>Note that --foo has no argument because it was not specified in the options section!</p>
  </div>
  
  In this scenario `prog --foo 10` is parsed as `--foo` and `NAME` argument because that is the only   option. However `prog --foo=10` is parsed as `NAME` argument without any `--foo` option. Usually to   prefer `--foo` you should put it first in the usage string:
  
  ```bash
  usage: prog (--foo NAME | NAME)
  
  options: --foo
  ```
  
  However, in this particular case it doesn't make any difference (as the options section is missing to indicate that `--foo` has an argument).

- `[]` is not inherited for all items, only for the group. To have all items optional use `[]` on every item. For example `usage: prog [go go]` means to have either two `go` or none. A single one is not allowed.
- We do not merge external "options" in the usage string with `[options]`. For example:

  ```bash
  usage: prog [options] [-a]
  
  options: -a
           -b
  ```
  
  Means that `-a` is actually allowed twice.
