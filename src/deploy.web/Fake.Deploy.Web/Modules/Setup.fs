namespace Fake.Deploy.Web.Module
open Fake.Deploy.Web
open Fancy
open Nancy
open Nancy.ModelBinding
open Nancy.Authentication.Forms


type Setup (dataProvider : IDataProvider) as http =
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
                        AvailableDataProviders = Data.dataProviders() |> Seq.map(fun p -> p.Id) |> Array.ofSeq
                        AvailableMembershipProviders = Data.membershipProviders() |> Seq.map(fun p -> p.Id) |> Array.ofSeq
                        DataProviderParametersDescription = 
                            Data.dataProviders()
                            |> Seq.map(fun dp -> dp.Id, dp.ParameterDescriptions)
                            |> dict
                        MembershipProviderParametersDescription =
                            Data.membershipProviders()
                            |> Seq.map(fun dp -> dp.Id, dp.ParameterDescriptions)
                            |> dict
                        UseFileUpload = false
                        UseNuGetFeedUpload = false
                        NuGetFeeds = [||]
                    }
                http.View.["setup"].WithModel appInfo |> box
        )

        http.post "/setup/save" (fun p ->
            let model = http.Bind<SetupInfo>()
            Data.init (new Data.Configuration()) model
            Data.saveSetupInfo model
            http.Response.AsRedirect "/"
        )
        
