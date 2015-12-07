namespace Docopt

open System
open System.Collections.Generic

type Options() =
  class
    let sopt = Dictionary<char, Token.Argument option>()
    let lopt = Dictionary<string, Token.Argument option>()
    member __.Add(opt:Token.Option) =
      begin
        if opt.Sname <> Char.MaxValue && not (sopt.ContainsKey(opt.Sname)) then
          sopt.Add(opt.Sname, opt.Arg)
        if opt.Lname <> null && not (lopt.ContainsKey(opt.Lname)) then
          lopt.Add(opt.Lname, opt.Arg)
      end
    member __.ContainsSopt = sopt.ContainsKey
    member __.ContainsLopt = lopt.ContainsKey
    override __.ToString() =
      begin
        let ret = Text.StringBuilder("Options { sopt = [") in
        for kv in sopt do ignore (ret.Append(kv.Key).Append(';'));
        ignore (ret.Append("]; lopt = ["));
        for kv in lopt do ignore (ret.Append(kv.Key).Append(';'));
        ignore(ret.Append("] }"));
        ret.ToString()
      end
  end
;;
