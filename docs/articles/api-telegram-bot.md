# Sending Notifications to a Telegram Bot API

In this article, you will learn how to send notifications using the [Telegram](https://telegram.org) Bot API in FAKE.
This assumes you already have a Telegram bot set up.

## Creating a Telegram Bot & Obtaining API Credentials

Before sending messages, you need to create a Telegram bot and obtain an API token

# 1. Step 1: Create a Telegram Bot
1. Open Telegram and search for @BotFather
2. Start a chat and use the command /newbot.
3. Follow the instructions to name your bot and set a username.
4. Once created, BotFather will provide you with a Bot Token, which looks like:

```makefile
715689912:AAeExvzsJsdfgggsasgagagaagjyuyE9_aZGEv
```

# 2. Step 2: Obtain the Chat ID

To send messages, you need the Chat ID where messages will be sent.

1. Send any message to your bot.
2. Call Telegram's getUpdates API using curl or a browser:

    ```
    curl "https://api.telegram.org/botYOUR_BOT_TOKEN/getUpdates"
    ```

3. Find the "chat" section in the response. The id value is your Chat ID.

## Sending a Notification to Telegram

The following sample FAKE target demonstrates how to send a message to a Telegram bot:

```fsharp
open Fake.Api

// The bot token and chat ID obtained from BotFather and getUpdates

let result =
    TelegramBot.sendMessage (fun p ->
        { p with
            BotToken = "715689912:AAeExvzsJsdfgggsasgagagaagjyuyE9_aZGEv"
            ChatId = "313916395"
            Text = "```csharp\nstring msg = \"Hello, World!\";\n```"
        })

match result with
| Ok _ -> printfn "✅ Message sent successfully"
| Error c -> printfn $"❌ Couldn't send the message. HttpCode: {c}"
```

This example sends a formatted C# code block to Telegram using MarkdownV2.

Expected Output
Once executed, you should see the message appear in the specified Telegram chat, formatted as a code block:

```csharp
string msg = "Hello, World!";
```

By default, messages in FAKE’s Telegram Bot API module use MarkdownV2 for formatting.
However, you can also use HTML formatting by setting ParseMode = Html. See for [details] (https://core.telegram.org/bots/api#formatting-options)

