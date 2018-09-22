/// Analyse a binlog and emit proper CI messages
module Fake.DotNet.MSBuildBinLog

open System
open Microsoft.Build.Framework
open Microsoft.Build.Logging
open Microsoft.Build.Logging.StructuredLogger