[<AutoOpen>]
/// Contains functions which allow to deal with permissions.
module Fake.PermissionsHelper

open System.Security.Principal

/// Returns whether the given user has administrator permissions.
/// ## Parameters
///  - `identity` - The windows identity of the user in question.
let isAdmin identity = 
    let principal = new WindowsPrincipal(identity)
    principal.IsInRole WindowsBuiltInRole.Administrator

/// Checks that the current user has administrator permissions - otherwise it throws an exception.
/// ## Parameters
///  - `f` - This Function will be excuted if the use has the right permissions.
///
/// ## Sample
///
///     Target "Install" (fun _ -> 
///          requiresAdmin (fun _ -> installMSI())
///      )
let requiresAdmin f = 
    if isAdmin(WindowsIdentity.GetCurrent()) then
        f()
    else
        invalidOp "Administrator privileges are required to run this function."