# Sending Notifications to a Slack Webhook

**Note:  This documentation is for FAKE before version 5 (or the non-netcore version). The new documentation can be found [here](api-slack.html)**

In this article you will learn how to create a [Slack](https://slack.com) webhook integration and send a notification to it. This article assumes that you already have a Slack team setup.

## Adding a Webhook Integration to a Channel

Follow the [instructions](https://my.slack.com/services/new/incoming-webhook/) for setting up an incoming webhook integration. When finished, you should have a Webhook URL that looks like "https://hooks.slack.com/services/some/random/text".

## Sending a Notification to the Webhook

    // The webhook URL from the integration you set up
    let webhookUrl = "https://hooks.slack.com/services/some/random/text"
	
	SlackNotification webhookUrl (fun p ->
        {p with
            Text = "My Slack Notification!\n<https://google.com|Click Here>!"
            Channel = "@SomeoneImportant"
            Icon_Emoji = ":ghost:"
            Attachments = [| 
                {SlackNotificationAttachmentDefaults with
                    Fallback = "Attachment Plain"
                    Text = "Attachment Rich"
                    Pretext = "Attachment Pretext"
                    Color = "danger"
                    Fields = [|
                        {SlackNotificationAttachmentFieldDefaults with
                            Title = "Field Title 1"
                            Value = "Field Value 2"}
                        {SlackNotificationAttachmentFieldDefaults with
                            Title = "Field Title 1"
                            Value = "Field Value 2"}|]
                }
                {SlackNotificationAttachmentDefaults with
                    Fallback = "Attachment 2 Plain"
                    Text = "Attachment 2 Rich"
                    Pretext = "Attachment 2 Pretext"
                    Color = "#FFCCDD"
                }|]
        })
    |> printfn "Result: %s"

The result should look something like this:

![alt text](pics/slacknotification/slacknotification.png "Slack Notification Result")

For additional information on the parameters, check out Slack's [Webhook Documentation](https://api.slack.com/incoming-webhooks)