module Fake.Installers

open System.ServiceProcess
open System.Configuration.Install
open System.ServiceProcess
open System.ComponentModel
open System.Reflection
open System.Diagnostics
open Fake.Services

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
    
    override x.OnCommitted(savedState) = 
        base.OnCommitted(savedState)
        let sc = new ServiceController(ServiceName)
        sc.Start()

let getInstaller() = 
    let ti = new TransactedInstaller()
    let installer = new FakeDeployInstaller()
    ti.Installers.Add(installer) |> ignore
    let ctx = new InstallContext("", [| "/assemblypath=" + (Assembly.GetEntryAssembly()).Location |])
    ti.Context <- ctx
    ti

/// Installs the Fake listener
let installServices() = getInstaller().Install(new System.Collections.Hashtable())

/// Uninstalls the Fake listener
let uninstallServices() = getInstaller().Uninstall(null)