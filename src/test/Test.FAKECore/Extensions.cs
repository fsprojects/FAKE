using System;
using Microsoft.FSharp.Core;

namespace Test.FAKECore
{
    public static class Extensions
    {
        public static FSharpFunc<T, T2> Convert<T, T2>(this Func<T, T2> func)
        {
            return FSharpFunc<T, T2>.FromConverter(new Converter<T, T2>(func));
        }
    }
}