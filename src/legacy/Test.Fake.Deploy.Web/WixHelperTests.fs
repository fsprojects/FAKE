module Test.Fake.Deploy.Web.WixHelper

open System
open Fake
open Xunit
open System.Text.RegularExpressions

[<Fact>]
let ``should find correct file id`` () =
    let wixDirString = "<Component Id=\"comp\" Guid=\"1290b30b-4242-4ed5-99z0-52e3ad1337e6\">
                            <File Id=\"fi_2\" Name=\"Castle.Core.dll\" Source=\"C:\Git\Test\Publish\wixHelper\Castle.Core.dll\" />
                            <File Id=\"fi_3\" Name=\"Castle.Windsor.dll\" Source=\"C:\Git\Test\Publish\wixHelper\Castle.Windsor.dll\" />
                            <File Id=\"fi_4\" Name=\"FAKE.Test.Abstractions.dll\" Source=\"C:\Git\Test\Publish\wixHelper\FAKE.Test.Abstractions.dll\" />
                            <File Id=\"fi_5\" Name=\"FAKE.Test.SomeProject.exe\" Source=\"C:\Git\Test\Publish\wixHelper\FAKE.Test.SomeProject.exe\" />
                            <File Id=\"fi_6\" Name=\"FAKE.Test.SomeProject.exe.config\" Source=\"C:\Git\Test\Publish\wixHelper\FAKE.Test.SomeProject.exe.config\" />
                            <File Id=\"fi_7\" Name=\"Magnum.dll\" Source=\"C:\Git\Test\Publish\wixHelper\Magnum.dll\" />
                            <File Id=\"fi_8\" Name=\"MassTransit.dll\" Source=\"C:\Git\Test\Publish\wixHelper\MassTransit.dll\" />
                        </Component>"
    let executableId = getFileIdFromWiXString wixDirString "\S*.exe"
    Assert.Equal<string>("fi_5", executableId)

[<Fact>]
let ``should find correct component id`` () =
    let wixDirString = "<Component Id=\"comp\" Guid=\"1290b30b-4242-4ed5-99z0-52e3ad1337e6\">
                            <File Id=\"fi_2\" Name=\"Castle.Core.dll\" Source=\"C:\Git\Test\Publish\wixHelper\Castle.Core.dll\" />
                            <File Id=\"fi_3\" Name=\"Castle.Windsor.dll\" Source=\"C:\Git\Test\Publish\wixHelper\Castle.Windsor.dll\" />
                        </Component>
                        <Component Id=\"comp2\" Guid=\"1337b30b-4242-4ed5-99z0-52e3ad1337e6\">
                            <File Id=\"fi_4\" Name=\"FAKE.Test.Abstractions.dll\" Source=\"C:\Git\Test\Publish\wixHelper\FAKE.Test.Abstractions.dll\" />
                            <File Id=\"fi_5\" Name=\"FAKE.Test.SomeProject.exe\" Source=\"C:\Git\Test\Publish\wixHelper\FAKE.Test.SomeProject.exe\" />
                            <File Id=\"fi_6\" Name=\"FAKE.Test.SomeProject.exe.config\" Source=\"C:\Git\Test\Publish\wixHelper\FAKE.Test.SomeProject.exe.config\" />
                            <File Id=\"fi_7\" Name=\"Magnum.dll\" Source=\"C:\Git\Test\Publish\wixHelper\Magnum.dll\" />
                            <File Id=\"fi_8\" Name=\"MassTransit.dll\" Source=\"C:\Git\Test\Publish\wixHelper\MassTransit.dll\" />
                        </Component>"
    let componentIds = getComponentIdsFromWiXString wixDirString
    Assert.Equal<string>("<ComponentRef Id=\"comp\" /><ComponentRef Id=\"comp2\" />", componentIds)


[<Fact>]
let ``should set components never overwrite true if desired`` () =
    let componentDefault = "<Component Id=\"comp\" Guid=\"1290b30b-4242-4ed5-99z0-52e3ad1337e6\">
                                <File Id=\"fi_2\" Name=\"Castle.Core.dll\" Source=\"C:\Git\Test\Publish\wixHelper\Castle.Core.dll\" />
                            </Component>"
    let componentExpected = "<Component NeverOverwrite=\"yes\" Id=\"comp\" Guid=\"1290b30b-4242-4ed5-99z0-52e3ad1337e6\">
                                <File Id=\"fi_2\" Name=\"Castle.Core.dll\" Source=\"C:\Git\Test\Publish\wixHelper\Castle.Core.dll\" />
                            </Component>"
    let actualComponent = setComponentsNeverOverwrite componentDefault
    Assert.Equal<string>(componentExpected, actualComponent)

[<Fact>]
let ``should create valid feature node`` () =
    let actualFeature = generateFeature (fun f -> 
                                                {f with  
                                                    Id = "TestFeatureID"
                                                    Title = "Test Feature Title"
                                                    Level = 1 
                                                    Description = "Test Feature Description" 
                                                    Display = FeatureDisplay.Hidden
                                                    InnerContent = "<Feature Id=\"NestedFeature\" />"
                                                })
    let expectedFeature = "<Feature Id=\"TestFeatureID\" Title=\"Test Feature Title\" Level=\"1\" Description=\"Test Feature Description\" Display=\"hidden\" "
                          + "ConfigurableDirectory=\"INSTALLDIR\"><Feature Id=\"NestedFeature\" /></Feature>"
    Assert.Equal<string>(expectedFeature, actualFeature.ToString())
[<Fact>]
let ``should create valid custom action node`` () =
    let actualAction = generateCustomAction (fun f ->
                                                        {f with
                                                            Id = "TestActionId"
                                                            FileKey = "TestFile"
                                                            Execute = CustomActionExecute.Deferred
                                                            Impersonate = YesOrNo.No
                                                            ExeCommand = "test"
                                                            Return = CustomActionReturn.Check
                                                        })
    let expectedAction =  "<CustomAction Id=\"TestActionId\" FileKey=\"TestFile\" Execute=\"deferred\" Impersonate=\"no\" ExeCommand=\"test\" Return=\"check\" />"
    Assert.Equal<string>(expectedAction, actualAction.ToString())
[<Fact>]
let ``should create valid custom action execution node`` () =
    let actualActionExecution = generateCustomActionExecution (fun f ->
                                                                    {f with 
                                                                        ActionId = "TestActionId"
                                                                        Verb = ActionExecutionVerb.After
                                                                        Target = "InstallFiles"                                                                        
                                                                        Condition = "(NOT WIX_UPGRADE_DETECTED) AND <![CDATA[(&SomeFeature = 3) AND NOT (!SomeFeature = 3)]]>"
                                                                    })
    let expectedActionExecution = "<Custom Action=\"TestActionId\" After=\"InstallFiles\"> (NOT WIX_UPGRADE_DETECTED) AND <![CDATA[(&SomeFeature = 3) AND NOT (!SomeFeature = 3)]]> </Custom>"
    Assert.Equal<string>(expectedActionExecution, actualActionExecution.ToString())
[<Fact>]
let ``should create valid uiref node`` () =
    let actualUiRef = generateUIRef (fun f ->
                                        {f with
                                            Id = "WixUI_Mondo"
                                        })
    let expectedUiRef = "<UIRef Id=\"WixUI_Mondo\" />"
    Assert.Equal<string>(expectedUiRef, actualUiRef.ToString())
[<Fact>]
let ``should create valid upgrade node`` () =
    let actualUpgrade = generateUpgrade (fun f ->
                                          {f with
                                             Id = Guid("E21A46F0-05AF-45D0-A2D6-5E2E3C77F615")
                                             UpgradeVersion = "<UpgradeVersion Minimum=\"1.0.0\" Property=\"SomeProperty\" IncludeMinimum=\"yes\" IncludeMaximum=\"no\" />"
                                          })
                                                 
    let expectedUpgrade = "<Upgrade Id=\"e21a46f0-05af-45d0-a2d6-5e2e3c77f615\"><UpgradeVersion Minimum=\"1.0.0\" Property=\"SomeProperty\" IncludeMinimum=\"yes\" IncludeMaximum=\"no\" /></Upgrade>"
    Assert.Equal<string>(expectedUpgrade, actualUpgrade.ToString())
[<Fact>]
let ``should create valid upgrade version node`` () =
    let actualUpgradeVersion = generateUpgradeVersion (fun f ->
                                                        {f with 
                                                            Minimum = "1.0.0"
                                                            Maximum = "10.0.0"
                                                            IncludeMinimum = YesOrNo.Yes
                                                            IncludeMaximum = YesOrNo.No
                                                            OnlyDetect = YesOrNo.Yes
                                                            Property = "SomeProperty"})
    let expectedUpgradeVersion = "<UpgradeVersion Minimum=\"1.0.0\" OnlyDetect=\"yes\" IncludeMinimum=\"yes\" Maximum=\"10.0.0\" IncludeMaximum=\"no\" Property=\"SomeProperty\" />"
    Assert.Equal<string>(expectedUpgradeVersion, actualUpgradeVersion.ToString())

[<Fact>]
let ``should create valid major upgrade node`` () =
    let actualMajorUpgrade = generateMajorUpgradeVersion(fun f ->
                                                        {f with 
                                                            Schedule = MajorUpgradeSchedule.AfterInstallInitialize
                                                            DowngradeErrorMessage = "A later version is already installed, exiting."
                                                        })
    let expectedMajorUpgrade = "<MajorUpgrade Schedule=\"afterInstallInitialize\" AllowDowngrades=\"no\" DowngradeErrorMessage=\"A later version is already installed, exiting.\" />"
    Assert.Equal<string>(expectedMajorUpgrade, actualMajorUpgrade.ToString())

[<Fact>]
let ``should create valid registry value`` () =
    let actualRegistryValue = generateRegistryValue(fun v -> 
                                                     {v with
                                                        Id = "asdasd"
                                                        Name = "Something"
                                                        Key = "Somewhere"
                                                        Root = Some WiXRegistryRootType.HKU
                                                        Type = WiXRegistryValueType.Integer
                                                        KeyPath = YesOrNo.No
                                                        Value = "2"
                                                     })
    let expectedRegistryValue = "<RegistryValue Id=\"asdasd\" Name=\"Something\" Key=\"Somewhere\" Root=\"HKU\" Type=\"integer\" Value=\"2\" KeyPath=\"no\" />"
    Assert.Equal<string>(expectedRegistryValue, actualRegistryValue.ToString())

[<Fact>]
let ``should create valid registry key`` () =
    let actualRegistryKey = generateRegistryKey(fun k ->
                                                  {k with
                                                    Id = "asdqwe"
                                                    Key = "SomeKey"
                                                    Root = Some WiXRegistryRootType.HKCR
                                                    ForceCreateOnInstall = YesOrNo.Yes
                                                    ForceDeleteOnUninstall = YesOrNo.No
                                                  })
    let expectedRegistryKey = "<RegistryKey Id=\"asdqwe\" Key=\"SomeKey\" Root=\"HKCR\" ForceCreateOnInstall=\"yes\" ForceDeleteOnUninstall=\"no\"></RegistryKey>"
    Assert.Equal<string>(expectedRegistryKey, actualRegistryKey.ToString())

[<Fact>]
let ``can nest registry keys and values`` () =
    let actualRegistryKey = generateRegistryKey(fun k ->
                                                  {k with
                                                    Id = "RootKeyId"
                                                    Key = "RootKey"
                                                    Root = Some WiXRegistryRootType.HKU
                                                    Keys = [
                                                      generateRegistryKey(fun k' ->
                                                                            {k' with
                                                                              Id = "NestedKeyId"
                                                                              Key = "NestedKey"
                                                                            })
                                                    ]
                                                    Values = [
                                                      generateRegistryValue(fun v ->
                                                                              {v with
                                                                                Id = "NestedValue"
                                                                                Value = "SomeValue"
                                                                              })
                                                    ]
                                                    })
    let expectedRegistryKey = "<RegistryKey Id=\"RootKeyId\" Key=\"RootKey\" Root=\"HKU\" ForceCreateOnInstall=\"no\" ForceDeleteOnUninstall=\"no\">" 
                                + "<RegistryKey Id=\"NestedKeyId\" Key=\"NestedKey\" ForceCreateOnInstall=\"no\" ForceDeleteOnUninstall=\"no\"></RegistryKey>" 
                                + "<RegistryValue Id=\"NestedValue\" Type=\"string\" Value=\"SomeValue\" KeyPath=\"no\" />"
                            + "</RegistryKey>"
    Assert.Equal<string>(expectedRegistryKey, actualRegistryKey.ToString())

[<Fact>]
let ``should create valid directory refs`` () =
    let component = generateComponent(fun f -> { f with Id="Nested" })
    let actualRef = generateDirectoryRef(fun f -> { f with 
                                                        Id = "TARGETDIR"
                                                        Components = [C component]
                                                  })
    let expectedRef = sprintf "<DirectoryRef Id=\"TARGETDIR\">%s</DirectoryRef>" (component.ToString())
    Assert.Equal<string>(expectedRef, actualRef.ToString())                                          

[<Fact>]
let ``should create nested features`` () =
    let nestedDir = generateComponentRef(fun f -> { f with Id="Nested"})
    let rootDir = generateComponentRef(fun f -> { f with Id="Root"})

    let innerFeature = generateFeatureElement(fun f -> { f with
                                                            Id = "Inner"
                                                            Components = [nestedDir]
                                                       })

    let nestedFeature = generateFeatureElement(fun f -> { f with
                                                            Id = "Fst"
                                                            NestedFeatures = [innerFeature]
                                                         })

    let sndFeature = generateFeatureElement(fun f -> { f with
                                                            Id = "Snd"
                                                     })

    let feature = generateFeatureElement(fun f -> { f with
                                                        Id = "Complete"
                                                        Components = [rootDir]
                                                        NestedFeatures = [nestedFeature; sndFeature]
                                                  })

    let featureString = feature.ToString()
    let expectedFeatureString = 
        "<Feature Id=\"Complete\" Title=\"Default Feature\" Level=\"1\" Description=\"Default Feature\" Display=\"expand\" ConfigurableDirectory=\"INSTALLDIR\">
            <Feature Id=\"Fst\" Title=\"Default Feature\" Level=\"1\" Description=\"Default Feature\" Display=\"expand\" ConfigurableDirectory=\"INSTALLDIR\">
                <Feature Id=\"Inner\" Title=\"Default Feature\" Level=\"1\" Description=\"Default Feature\" Display=\"expand\" ConfigurableDirectory=\"INSTALLDIR\">
                    <ComponentRef Id=\"Nested\" />
                </Feature>
            </Feature>
            <Feature Id=\"Snd\" Title=\"Default Feature\" Level=\"1\" Description=\"Default Feature\" Display=\"expand\" ConfigurableDirectory=\"INSTALLDIR\"></Feature>
            <ComponentRef Id=\"Root\" />
        </Feature>".Replace("\n", "").Replace("\r", "")

    let cleanResult = Regex.Replace(featureString, @"\s+", "")
    let cleanExpected = Regex.Replace(expectedFeatureString, @"\s+", "")

    Assert.Equal<string>(cleanExpected, cleanResult)