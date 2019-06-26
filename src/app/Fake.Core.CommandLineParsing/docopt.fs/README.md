Docopt.fs is a F\# port of [docopt]
===================================
```let ``Docopt.fs`` = docopt |> ``F#`` in```
***

Isn't it awesome how [CommandLineParser] and [PowerArgs] generate help
messages based on your code?!

*Hell no!*  You know what's awesome?  It's when the option parser *is*
generated based on the beautiful help message that you write yourself!
This way you don't need to write this stupid repeatable parser-code,
and instead can write only the help message—*the way you want it*.

**Docopt.fs** helps you create most beautiful command-line interfaces
*easily*:

```fsharp
open Docopt

let doc = """
Naval Fate.

Usage:
  naval_fate.exe ship new <name>...
  naval_fate.exe ship <name> move <x> <y> [--speed=<kn>]
  naval_fate.exe ship shoot <x> <y>
  naval_fate.exe mine (set|remove) <x> <y> [--moored | --drifting]
  naval_fate.exe (-h | --help)
  naval_fate.exe --version

Options:
  -h --help     Show this screen.
  --version     Show version.
  --speed=<kn>  Speed in knots [default: 10].
  --moored      Moored (anchored) mine.
  --drifting    Drifting mine.
"""

[<EntryPoint>]
let main argv =
  let docopt = new Docopt(doc)
  try
    let dict = docopt.Parse(argv)
    printfn "Success:\n%A" dict
    0
  with ArgvException(message) ->
    printfn "Error: %s" message
    -42
```

Beat that! The option parser is generated based on the docstring above
that is passed to the `Docopt.Parse` method. `Docopt.Parse` parses the usage
pattern (`doc` in this example) and option descriptions (lines starting
with dash `-`) and ensures that the program invocation matches the
usage pattern. It parses options, arguments and commands based on
that. The basic idea is that *a good help message has all necessary
information in it to make a parser*.

Differences from reference python implementation
------------------------------------------------

- This port should be fully Docopt language compatible with the 
  python reference implementation.
- Currently, if `--help` or `--version` is matched, nothing is done by the
  library and the user must write the help message or the version himself.
- The returned dictionary maps `string`s to `Docopt.Arguments.Result`, a
  discriminated union wrapping the results.
- If a key is not registered in the dictionary, its value is always
  `Result.None` instead of throwing a `KeyNotFoundException` to allow better
  pattern matching.
- The parsing is done thanks to the parser combinator library [FParsec] instead
  of regular expressions.

Compiling
---------

*TODO*

Installation
------------

*TODO*

Testing
-------

After compiling, you can run the `TestCases.exe` binary in 
`bin/Tests/TestCases.exe`. If no window appears, it’s all good!


API
===

Docopt.fsi
----------

```fsharp
type Docopt.HelpCallback = string -> bool
```
A simple callback function used by the `Docopt.Docopt` described below.

```fsharp
new Docopt.Docopt : doc      : string
                  * ?argv    : string array
                  * ?help    : HelpCallback
                  * ?version : obj
                  -> Docopt
```
The main constructor takes **1 required** and **3 optional** arguments:

- **`doc`** is a `string` that contains a help message that will be parsed to
  create the option parser. The syntax of such a help message is given in next
  sections. For a quick example, you can refer to the first example of this
  document.

- **`argv`** is an optional `string array` which defaults to
  `System.Environment.GetCommandLineArgs().[1..]`, the program’s original
  arguments vector.

- **`help`** is an optional `HelpCallback` help. By default, it just prints the
  help message (supplied as `doc`) and exits. If the user-supplied function
  returns `true` the program is exited when the function returns; else if the
  function returns `false` the `-h` or `--help` options can be handled like any
  other. The argument given to the function is the *Usage:* part of `doc`.  
  For example, if you want Docopt to ignore these options, you can just do:
  ```fsharp
  let docopt = Docopt(doc, help=fun _ -> false) in
  ```

- **`version`** is an optional `obj` which defaults to a basic `System.Object`
  whose `ToString()` member returns an error. The type of `version` defines how
  the version is retrieved:
  * `null`: Docopt will ignore `--version` and treat it like a normal
    option akin to `-h` and `--help` above.
  * `Lazy<obj>`: Docopt will get the version string by invoking
    `version.Value.ToString()` on the given lazy value, print it  to stdout and 
    exit with `exit 0`.
  * `obj`: Docopt will get the version string by invoking `version.ToString()`
  on the given object, print it to stdout and exit with `exit 0`.

This constructor can throw an exception of type `UsageException` defined as:
> ```fsharp
> exception UsageException of string
> ```

if there is a syntax error in the usage patterns. The message carried by the
exception describes precisely the error.

```fsharp
member Docopt.Parse : ?argv : string array 
                    * ?args : Arguments.Dictionary 
                    -> Arguments.Dictionary 
```
The parse method parses the argument vector and stores the results in a
`Docopt.Arguments.Dictionary` instance. It takes **2 optional** arguments:

- **`argv`** is an optional `string array` representing the argument vector to
  be parsed. By default the parsing will be done on the argv Docopt got during
  its construction.

- **`args`** is an optional `Docopt.Arguments.Dictionary`. It defaults to an
  empty newly constructed dictionary. The parser will store the results in this,
  so results will add with existing values.

The **return value** is a `Docopt.Arguments.Dictionary` with options, arguments
and commands as keys, spelled exactly like in your help message. Informations
that were described but not matched will not be in the dictionary (options with
`[default:...]` arguments will still be registered); however accessing them will
yield `Docopt.Result.None` instead of throwing an exception. For instance, if
you invoke the top example as:
> ```sh
> naval_fate.exe ship Guardian move 100 150 --speed=15
> ```

the returned dictionary will be:

> ```fsharp
> Docopt.Arguments.Dictionary [("ship", Flag);
>                              ("<name>", Argument "Guardian");
>                              ("move", Flag);
>                              ("<x>", Argument "100");
>                              ("<y>", Argument "150");
>                              ("--speed", Argument "15")]
> ```

This function can throw an exception of type `ArgvException` defined as:
> ```fsharp
> exception ArgvException of string
> ```

if argv does not satisfy the AST created at construction. The message carried by
the exception describes what the error is.

```fsharp
member Docopt.Usage : string
```
This property contains all the usage parts fused into one string for
convenience. Might be slow, as each use calls `System.String.Join`.

```fsharp
member Docopt.UsageParser : UsageParser
```
This property allows you to get the `UsageParser` generated from `doc`.


Help message format
===================

Help message consists of 2 parts:

- Usage pattern, e.g.:
  ```
  Usage: my_program.py [-hso FILE] [--quiet | --verbose] [INPUT ...]
  ```

- Option descriptions, e.g.:
  ```
  -h --help    show this
  -s --sorted  sorted output
  -o FILE      specify output file [default: ./test.txt]
  --quiet      print less text
  --verbose    print more text
  ```

Their format is described below; other text is ignored.

Usage patterns syntax
---------------------

This list describes each lexeme in the usage pattern grammar, and exposes the
lexeme’s syntax using standard [EBNF] extended with range (`"a"..."z"`) syntax
at the end.  
Usage pattern is a substring of `doc` that starts with `usage:` (*case
insensitive*) and ends with a *visibly* empty line. Minimum example:
```fsharp
"""Usage: my_program.exe

"""
```
The first word after `usage:` is interpreted as your program’s name. You can
specify your program's name several times to signify several exclusive patterns:
```fsharp
"""Usage: my_program.exe FILE
          my_program.exe COUNT FILE

"""
```
Each pattern can consist of the following elements:

- #### `<lower-case argument>` or `UPPER-CASE-ARGUMENTS`
  Positional arguments. Both form can be used to specify an argument, there is
  no difference between the two.  
  ```ini
  lower_arg_char  = ? any Unicode code point except ">" ? ;
  lower_arg       = "<" , lower_arg_char , { lower_arg_char } , ">" ;
  upper_letter    = ? Unicode category Lu ? ;
  decimal_digit   = ? Unicode category Nd ? ;
  upper_arg_start = upper_letter | decimal_digit ;
  upper_arg_cont  = upper_arg_start | "-" ;
  upper_arg       = upper_arg_start , { upper_arg_cont } ;
  argument        = lower_arg | upper_arg ;
  ```

- #### `-o` or `--option`
  Options are typically optional (though this is up to you), and can be either
  boolean (present/absent), as in `--flag`, or expect a trailing argument (see
  next section).

  Short-style options, as in `-f`, are also allowed. Synonyms between different
  spellings of the same option (e.g. `-v` and `--verbose`) can be established
  in the option descriptions (see below). Short-style options can also be
  stacked, as in `-rfA`. When options are stacked, `-rfA` is effectively
  equivalent to `(-r | -f | -A)...` to the argument parser.
  ```ini
  short_option_char = "a"..."z" | "A"..."Z" | "0"..."9" | "?" ;
  short_option      = "-" , short_option_char , { short_option_char } ;
  long_option_char  = short_option_char | "-" ;
  long_option       = "-" , "-" , long_option_char , { long_option_char } ;
  ```

- #### `-aARG` or `-a ARG` or `--argument=ARG` or `--argument ARG` 
  You can add a trailing argument to a long option. Arguments can be separated
  from the option name by an `=` or a single space, and can be specified as
  `<arg>` or `ARG` (consistency of style is recommended, but it is not enforced)
  .

  You can also add a trailing argument to a short option. In this case,
  arguments can be separated from the option name or joined to it. As with long
  options, either style can be used but consistency of style is recommended.
  ```ini
  long_option_arg  = ( "=" | " " ) , argument ;
  short_option_arg = [ " " ] , argument ;
  option           = long_option , [ long_option_arg ]
                   | short_option , [ short_option_arg ]
                   ;
  ```

- #### `[]` (brackets)
  Patterns inside brackets are optional. `[pattern]` is exactly equivalent to
  `ε | pattern`, with `ε` the epsilon parser (does absolutely nothing but is
  always successful).
  ```ini
  brackets = "[" , whitespace , pattern , "]" ;
  ```

- #### `()` (parens)
  Patterns inside parens are **required** (the same as patterns *not* in `()`
  are required). Parens are useful if you need to group some elements in
  subpatterns, either for use with `|` or `...`.
  ```ini
  parens = "(" , whitespace , pattern , ")" ;
  ```

- #### `|` (pipe)
  A pipe `|` separates mutually exclusive elements in a pattern. The different
  patterns described by each usage line are also mutually exclusive.
  ```ini
  pipe = pattern , "|" , whitespace , pattern ;
  ```

- #### `...` (ellipsis)
  An ellipsis can trail any element or group to make it repeatable once or
  more. Repeatable elements will be accumulated into a list of occurrences.
  ```ini
  ellipsis = pattern , "..." ;
  ```
- #### `[options]` (case sensitive)
  The string `[options]` is a shortcut to match any options specified in your
  option descriptions.
  ```ini
  options = "[options]" ;
  ```

- #### `[-]` and `[--]`
  Single hyphen `-` is used by convention to specify using `stdin` as input
  instead of reading a file.  
  Double hyphen `--` is typically used to manually separate leading options
  from trailing positional arguments.

  Both of these are treated as `command`s, and so are perfectly legal in usage
  patterns. They are typically optional elements, but can be required if you
  drop the `[]`.
  ```ini
  single_hyphen = [ "[" ] , "-" , [ "]" ] ;
  double_hyphen = [ "[" ] , "--" , [ "]" ] ;
  ```

- #### `commands`
  Anything not recognised as something described above is a command (or
  subcommand).
  ```ini
  command = ? any group of characters surrounded by whitespace ? ;
  ```

This gives us the following BNF describing a full usage pattern:
```ini
whitespace   = " " | ? tabulation ? ;
newline      = ? newline or eof ? ;
pattern_base = argument
             | option
             | brackets
             | parens
             | pipe
             | ellipsis
             | options
             | single_hyphen
             | double_hyphen
             | command
             ;
pattern      = pattern_base , whitespace ;
program_name = command ;
usage_part   = ? case-insensitive "usage:" ? ;
usage_line   = [ usage_part ] , program_name , { pattern } ;
usage        = { usage_line , newline } ;
grammar      = usage .
```

If your pattern allows to match argument-less option (a flag) several times:
```fsharp
"""
Usage: my_program.exe [-v | -vv | -vvv]
"""
```
then number of occurrences of the option will be counted. I.e. `args.["-v"]`
will be `Flags(2)` if program was invoked as `my_program.exe -vv`. Same works
for commands.

If your usage patterns allows to match same-named option with argument or
positional argument several times, the matched arguments will be collected into
a list:
```fsharp
"""
Usage: my_program.py <file> <file> --path=<path>...
"""
```
I.e. invoked with `my_program.exe file1 file2 --path=./here --path=./there` the
returned dictionary will be:
```fsharp
Docopt.Arguments.Dictionary [("<file>", Arguments ["file1";"file2"]);
                             ("--path", Arguments ["./here";"./there"])]
```

Option descriptions format
--------------------------

**Option descriptions** consist of a list of options that you put below your
usage patterns.

It is necessary to list option descriptions in order to specify:

* synonymous short and long options,
* if an option has an argument,
* if option's argument has a default value.

The rules are as follows:

* Every line in `doc` that starts with `-` or `--` (not counting spaces) is
  treated as an option description, e.g.:
  ```
  Options:
    --verbose   # GOOD
    -o FILE     # GOOD
  Other: --bad  # BAD, line does not start with dash "-"
  ```

* To specify that option has an argument, put a word describing that argument
  after space (or equals "`=`" sign) as shown below. Follow either <lowercase>
  or UPPERCASE convention for options' arguments. You can use comma if you want
  to separate options. In the example below, both lines are valid, however you
  are recommended to stick to a single style.:
  ```
  -o FILE --output=FILE       # without comma, with "=" sign
  -i <file>, --input <file>   # with comma, without "=" sing
  ```
  Here is a BNF for this:
  ```ini
  option_desc_short = whitespace , [ short_option ] ;
  option_desc_long  = whitespace , [ long_option ] ;
  option_desc_both  = option_desc_short , [ "," ] , " " , long_option ;
  option_desc       = option_desc_short
                    | option_desc_long
                    | option_desc_both
                    ;
  option_desc_line  = option_part , ( "  " , ? text ? | whitespace ) , newline ;
  ```
  You should get the [DFA] for unambiguous (and easier) parsing.

* Use two spaces to separate options with their informal description:
  ```
  --verbose More text.   # BAD, will be treated as if verbose option had
                         # an argument "More", so use 2 spaces instead
  -q        Quit.        # GOOD
  -o FILE   Output file. # GOOD
  --stdout  Use stdout.  # GOOD, 2 spaces
  ```

* If you want to set a default value for an option with an argument, put it
  into the option-description, in form `[default: <my-default-value>]`:
  ```
  --coefficient=K  The K coefficient [default: 2.95]
  --output=FILE    Output file [default: test.txt]
  --directory=DIR  Some directory [default: ./]
  ```

***
***

### Changelog

The first stable release will be v1.0. Until then, use with caution as things 
might change drastically.

- `0.1.0 - 29/01/2016` First release. All reference language agnostic tests 
  pass.

### Notes

Pull requests welcome!  
If you see a typo, or something wrong in the documentation, feel free to submit
one too!

### What’s next

- Option to exit automatically on help or version
- `[--]` token
- `options_first` parameter
- Active patterns for result dictionary
- Code cleanup
- Comments
- *Type provider?*
- *Typed arguments?*

[docopt]: https://github.com/docopt/docopt
[CommandLineParser]: https://nuget.org/packages/CommandLineParser/
[PowerArgs]: https://nuget.org/packages/PowerArgs/
[FParsec]: http://www.quanttec.com/fparsec/
[EBNF]: https://en.wikipedia.org/wiki/Extended_Backus–Naur_Form
[DFA]: https://github.com/Aksamyt/docopt.fs/blob/master/Resources/PoptDescLine-DFA.png?raw=true
