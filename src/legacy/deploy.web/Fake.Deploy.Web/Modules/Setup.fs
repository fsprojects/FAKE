namespace Fake.Deploy.Web.Module
open Fake.Deploy.Web
open Fake.Deploy.Web.Data
open Fake.Deploy.Web.Module.NancyOp
open Nancy
open Nancy.ModelBinding
open Nancy.Authentication.Forms

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type Setup (config : Configuration) as http =
    inherit FakeModule("setup")

    do
        http.get "/" (fun p ->
            // Why do you have to be un-authenticated to setup ?
            if http.Context.CurrentUser <> null // http.Request.IsAuthenticated
            then
                http.LogoutAndRedirect "/setup" |> box
            else
                let appInfo = 
                    {
                        AdministratorUserName = ""; AdministratorEmail=""; 
                        AdministratorPassword="";ConfirmAdministratorPassword="";
                        DataProvider=""; DataProviderParameters="";
                        MembershipProvider=""; MembershipProviderParameters="";
                        AvailableDataProviders = config.DataProviders |> Seq.map(fun p -> p.Id) |> Array.ofSeq
                        AvailableMembershipProviders = config.MembershipProviders |> Seq.map(fun p -> p.Id) |> Array.ofSeq
                        DataProviderParametersDescription = 
                            config.DataProviders
                            |> Seq.map(fun dp -> dp.Id, dp.ParameterDescriptions)
                            |> dict
                        MembershipProviderParametersDescription =
                            config.MembershipProviders
                            |> Seq.map(fun dp -> dp.Id, dp.ParameterDescriptions)
                            |> dict
                        UseFileUpload = false
                        UseNuGetFeedUpload = false
                        NuGetFeeds = [||]
                    }
                http.View.["setup"].WithModel appInfo |> box
        )

        http.post "/save" (fun p ->
            let model = http.Bind<SetupInfo>()
            Data.init (config) model
            Data.saveSetupInfo model
            http.Response.AsText("").WithStatusCode HttpStatusCode.Created
        )
        
