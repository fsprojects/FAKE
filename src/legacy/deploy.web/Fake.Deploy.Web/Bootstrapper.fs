namespace Fake.Deploy.Web
open System
open Nancy
open Nancy.Security
open Nancy.Authentication.Forms
open Fake.Deploy.Web.Data
open Newtonsoft.Json

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type Bootstrapper() =
    inherit DefaultNancyBootstrapper()

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]    
    override this.ConfigureApplicationContainer(container) =
        let m = UserMapper()
        container.Register<IUserMapper, UserMapper>(m) |> ignore
        container.Register<UserMapper, UserMapper>(m) |> ignore
        let c = new Configuration()
        Data.start c
        container.Register<Configuration>(c) |> ignore
        
        let fd (x: TinyIoc.TinyIoCContainer) (y : TinyIoc.NamedParameterOverloads) = c.Data
        container.Register<IDataProvider>(fd) |> ignore
        
        let fm (x: TinyIoc.TinyIoCContainer) (y : TinyIoc.NamedParameterOverloads) = c.Membership
        container.Register<IMembershipProvider>(fm) |> ignore

        container.Register<AgentProxy, AgentProxy>().AsSingleton() |> ignore

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    override this.ApplicationStartup (container, pipelines) =
        //StaticConfiguration.Caching.EnableRuntimeViewUpdates <- true
        StaticConfiguration.EnableRequestTracing <- true
        StaticConfiguration.DisableErrorTraces <- false
        Csrf.Enable pipelines
        
        Nancy.Json.JsonSettings.MaxJsonLength <- 1024 * 1024

        base.ApplicationStartup(container, pipelines);

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]    
    override this.RequestStartup(container, pipelines, context) =
        let c = container.Resolve<Configuration>()

        let formsAuthConfig = FormsAuthenticationConfiguration()
        formsAuthConfig.RedirectUrl <- if c.IsConfigured then "~/Account/Login" else "~/Setup"
        let u = container.Resolve<IUserMapper>()
        formsAuthConfig.UserMapper <- u
        FormsAuthentication.Enable(pipelines, formsAuthConfig)

