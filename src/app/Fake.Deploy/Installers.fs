module Fake.Installers

open System.ServiceProcess
open System.Configuration.Install
open System.ServiceProcess
open System.ComponentModel
open System.Reflection

[<RunInstaller(true)>]
type FakeDeployInstaller() as self = 
    inherit Installer()
     
    let processInstaller = new ServiceProcessInstaller(Account = ServiceAccount.LocalSystem)
    let serviceInstaller = 
        new ServiceInstaller(
                                DisplayName = "Fake Deploy Service Agent",
                                Description = "Allows FAKE scripts to run as a deployment",
                                ServiceName = "Fake Deploy Agent",
                                StartType = ServiceStartMode.Automatic
                            )

    do 
        self.Installers.Add processInstaller |> ignore
        self.Installers.Add serviceInstaller |> ignore

    override x.OnCommitted(savedState) = 
        base.OnCommitted(savedState)
        let sc = new ServiceController("Fake Deploy Agent")
        sc.Start()


let getInstaller() = 
    let ti = new TransactedInstaller()
    let installer = new FakeDeployInstaller()
    ti.Installers.Add(installer) |> ignore
    let ctx = new InstallContext("", [|"/assemblypath=" + (Assembly.GetEntryAssembly()).Location|]) 
    ti.Context <- ctx
    ti

/// Installs the Fake listener
let installServices() = getInstaller().Install(new System.Collections.Hashtable())

/// Uninstalls the Fake listener
let uninstallServices() = getInstaller().Uninstall(null)