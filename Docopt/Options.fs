namespace Docopt

open System.Collections.Generic

type Options() =
  class
    let sopt = List<string * Token.Argument>()
    let lopt = List<string * Token.Argument>()
    member __.Add(opt:Token.Option) =
      
  end
;;
