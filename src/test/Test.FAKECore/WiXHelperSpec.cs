// ReSharper disable InconsistentNaming 
// ReSharper disable CheckNamespace

using System;
using Fake;
using FSharp.Testing;
using Machine.Specifications;
using Microsoft.FSharp.Core;

namespace Test.FAKECore.WiXHelperSpec
{
	internal class When_creating_a_WiDir
	{
		private static WiXHelper.WiXDir Parameters;
		private static WiXHelper.WiXDir Arguments;

		Establish context = () => {
			Parameters = WiXHelper.WiXDirDefaults
				.With(p => p.Id, "1")
				.With(p => p.Name, "Foo");
		};

		Because of = () => {
			Func<WiXHelper.WiXDir, WiXHelper.WiXDir> f = _ => Parameters;
			var fs = FuncConvert.ToFSharpFunc(new Converter<WiXHelper.WiXDir, WiXHelper.WiXDir>(f));
			Arguments = WiXHelper.generateDirectory(fs);
			Console.WriteLine(Arguments.ToString());
		};

		private It should_return_a_proper_XML_tag_on_method_call_ToString = () => Arguments.ToString()
			.ShouldContain("<Directory Id=\"1\" Name=\"Foo\"></Directory");
	}
}