/// New Command line interface for FAKE that utilises Argu.
[<RequireQualifiedAccessAttribute>]
module Cli

open System
open Argu

type RunArgs =
  | [<UniqueAttribute>][<AltCommandLine("-f")>][<GatherUnrecognized>] Script of string
  | [<AltCommandLine("-t")>] Target of string
  | [<AltCommandLine("-e")>] EnvironmentVariable of string * string
  | [<AltCommandLine("-d")>] Debug
  | [<AltCommandLine("-s")>] SingleTarget
  | [<AltCommandLine("-n")>] NoCache
  | FsiArgs of string
with
  interface IArgParserTemplate with
    member s.Usage =
      match s with
      | Script _ -> "Specify the script to run. (--script or -f is optional)"
      | EnvironmentVariable _ -> "Set an environment variable."
      | FsiArgs _ -> "Arguments passed to the f# interactive."
      | Debug _ -> "Debug the script (set a breakpoint at the start)."
      | SingleTarget _ -> "Run only the specified target."
      | Target _ -> "The target to run."
      | NoCache _ -> "Disable caching of the compiled script."

type BuildArgs =
  | [<UniqueAttribute>][<AltCommandLine("-t")>][<GatherUnrecognized>] Target of string
  | [<AltCommandLine("-f")>] Script of string
  | [<AltCommandLine("-e")>] EnvironmentVariable of string * string
  | [<AltCommandLine("-d")>] Debug
  | [<AltCommandLine("-s")>] SingleTarget
  | [<AltCommandLine("-n")>] NoCache
  | FsiArgs of string
with
  interface IArgParserTemplate with
    member s.Usage =
      match s with
      | Target _ -> "The target to run (--target or -t is optional when running 'build')."
      | Script _ -> "Specify the script to run. "
      | EnvironmentVariable _ -> "Set an environment variable."
      | FsiArgs _ -> "Arguments passed to the f# interactive."
      | Debug _ -> "Debug the script (set a breakpoint at the start)."
      | SingleTarget _ -> "Run only the specified target."
      | NoCache _ -> "Disable caching of the compiled script."

type FakeArgs =
  | Version
  | (*[<Inherit>]*) [<AltCommandLine("-v")>] Verbose
  | [<AltCommandLine("-s")>] Silent
  | [<CliPrefix(CliPrefix.None)>] Run of Argu.ParseResults<RunArgs>
  | [<CliPrefix(CliPrefix.None)>] Build of Argu.ParseResults<BuildArgs>
with
  interface IArgParserTemplate with
    member s.Usage =
      match s with
      | Version _ -> "Prints the version."
      | Verbose _ -> "More verbose output. Can be used more than once."
      | Silent _ -> "Hide all output from the fake runner (output from script is still shown)."
      | Run _ -> "Runs a script."
      | Build _ -> "Build a target via build.fsx script."