namespace Test.FAKECore

open System.Runtime.CompilerServices
[<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]

[<Extension>]
type public FSharpFuncUtil = 

    [<Extension>] 
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    static member ToFSharpFunc<'a> (func : System.Func<'a>) = fun () -> func.Invoke()

    [<Extension>] 
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    static member ToFSharpFunc<'a,'b> (func : System.Func<'a,'b>) = fun x -> func.Invoke(x)

    [<Extension>] 
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    static member ToFSharpFunc<'a,'b,'c> (func : System.Func<'a,'b,'c>) = fun x y -> func.Invoke(x,y)

    [<Extension>] 
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    static member ToFSharpFunc<'a,'b,'c,'d> (func : System.Func<'a,'b,'c,'d>) = fun x y z -> func.Invoke(x,y,z)

    [<Extension>] 
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    static member ToFSharpFunc<'a> (func : System.Action) = fun () -> func.Invoke()

    [<Extension>] 
    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    static member ToFSharpFunc<'a> (func : System.Action<'a>) = fun x -> func.Invoke(x)

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    static member Create<'a,'b> (func : System.Func<'a,'b>) = FSharpFuncUtil.ToFSharpFunc func

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    static member Create<'a,'b,'c> (func : System.Func<'a,'b,'c>) = FSharpFuncUtil.ToFSharpFunc func

    [<System.Obsolete("This function, type or module is obsolete. There is no alternative in FAKE 5 yet. If you need this functionality consider porting the module (https://fake.build/contributing.html#Porting-a-module-to-FAKE-5).")>]
    static member Create<'a,'b,'c,'d> (func : System.Func<'a,'b,'c,'d>) = FSharpFuncUtil.ToFSharpFunc func

