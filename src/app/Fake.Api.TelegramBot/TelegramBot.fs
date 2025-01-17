namespace Fake.Api

open System.Net.Http
open System.Text
open Newtonsoft.Json

[<RequireQualifiedAccess>]
module TelegramBot =
    module private TgApiEndpoints =
        [<Literal>]
        let sendMessage = "https://api.telegram.org/bot%s/sendMessage"

    type ParseMode =
        | MarkdownV2
        | Html

        member this.toText() =
            match this with
            | MarkdownV2 -> "MarkdownV2"
            | Html -> "HTML"

    type MessageParams =
        {
          BotToken: string
          ChatId: string
          Text: string
          ParseMode: ParseMode
          DisableNotification: bool }
        
    let MessageParamsDefaults : MessageParams =
        { BotToken = ""
          ChatId = ""
          Text = ""
          ParseMode = MarkdownV2
          DisableNotification = false
          }
        
    let private validateParams messageParams =
        if System.String.IsNullOrWhiteSpace messageParams.BotToken then
            failwith "You must set BokToken value"
            
        if System.String.IsNullOrWhiteSpace messageParams.ChatId then
            failwith "You must set ChatId value"
            
        if System.String.IsNullOrWhiteSpace messageParams.Text then
            failwith "You must set Text value"
            
        messageParams

    let asyncSendMessage messageParams =
        async {
            use httpClient = new HttpClient()

            let body =
                {| chat_id = messageParams.ChatId
                   parse_mode = messageParams.ParseMode.toText ()
                   disable_notification = messageParams.DisableNotification
                   text = messageParams.Text |}

            let json = JsonConvert.SerializeObject(body)
            let url = sprintf TgApiEndpoints.sendMessage messageParams.BotToken
            use jsonContent = new StringContent(json,Encoding.UTF8, "application/json");
            let! response = httpClient.PostAsync(url, jsonContent) |> Async.AwaitTask

            return
                match response.IsSuccessStatusCode with
                | true -> Ok()
                | false -> Error(int response.StatusCode)
        }

    let sendMessage (setParams: MessageParams -> MessageParams) =
        MessageParamsDefaults
        |> setParams
        |> validateParams
        |> asyncSendMessage
        |> Async.RunSynchronously
