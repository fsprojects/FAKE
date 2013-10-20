[<AutoOpen>]
/// Contains functions which allow to deal with permissions.
module Fake.PermissionsHelper

open System.Security.Principal

/// Checks that the current user has administrator permissions
let requiresAdmin installerF = 
    let principal = new WindowsPrincipal(WindowsIdentity.GetCurrent())
    if principal.IsInRole WindowsBuiltInRole.Administrator then
        installerF()
    else
        invalidOp "Administrator privileges are required to run this installer"