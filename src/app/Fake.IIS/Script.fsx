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
let path =  @"C:\inetpub\wwwroot"

UnlockSection "system.webServer/security/authentication/anonymousauthentication"

let site = SiteConfig(siteName, port, path, appPool)
let dotNetFourAppPool = ApplicationPoolConfig(siteName, allow32on64 = true, identity = Administration.ProcessModelIdentityType.LocalSystem)
let dotNetTwoAppPool = ApplicationPoolConfig(siteName, runtime = "v2.0", allow32on64 = true)

(IIS
  (Site site)
  (ApplicationPool dotNetFourAppPool)
  (Some(Application vdir appDir)))

(IIS
  (Site site)
  (ApplicationPool dotNetTwoAppPool)
  (Some(Application "/vdir2" @"C:\temp")))

deleteSite siteName
deleteApplicationPool appPool