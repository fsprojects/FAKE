namespace Fake.UnitTest

open System

type TestStatus = 
| Ok
| Ignored of string * string
| Failure of string * string

type Test = {
    Name:string 
    RunTime: TimeSpan
    Status: TestStatus }

type TestResults = {
    SuiteName : string
    Tests: Test list  }
