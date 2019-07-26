namespace Fake.Api

#if NETSTANDARD
open System.Net.Http
#else
open System.Net
#endif
open Newtonsoft.Json

/// Contains a task to send notification messages to a [Slack](https://slack.com/) webhook
[<RequireQualifiedAccess>]
module Slack = 
    /// The Slack notification attachment field parameter type
    type NotificationAttachmentFieldParams = {
        /// (Required) The field title
        Title: string
        /// (Required) Text value of the field
        Value: string
        /// Whether the value is short enough to be displayed side-by-side with other values
        Short: bool
    }
    
    /// The Slack notification attachment parameter type
    type NotificationAttachmentParams = {
        /// (Required) Text summary of the attachment that is shown by clients that understand attachments but choose not to show them
        Fallback: string
        /// The title of the attachment
        Title: string
        /// Content to which the title should link
        TitleLink: string
        /// Text that should appear within the attachment
        Text: string
        /// Text that should appear above the formatted data
        Pretext: string
        /// Color of the attachment text. Can be hex-value(e.g. "#AABBCC") or one of "'good', 'warning', 'danger'.
        Color: string
        /// Text to be displayed as a table below the message
        Fields: NotificationAttachmentFieldParams[]
    }
    
    /// The Slack notification parameter type
    type NotificationParams = {
        /// (Required) The message body
        Text: string
        /// Name the message will appear to be sent from. Default value: Specified in your Slack Webhook configuration.
        Username: string
        /// Channel to which the message will be posted. Default value: Specified in your Slack Webhook configuration.
        Channel: string
        /// The icon to be displayed with the message. Default value: Specified in your slack Webhook configuration.
        IconURL: string
        /// The emoji to be displayed with the message. Default value: Specified in your slack Webhook configuration.
        IconEmoji: string
        /// Whether to force inline unfurling of attached links. Default value: false.
        UnfurlLinks: bool
        // Richly formatted message attachments for the notification
        Attachments: NotificationAttachmentParams[]
        // Whether or not to link names of users or channels (beginning with @ or #), Default value : false
        LinkNames: bool
    }
    
    /// The default Slack notification parameters
    let NotificationDefaults = {
        Text = ""
        Username = null
        Channel = null
        IconURL = null
        IconEmoji = null
        UnfurlLinks = false
        Attachments = Array.empty
        LinkNames = false
    }
    
    /// The default parameters for Slack notification attachments
    let NotificationAttachmentDefaults = {
        Fallback = ""
        Title = null
        TitleLink = null
        Text = null
        Pretext = null
        Color = null
        Fields = Array.empty
    }
    
    /// The default parameters for Slack notification attachment fields
    let NotificationAttachmentFieldDefaults = {
        Title = ""
        Value = ""
        Short = false
    }
    
    /// [omit]
    let private lowerCaseContractResolver = { new Newtonsoft.Json.Serialization.DefaultContractResolver() with
        override this.ResolvePropertyName (key : string) =
            key.ToLower()
    }
    
    /// [omit]
    let private ValidateParams webhookURL (param : NotificationParams) =
        if webhookURL = "" then failwith "You must specify a webhook URL"
        if param.Text = "" && param.Attachments.Length = 0 then failwith "You must specify a message or include an attachment"
        let validateField (field : NotificationAttachmentFieldParams) =
            if field.Title = "" then failwith "Each field must have a title"
            if field.Value = "" then failwith "Each field must have a value"
        let validateAttachment (attachment : NotificationAttachmentParams) =
            if attachment.Fallback = "" then failwith "Each attachment must have a fallback"
            Array.iter(fun field -> validateField field) attachment.Fields
        Array.iter(fun attachment -> validateAttachment attachment) param.Attachments
    
        param
    
    /// [omit]
    let private SerializeData data =
        JsonConvert.SerializeObject(data, Formatting.None, new JsonSerializerSettings(NullValueHandling = NullValueHandling.Ignore, ContractResolver = lowerCaseContractResolver))
    
    /// Sends a notification to a Slack Channel
    /// ## Parameters
    ///  - `webhookURL` - The Slack webhook URL
    ///  - `setParams` - Function used to override the default notification parameters
    let sendNotification (webhookURL : string) (setParams: NotificationParams -> NotificationParams) =
        let sendNotification param =
#if NETSTANDARD
            use client = (new HttpClient())
            let response = client.PostAsync(webhookURL, new StringContent(SerializeData param, System.Text.Encoding.UTF8, "application/json")).Result
            response.Content.ReadAsStringAsync().Result
#else
            use client = (new WebClient())
            client.Headers.Add(HttpRequestHeader.ContentType, "application/json")
            client.UploadString(webhookURL, "POST", SerializeData param)
#endif
        NotificationDefaults 
        |> setParams
        |> ValidateParams webhookURL
        |> sendNotification
