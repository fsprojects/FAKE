namespace Fake.Deploy.Web

open System
open System.IO
open System.Net
open System.Web
open log4net

[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
type AgentProxy(dataProvider : IDataProvider) as this = 
    let logger = LogManager.GetLogger(this.GetType().Name)

    let mutable authToken : Guid = Guid.Empty
    let mutable lastCallAt : DateTime = DateTime.MinValue

    let getAgent agentId = dataProvider.GetAgents [ agentId ] |> Seq.tryFind (fun i -> true)
    
    let authenticate (agent : Agent) = 
        match lastCallAt.AddMinutes 10. < DateTime.Now with
        | false -> ()
        | true -> 
            // From config
            let userId = "Test@Fake.org" 
            let keyFile = @"C:\code\github\FAKE2\src\test\Test.Fake.Deploy\TestData\test_rsa"
            let password = "test"
            match Fake.FakeDeployAgentHelper.authenticate (agent.Address.AbsoluteUri + "fake") userId keyFile password with
            | None -> ()
            | Some x -> authToken <- x
    
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    member public this.CallAgent (agentId : string) (urlPart : string) (f : string -> 'T) = 
        match getAgent agentId with
        | None -> failwith (sprintf "Agent %s not found!" agentId)
        | Some agent -> 
            //authenticate agent
            let url = sprintf "%Afake/%s" (agent.Address) urlPart
            let wc = new WebClient()
            wc.Headers.Add("AuthToken", authToken.ToString())
            let str = wc.DownloadString(url)
            lastCallAt <- DateTime.Now
            f str
