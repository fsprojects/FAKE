/// Contains a task to send notification messages to a [Office 356 Connector](https://dev.outlook.com/connectors/reference) webhook
/// 
/// ## Sample
///
///     let imageUrl = sprintf "https://connectorsdemo.azurewebsites.net/images/%s" 
///     
///     let notification p =
///         { p with
///             Summary = Some "Max Muster ran a build"
///             Title = Some "Sample Project"
///             Sections =
///                [ { SectionDefaults with 
///                      ActivityTitle = Some "Max Muster"
///                      ActivitySubtitle = Some "on Sample Project" 
///                      ActivityImage = 
///                         imageUrl "MSC12_Oscar_002.jpg" 
///                         |> ImageUri.FromUrl
///                         |> Some 
///                  }
///                  { SectionDefaults with
///                      Title = Some "Details"
///                      Facts = [ { Name = "Labels"; Value = "FOO, BAR" }
///                                { Name = "Version"; Value = "1.0.0" }
///                                { Name = "Trello Id"; Value = "1101" } ]
///                  } 
///                ]
///             PotentialActions =
///                [
///                  {
///                    Name = "View in Trello"
///                    Target = System.Uri("https://trello.com/c/1101/")
///                  }
///                ]
///         }
///     
///     let webhookURL = "<YOUR WEBHOOK URL>"
///     
///     Office365Notification webhookURL notification |> ignore
///

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
module Fake.Office365ConnectorHelper

open System.Net
open System
open Newtonsoft.Json

/// This type alias for string gives you a hint where you can use markdown
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type MarkdownString = string

/// This type alias for string gives you a hint where you **can't** use markdown
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type SimpleString = string

/// This type alias gives you a hint where you have to use a Hex color value (e.g. #AAFF77)
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type ColorHexValue = string

/// [omit]
let inline private writeJson (w: JsonWriter) (x: ^T) = (^T: (member WriteJson: JsonWriter -> JsonWriter) x, w)

/// [omit]
let private writePropertyName title (writer: JsonWriter) =
    writer.WritePropertyName(title)
    writer

/// [omit]
let private writeString (value: string) (writer: JsonWriter) =
    writer.WriteValue(value)
    writer

/// [omit]
let private writeNamedString title value (writer: JsonWriter) =
    writer
    |> writePropertyName title
    |> writeString value

/// [omit]
let private writeNonEmptyValue title value (writer: JsonWriter) =
    match value with
    | Some v when v |> isNotNullOrEmpty ->
        writer 
        |> writePropertyName title
        |> writeString v
    | _ -> writer

/// [omit]
let private asList title (writeValues: JsonWriter -> JsonWriter) (writer: JsonWriter) =
    writer.WritePropertyName(title)
    writer.WriteStartArray()
    writer |> writeValues |> ignore
    writer.WriteEndArray()
    writer

/// [omit]
let private asObject (writeValues: JsonWriter -> JsonWriter) (writer: JsonWriter) =
    writer.WriteStartObject()
    writer |> writeValues |> ignore
    writer.WriteEndObject()
    writer

/// Represents an action button
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type ViewAction = 
    {
        /// (Required) The name of the Action (appears on the button).
        Name: SimpleString 

        /// (Required) The Url of the link for the button
        Target: Uri 
    }

    /// Writes the action to a JSON writer
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member self.WriteJson (writer: JsonWriter) =
        writer |> asObject (fun _ -> 
            writer
            |> writeNamedString "@context" "http://schema.org"
            |> writeNamedString "@type" "ViewAction"
            |> writeNamedString "name" self.Name
            |> asList "target" (fun _ -> writer |> writeString (self.Target.ToString())))

        
/// Represents the URI to an image (either a normal URI or a DataUri)
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type ImageUri =
    /// A simple URI of the image
    | ImageUrl of Uri

    /// A Data uri of the image encoded as Base64 data
    | DataUri of string

    /// Writes the image uri to a JSON writer
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member self.WriteJson (writer: JsonWriter) =
        match self with
        | ImageUrl uri -> writer |> writeString (uri.ToString())
        | DataUri uri -> writer |> writeString uri
        
    /// Creates a new ImageUrl from a given url string
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    static member FromUrl url =
        System.Uri(url) |> ImageUrl

    /// Creates a new DataUri from a given file
    /// png, gif, jpg and bmp files are supported
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    static member FromFile fileName =
        let allowedExtensions = [ "png"; "gif"; "jpg"; "bmp" ]
        let extension = System.IO.Path.GetExtension(fileName) |> toLower

        match extension with
        | ext when (allowedExtensions |> List.contains ext) ->
            let imageBytes = System.IO.File.ReadAllBytes(fileName)
            let data = Convert.ToBase64String(imageBytes, Base64FormattingOptions.None)

            DataUri (sprintf "data:image/%s,base64,%s" ext data) |> Some
        | _ -> 
            traceError (sprintf "Extension \"%s\" is not supported!" extension)
            None
        
/// A simple key/value pair
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type Fact = 
    { 
        /// (Required) Name of the fact
        Name: SimpleString
        
        /// (Required) Value of the fact
        Value: MarkdownString 
    }

    /// Writes the fact to a JSON writer
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member self.WriteJson (writer: JsonWriter) =
        writer |> asObject (fun _ ->
            writer
            |> writeNamedString "name" self.Name
            |> writeNamedString "value" self.Value)

/// Represents a described image object
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type Image = 
    {
        /// (Optional) Alt-text for the image
        Title: SimpleString option

        /// (Required) A URL to the image file or a data URI with the base64-encoded image inline
        Image: ImageUri 
    }

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    static member FromUrlWithoutTitle url = 
        { Title = None
          Image = url |> ImageUri.FromUrl } 

    /// Writes the Image to a JSON writer
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member self.WriteJson (writer: JsonWriter) =
        writer |> asObject (fun w ->
            w
            |> writeNonEmptyValue "title" self.Title
            |> writePropertyName "image"
            |> self.Image.WriteJson)

/// A section in a ConnectorCard
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type Section = 
    { 
        /// (Optional) The title of the section
        Title: MarkdownString option

        /// (Optional) Title of the event or action. Often this will be the name of the "actor".
        ActivityTitle: MarkdownString option

        /// (Optional) A subtitle describing the event or action. Often this will be a summary of the action.
        ActivitySubtitle: MarkdownString option

        /// (Optional) An image representing the action. Often this is an avatar of the "actor" of the activity.
        ActivityImage: ImageUri option

        /// (Optional) A full description of the action.
        ActivityText: MarkdownString option

        /// A list of facts, displayed as key-value pairs.
        Facts: Fact list

        /// A list of images that will be displayed at the bottom of the section.
        Images: Image list

        /// (Optional) A text that will appear before the activity.
        Text: string option

        /// (Optional) Set this to false to disable markdown parsing on this section's content. Markdown parsing is enabled by default.
        IsMarkdown: bool option

        /// This list of ViewAction objects will power the action links found at the bottom of the section
        PotentialActions: ViewAction list
    }

    /// Writes the Section to a JSON writer
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member self.WriteJson (writer: JsonWriter) =
        writer |> asObject (fun _ ->
            writer
            |> writeNonEmptyValue "title" self.Title
            |> writeNonEmptyValue "activityTitle" self.ActivityTitle
            |> writeNonEmptyValue "activitySubtitle" self.ActivitySubtitle
            |> writeNonEmptyValue "activityText" self.ActivityText
            |> fun _ -> match self.ActivityImage with
                        | Some i -> writer |> writePropertyName "activityImage" |> i.WriteJson
                        | _ -> writer
            |> fun _ -> match self.Facts with
                        | [] -> writer
                        | _ -> writer |> asList "facts" (fun _ -> self.Facts |> List.fold writeJson writer)

            |> fun _ -> match self.Images with
                        | [] -> writer
                        | _ -> writer |> asList "images" (fun _ -> self.Images |> List.fold writeJson writer)
            |> fun _ -> match self.IsMarkdown with
                        | Some false -> writer.WritePropertyName("markdown")
                                        writer.WriteValue(false)
                                        writer
                        | _ -> writer
            |> fun _ -> match self.PotentialActions with
                        | [] -> writer
                        | _ -> writer |> asList "potentialAction" (fun _ -> self.PotentialActions |> List.fold writeJson writer))

/// This is the base data, which will be sent to the Office 365 webhook connector
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type ConnectorCard = 
    {
        /// (Required, if the text property is not populated) A string used for summarizing card content. This will be shown as the message subject.
        Summary: SimpleString option

        /// (Optional) A title for the Connector message. Shown at the top of the message.
        Title: SimpleString option

        /// The main text of the card. This will be rendered below the sender information and optional title, and above any sections or actions present.
        Text: MarkdownString option

        /// (Optional) Accent color used for branding or indicating status in the card
        ThemeColor: ColorHexValue option

        /// Contains a list of sections to display in the card
        Sections: Section list

        /// This array of ViewAction objects will power the action links found at the bottom of the card
        PotentialActions: ViewAction list 
    }

    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member self.WriteJson (writer: JsonWriter) =
        writer |> asObject (fun _ ->
            writer
            |> writeNonEmptyValue "summary" self.Summary
            |> writeNonEmptyValue "title" self.Title
            |> writeNonEmptyValue "text" self.Text
            |> writeNonEmptyValue "themeColor" self.ThemeColor
            |> fun _ -> match self.Sections with
                        | [] -> writer
                        | _ -> writer |> asList "sections" (fun _ -> self.Sections |> List.fold writeJson writer)
            |> fun _ -> match self.PotentialActions with
                        | [] -> writer
                        | _ -> writer |> asList "potentialAction" (fun _ -> self.PotentialActions |> List.fold writeJson writer))

    /// Converts the connector card to a JSON string
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    member self.AsJson() =
        let sb = System.Text.StringBuilder()
        let sw = new System.IO.StringWriter(sb)
        use writer = new JsonTextWriter(sw)
        writer |> self.WriteJson |> ignore
        sb.ToString ()

/// Default values for a Section in a ConnectorCard (everything is empty here)
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let SectionDefaults = 
    {
        Title = None
        ActivityTitle = None
        ActivitySubtitle = None
        ActivityImage = None
        ActivityText = None
        Facts = []
        Images = []
        Text = None
        IsMarkdown = None
        PotentialActions = []
    }

/// Default values for a ConnectorCard (everything is empty here)
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let ConnectorCardDefaults = 
    {
        Summary = None
        Title = None
        Text = None
        ThemeColor = None
        Sections = []
        PotentialActions = []
    }

/// [omit]
let private validateParams webhookURL (card : ConnectorCard) =
    if webhookURL = "" then failwith "You must specify a webhook URL"
    if card.Text.IsNone && card.Sections.Length = 0 then failwith "You must specify a message or include a section"

    let validateAction (action: ViewAction) = 
        if action.Name |> isNullOrEmpty then
            failwith "You must specifiy a name for a ViewAction"
        
    let validateSection (section: Section) =
        if section.Text.IsNone && section.ActivityText.IsNone && section.ActivityTitle.IsNone && section.Facts.Length = 0 && section.Images.Length = 0 then
            failwith "You must specifiy a text or an activityText/activityTitle or some facts or some images in a section"
        section.PotentialActions |> List.iter validateAction
        ()
    
    card.Sections |> List.iter validateSection
    card.PotentialActions |> List.iter validateAction
    
    card

/// Sends a notification to an Office 365 Connector
/// ## Parameters
///  - `webhookURL` - The Office 365 webhook connector URL
///  - `setParams` - Function used to override the default notification parameters
[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
let Office365Notification (webhookURL : string) (setParams: ConnectorCard -> ConnectorCard) =
    let sendNotification (card: ConnectorCard) =
        use client = (new WebClient())

        client.Headers.Add(HttpRequestHeader.ContentType, "application/json")
        client.UploadString(webhookURL, "POST", card.AsJson ())

    ConnectorCardDefaults 
    |> setParams
    |> validateParams webhookURL
    |> sendNotification
