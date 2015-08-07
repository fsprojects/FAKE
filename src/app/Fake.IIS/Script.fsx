#r @"..\..\..\packages\Microsoft.Web.Administration\lib\net20\Microsoft.Web.Administration.dll"
#r @"..\..\..\build\FakeLib.dll"

#load "IISHelper.fs"
open Fake.IISHelper
open Microsoft.Web

let siteName = "fake.site"
let appPool = "fake.appPool"
let port = ":8081:"
let vdir = "/fakevdir"
let appDir = @"C:\Users"

UnlockSection "system.webServer/security/authentication/anonymousauthentication"

let dotNetFourAppPool = ApplicationPoolConfig(siteName, allow32on64 = true, identity = Administration.ProcessModelIdentityType.LocalSystem)
let dotNetTwoAppPool = ApplicationPoolConfig(siteName, runtime = "v2.0", allow32on64 = true)

(IIS
  (Site siteName "http" port @"C:\inetpub\wwwroot" appPool)
  (ApplicationPool dotNetFourAppPool)
  (Some(Application vdir appDir)))

(IIS
  (Site siteName "http" port @"C:\inetpub\wwwroot" appPool)
  (ApplicationPool dotNetTwoAppPool)
  (Some(Application "/vdir2" @"C:\temp")))

deleteSite siteName
deleteApplicationPool appPool