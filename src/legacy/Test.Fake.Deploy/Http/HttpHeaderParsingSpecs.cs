using Fake;
using Machine.Specifications;

namespace Test.Fake.Deploy.Http
{
    public class when_parsing_values_without_commas
    {
        private static string[] _results;

        private Because of = () => _results = HttpHeaderHelper.fromHeaderValue("\"param%22one\",\"second\"");

        private It should_parse_params_with_quotes = () =>
            _results[0].ShouldEqual("param\"one");
        private It should_parse_params_without_quotes = () =>
            _results[1].ShouldEqual("second");
    }


    public class when_parsing_values_with_commas
    {
        private static string[] _results;

        private Because of = () => _results = HttpHeaderHelper.fromHeaderValue("\"param,%22one\",\"second\",\"another, %22 parameter\"");

        private It should_parse_params_with_comma_and_quotes = () =>
            _results[0].ShouldEqual("param,\"one");
        private It should_parse_params_without_quotes = () =>
            _results[1].ShouldEqual("second");
        private It should_parse_third_param = () =>
            _results[2].ShouldEqual("another, \" parameter");
    }

    public class when_parsing_unquoted_params
    {
        private static string[] _results;

        private Because of = () => _results = HttpHeaderHelper.fromHeaderValue("simple command");

        private It should_parse_exact_value = () =>
            _results[0].ShouldEqual("simple command");
    }


}