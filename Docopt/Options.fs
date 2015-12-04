namespace Docopt

open System
open System.Collections.Generic

type Options() =
  class
    let sopt = HashSet<string * Token.Argument option>()
    let lopt = HashSet<string * Token.Argument option>()
    member __.Add(opt:Token.Option) =
      begin
        if opt.Sname <> Char.MaxValue then
          sopt.Add(String([|'-';opt.Sname|]), opt.Arg) |> ignore
        if opt.Lname <> null then
          lopt.Add(opt.Lname, opt.Arg) |> ignore
      end
    override __.ToString() =
      begin
        let ret = Text.StringBuilder("Options { sopt = [") in
        for (str, _) in sopt do ignore (ret.Append(str).Append(';'));
        ignore (ret.Append("]; lopt = ["));
        for (str, _) in lopt do ignore (ret.Append(str).Append(';'));
        ignore(ret.Append("] }"));
        ret.ToString()
      end
  end
;;
