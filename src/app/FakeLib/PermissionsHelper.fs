[<AutoOpen>]
module Fake.PermissionsHelper

open System.Security.Principal

let requiresAdmin installerF = 
    let principal = new WindowsPrincipal(WindowsIdentity.GetCurrent())
    if principal.IsInRole WindowsBuiltInRole.Administrator then
        installerF()
    else
        invalidOp "Administrator privileges are required to run this installer"