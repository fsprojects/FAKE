module FsCheck.Fake.TestStringHelper

open Fake
open global.Xunit
open FsCheck

[<Fact>]
let ``NormalizeVersion removes tralining .0`` () =
    let noTrailingZero x = (NormalizeVersion x).EndsWith(".0") = false
    Check.QuickThrowOnFailure noTrailingZero
