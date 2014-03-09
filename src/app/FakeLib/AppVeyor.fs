[<AutoOpen>]
/// Contains code to configure FAKE for AppVeyor integration
module Fake.AppVeyor

open Fake.MSBuildHelper

// Add MSBuildLogger to track build messages
if buildServer = BuildServer.AppVeyor then
    MSBuildLoggers <- @"""C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll""" :: MSBuildLoggers
