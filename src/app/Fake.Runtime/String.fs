/// Contains basic functions for string manipulation.

module Fake.Runtime.String
type String = System.String
open System

/// Returns if the string is null or empty
let inline isNullOrEmpty value = String.IsNullOrEmpty value

/// Returns if the string is not null or empty
let inline isNotNullOrEmpty value = String.IsNullOrEmpty value |> not
