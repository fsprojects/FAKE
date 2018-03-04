module Test.Fake.Deploy.Web.NancyTestHelpers
open Nancy
open Nancy.Testing
open Newtonsoft.Json
open Xunit
open Fake.Deploy.Web
open Fake.Deploy.Web.Data

type TestingRootPathProvider =
    inherit System.Object

    interface IRootPathProvider with
        member x.GetRootPath() = Data.appdata.FullName
        member x.Equals o = x.Equals o
        member x.GetHashCode() = x.GetHashCode()
        member x.GetType() = x.GetType()
        member x.ToString() = x.ToString()

let dataProvider = new InMemoryDataProvider() :> IDataProvider
let membershipProvider = new InMemoryMembershipProvider() :> IMembershipProvider

let createBrowser<'TModule when 'TModule :> INancyModule> () =
    let configureBrowser (c : ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator) =
        let t = typedefof<'TModule>
        c.Module<'TModule>()
         .ApplicationStartup(fun c x -> 
            c.Register<IDataProvider, IDataProvider>(dataProvider) |> ignore
            c.Register<IMembershipProvider, IMembershipProvider>(membershipProvider) |> ignore
         )
        |> ignore

    Browser(configureBrowser)

let get<'TModule, 'T  when 'TModule :> INancyModule> (url : string) =
    let browser = createBrowser<'TModule>()
    let response = browser.Get url
    Assert.Equal(HttpStatusCode.OK, response.StatusCode)
    JsonConvert.DeserializeObject<'T>(response.Body.AsString())

let post<'T, 'T2> (browser : Browser) (uri:string) (postData:'T2) =
    let content (c : BrowserContext) =
        c.Body(JsonConvert.SerializeObject postData)
        c.Header("content-type", "application/json")

    let response = browser.Post(uri, content)
    Assert.Equal(HttpStatusCode.Created, response.StatusCode)
    JsonConvert.DeserializeObject<'T>(response.Body.AsString())
