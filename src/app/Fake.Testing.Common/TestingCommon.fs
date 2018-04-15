// Some common definition for testing data
module Fake.Testing.Common

open Fake.Core
open System

/// Option which allows to specify if a test runner error should break the build.
type TestRunnerErrorLevel =
    /// This option instructs FAKE to break the build if a test runner reports an error.
    | Error
    /// This option instructs FAKE to break the build if a test runner finds the first error.
    | FailOnFirstError
    /// With this option set, no exception is thrown if a test is broken.
    | DontFailBuild

type FailedTestsException(msg) =
    inherit Trace.FAKEException(msg)
