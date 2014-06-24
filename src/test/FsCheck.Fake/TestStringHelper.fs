module FsCheck.Fake.TestStringHelper

open FsCheck
open Fake
open global.Xunit

[<Fact>]
let ``startsWith is alias for string.StartsWith`` () =
    let startsWithIsAlias (prefix:string) (text:string) = startsWith prefix text <> text.StartsWith prefix
    Check.QuickThrowOnFailure startsWithIsAlias
