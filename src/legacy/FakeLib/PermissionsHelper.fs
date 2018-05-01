[<AutoOpen>]
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
/// Contains functions which allow to deal with permissions.
module Fake.PermissionsHelper

open System.Security.Principal

/// Returns whether the given user has administrator permissions.
/// ## Parameters
///  - `identity` - The windows identity of the user in question.
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
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
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let requiresAdmin f = 
    if isAdmin(WindowsIdentity.GetCurrent()) then
        f()
    else
        invalidOp "Administrator privileges are required to run this function."
