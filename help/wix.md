# Create WiX Setup

If you often ship software to customers, it might be comfortable for you to install it using setups rather than manually deploying.
FAKE provides you with support for creating MSI setups using the WiX Toolset (http://wixtoolset.org/).

## Minimal working example

    Target "BuildWiXSetup" (fun _ ->
        // This defines, which files should be collected when running bulkComponentCreation
        let fileFilter = fun (file : FileInfo) -> 
            if file.Extension = ".dll" || file.Extension = ".exe" || file.Extension = ".config" then 
                true 
            else 
                false
            
        // Collect Files which should be shipped. Pass directory with your deployment output for deployDir
        // along with the targeted architecture.
        
        let components = bulkComponentCreation fileFilter (DirectoryInfo deployDir) Architecture.X86
                 
        // Collect component references for usage in features
        let componentRefs = components |> Seq.map(fun comp -> comp.ToComponentRef())

        let completeFeature = generateFeatureElement (fun f -> 
                                                        {f with  
                                                            Id = "Complete"
                                                            Title = "Complete Feature"
                                                            Level = 1 
                                                            Description = "Installs all features"
                                                            Components = componentRefs
                                                            Display = Expand 
                                                        })

        // Generates a predefined WiX template with placeholders which will be replaced in "FillInWiXScript"
        generateWiXScript "SetupTemplate.wxs"

        let WiXUIMondo = generateUIRef (fun f ->
                                            {f with
                                                Id = "WixUI_Mondo"
                                            })

        let WiXUIError = generateUIRef (fun f ->
                                            {f with
                                                Id = "WixUI_ErrorProgressText"
                                            })

        let MajorUpgrade = generateMajorUpgradeVersion(
                                fun f ->
                                    {f with 
                                        Schedule = MajorUpgradeSchedule.AfterInstallExecute
                                        DowngradeErrorMessage = "A later version is already installed, exiting."
                                    })

        FillInWiXTemplate "" (fun f ->
                                {f with
                                    // Guid which should be generated on every build
                                    ProductCode = Guid.NewGuid()
                                    ProductName = "Test Setup"
                                    Description = "Description of Test Setup"
                                    ProductLanguage = 1033
                                    ProductVersion = "1.0.0"
                                    ProductPublisher = "YouOrYourCompany"
                                    // Set fixed upgrade guid, this should never change for this project!
                                    UpgradeGuid = WixProductUpgradeGuid
                                    MajorUpgrade = [MajorUpgrade]
                                    UIRefs = [WiXUIMondo; WiXUIError]
                                    ProgramFilesFolder = ProgramFiles32
                                    Components = components
                                    BuildNumber = "Build number"
                                    Features = [completeFeature]
                                })
            

        // run the WiX tools
        WiX (fun p -> {p with ToolDirectory = WiXPath}) 
            setupFileName
            @".\SetupTemplate.wxs"
    )

## Further possibilities
Besides just plainly shipping those files as setup you can also use the custom action and custom action execution elements to execute various commands before or after certain events during the installation.
This gives you the possibility to for example install, uninstall, start or stop certain services when you need to.
If your software is for example running as a service at your customer's side, you would need to manually stop and start the services on upgrades.
WiX knows the "ServiceControl" element for starting, stopping and removing services. You attach it to components.
If a component's files change and it has a Service Control element, the service that is referred to will be started, stopped or uninstalled, just as you defined it.
You can use the attachServiceToControlComponents function for attaching service controls to components. You would only have to slightly change the above example:

### Example
    let serviceControl = generateServiceControl(fun f -> {f with 
                                                            Id = "PickAnId"
                                                            Name = "QualifiedNameOfYourService"
                                                            Start = InstallUninstall.Install
                                                            Stop = InstallUninstall.Both
                                                            Remove = InstallUninstall.Uninstall})

    // This defines, that all executable files should be tagged with the service control element
    let componentSelector = fun (comp : WiXComponent) -> comp.Files |> Seq.exists(fun file -> file.Name.EndsWith(".exe")) 

    let components = attachServiceControlToComponents
                        (bulkComponentCreation (fun file -> 
                            if file.Extension = ".dll" || file.Extension = ".exe" || file.Extension = ".config" then 
                                true 
                            else 
                                false) (DirectoryInfo deployDir))
                        componentSelector 
                        [serviceControl]
      
                        
## Registry
If you need your installer to modify the registry, you can achieve this by creating a component containing `RegistryKey` and `RegistryValue` elements. A `RegistryKey` can have further keys and values nested as children.

### Example

First, define the desired registry layout and add it to a new component:

    let registryDefinition = generateRegistryKey (fun f -> {f with
                                                              Root = Some WiXRegistryRootType.HKCU
                                                              Key = "Some key name"
                                                              Values = [
                                                                generateRegistryValue(fun v -> 
                                                                                        {v with
                                                                                           Name = "Something"
                                                                                           Type = WiXRegistryValueType.Integer
                                                                                           Value = "1"
                                                                                        })
                                                              ]
                                                              Keys = [
                                                                //Could nest more keys here
                                                              ]
                                                           })
                                                          
    let regComponent = C (generateComponent (fun c -> {c with
                                                         Id = "RegistryEntries"
                                                         RegistryKeys = [registryDefinition]
                                                      }))
                                                      
This component needs to be attached to the `TARGETDIR` directory. This effectively means the registry entries should be installed to the target user's machine. For this we will use a `DirectoryRef` pointing to `TARGETDIR`.

    let targetDirRef = generateDirectoryRef (fun r -> {r with
                                                         Id = "TARGETDIR"
                                                         Components = [regComponent]
                                                      })
    
The directory reference should be included in the `DirectoryRefs` sequence when calling `FillInWixTemplate`, and a reference to `regComponent` should be added to the relevant feature.
              
    let componentRefs = (Seq.append components [regComponent]) |> Seq.map(fun comp -> comp.ToComponentRef())
    
    let completeFeature = generateFeatureElement (fun f -> 
                                                    {f with  
                                                        Id = "Complete"
                                                        Title = "Complete Feature"
                                                        Level = 1 
                                                        Description = "Installs all features"
                                                        Components = componentRefs
                                                        Display = Expand 
                                                    })
  
    FillInWiXTemplate "" (fun f ->
                            {f with
                                // Guid which should be generated on every build
                                ProductCode = Guid.NewGuid()
                                ProductName = "Test Setup"
                                Description = "Description of Test Setup"
                                ProductLanguage = 1033
                                ProductVersion = "1.0.0"
                                ProductPublisher = "YouOrYourCompany"
                                // Set fixed upgrade guid, this should never change for this project!
                                UpgradeGuid = WixProductUpgradeGuid
                                MajorUpgrade = [MajorUpgrade]
                                UIRefs = [WiXUIMondo; WiXUIError]
                                ProgramFilesFolder = ProgramFiles32
                                Components = components
                                BuildNumber = "Build number"
                                Features = [completeFeature]
                                DirectoryRefs = [targetDirRef]
                            })

    
