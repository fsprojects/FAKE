namespace Docopt

open System
open System.Collections.Generic

type Options() =
  class
    member val internal sopt = Dictionary<char, Token.Argument option>()
    member val internal lopt = Dictionary<string, Token.Argument option>()
    member xx.Add(opt:Token.Option) =
      begin
        if opt.Sname <> Char.MaxValue && not (xx.sopt.ContainsKey(opt.Sname)) then
          xx.sopt.Add(opt.Sname, opt.Arg)
        if opt.Lname <> null && not (xx.lopt.ContainsKey(opt.Lname)) then
          xx.lopt.Add(opt.Lname, opt.Arg)
      end
    member xx.ContainsSopt = xx.sopt.ContainsKey
    member xx.ContainsLopt = xx.lopt.ContainsKey
    override xx.ToString() =
      begin
        let ret = Text.StringBuilder("Options { sopt = [") in
        for kv in xx.sopt do ignore (ret.Append(kv.Key).Append(';'));
        ignore (ret.Append("]; lopt = ["));
        for kv in xx.lopt do ignore (ret.Append(kv.Key).Append(';'));
        ignore(ret.Append("] }"));
        ret.ToString()
      end
  end
;;
