module Test.Fake.Deploy.Web.WixHelper

open System
open Fake
open Xunit

[<Fact>]
let ``should find correct id`` () =
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