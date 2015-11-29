namespace Docopt

type HelpCallback = unit -> string

type Docopt =
  class
    /// <summary>Generate the parsers from <c>doc</c> and create a new <c>Docopt</c> instance.</summary>
    /// <param name="doc">The usage string of the program, formatted with docopt's syntax.</param>
    /// <returns>A new instance of <c>Docopt</c>.</returns>
    new : doc:string
          * ?argv:string array
          * ?help:HelpCallback
          * ?version:obj
          -> Docopt

    /// <summary>Parse argv and return the results in a new or existing <see cref="T:Docopt.Args"/> instance.</summary>
    /// <param name="argv">(optional) The <c>argv</c> value to use.</param>
    /// <param name="args">(optional) An existing container to store the arguments in.</param>
    /// <returns>The parsed arguments.</returns>
    member Parse : ?argv:string array
                   * ?args:Docopt.Args
                   -> Docopt.Args
  end
;;
