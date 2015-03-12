/// Contains a task to send notification messages to a [HipChat](https://www.hipchat.com/) room
module Fake.HipChatNotificationHelper

open System
open System.Web
open Fake

/// The HipChat notification paramater type
type HipChatNotificationParams = {
    /// (Required) Auth token from HipChat
    AuthToken: string
    /// (Required) ID or name of the room to send the notification to
    RoomId: string
    /// (Required) Name the message will appear to be sent from
    From: string
    /// (Required) The message body
    Message: string
    /// The message format, which can either be text or html. Default value: text
    MessageFormat: string
    /// Whether or not this message should trigger a notification for people in the room. Default value: false
    Notify: bool
    /// The background color for the message, which can be yellow, red, green, purple, gray, or random. Default value: yellow
    Color: string
}

/// The default HipChat notification parameters
let HipChatNotificationDefaults = {
    AuthToken = ""
    RoomId = ""
    From = ""
    Message = ""
    MessageFormat = "text"
    Notify = false
    Color = "yellow"
}

/// [omit]
let validateParams param =
    if param.AuthToken = "" then failwith "You must provide your auth token"
    if param.RoomId = "" then failwith "You must specify which room to notify"
    if param.From = "" then failwith "You must specify the name to send the message from"
    if param.Message = "" then failwith "You must provide a message to send"

    param

/// Sends a notification to a HipChat room
/// ## Parameters
///  - `setParams` - Function used to override the default notification parameters
let HipChatNotification (setParams: HipChatNotificationParams -> HipChatNotificationParams) =
    let sendNotification param =
        ["room_id", param.RoomId
         "from", param.From
         "message", param.Message
         "message_format", param.MessageFormat
         "notify", Convert.ToInt32(param.Notify).ToString()
         "color", param.Color
         "format", "json"]
        |> List.map(fun (key, value) -> key + "=" + Uri.EscapeDataString(value))
        |> String.concat "&"
        |> fun curlData -> String.Format("-s -d {0} https://api.hipchat.com/v1/rooms/message?auth_token={1}&format=json", curlData, param.AuthToken)
        |> fun curlArgs -> Shell.Exec("curl", curlArgs)

    HipChatNotificationDefaults 
    |> setParams
    |> validateParams
    |> sendNotification