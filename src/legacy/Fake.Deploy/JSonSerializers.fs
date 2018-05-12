namespace Fake
open Fake.Json
open Newtonsoft.Json
open Nancy.Serialization.JsonNet

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type JsonSerializerForFsharp =
    inherit JsonNetSerializer
    
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    new (s:JsonSerializer) =
        { inherit JsonNetSerializer(s) }

[<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
type JsonBodySerializerForFsharp =
    inherit JsonNetBodyDeserializer
    
    [<System.Obsolete("This API is obsolete. There is no alternative in FAKE 5 yet. You can help by porting this module.")>]
    new (s:JsonSerializer) =
        { inherit JsonNetBodyDeserializer(s) }

