[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
module Fake.Installers

open System.ServiceProcess
open System.Configuration.Install
open System.ServiceProcess
open System.ComponentModel
open System.Reflection
open System.Diagnostics
open Fake.Services

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
[<RunInstaller(true)>]
type FakeDeployInstaller() as self = 
    inherit Installer()
    let eventLogInstaller = new EventLogInstaller()
    let processInstaller = new ServiceProcessInstaller(Account = ServiceAccount.LocalSystem)
    let serviceInstaller = 
        new ServiceInstaller(DisplayName = "Fake Deploy Service Agent", 
                             Description = "Allows FAKE scripts to run as a deployment task", ServiceName = ServiceName, 
                             StartType = ServiceStartMode.Automatic)
    
    do 
        eventLogInstaller.Source <- ServiceName
        self.Installers.Add eventLogInstaller |> ignore
        self.Installers.Add processInstaller |> ignore
        self.Installers.Add serviceInstaller |> ignore
    
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    override x.OnCommitted(savedState) = 
        base.OnCommitted(savedState)
        let sc = new ServiceController(ServiceName)
        sc.Start()

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let getInstaller() = 
    let ti = new TransactedInstaller()
    let installer = new FakeDeployInstaller()
    ti.Installers.Add(installer) |> ignore
    let ctx = new InstallContext("", [| "/assemblypath=" + (Assembly.GetEntryAssembly()).Location |])
    ti.Context <- ctx
    ti

/// Installs the Fake listener
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let installServices() = getInstaller().Install(new System.Collections.Hashtable())

/// Uninstalls the Fake listener
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
let uninstallServices() = getInstaller().Uninstall(null)
