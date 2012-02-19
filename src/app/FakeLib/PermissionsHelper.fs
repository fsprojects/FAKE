namespace Fake

[<AutoOpen>]
module PermissionsHelper =

    open System.Security.Principal

    let requiresAdmin f = 
       let principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
       if (principal.IsInRole(WindowsBuiltInRole.Administrator)) = false
       then invalidOp "Administrator privledges are required to run this installer"
       else f()


    

