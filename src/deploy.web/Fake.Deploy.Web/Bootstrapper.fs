namespace Fake.Deploy.Web
open System
open Nancy
open Nancy.Security
open Nancy.Authentication.Forms
open Fake.Deploy.Web.Data

type Bootstrapper() =
    inherit DefaultNancyBootstrapper()
        
    override this.ConfigureApplicationContainer(container) =
        let m = UserMapper()
        container.Register<IUserMapper, UserMapper>(m) |> ignore
        container.Register<UserMapper, UserMapper>(m) |> ignore

    override this.ApplicationStartup (container, pipelines) =
        //StaticConfiguration.Caching.EnableRuntimeViewUpdates <- true
        StaticConfiguration.EnableRequestTracing <- true
        StaticConfiguration.DisableErrorTraces <- false
        Csrf.Enable pipelines
        Nancy.Json.JsonSettings.MaxJsonLength <- 1024 * 1024

        use c = new Configuration()
        Data.start c

        container.Register<IDataProvider, IDataProvider>(c.Data) |> ignore
        container.Register<IMembershipProvider, IMembershipProvider>(c.Membership) |> ignore     

        base.ApplicationStartup(container, pipelines);
        
    override this.RequestStartup(container, pipelines, context) =
        let formsAuthConfig = FormsAuthenticationConfiguration()
        formsAuthConfig.RedirectUrl <- if Data.isInitialized() then "~/Account/Login" else "~/Setup"
        let u = container.Resolve<IUserMapper>()
        formsAuthConfig.UserMapper <- u
        FormsAuthentication.Enable(pipelines, formsAuthConfig)

