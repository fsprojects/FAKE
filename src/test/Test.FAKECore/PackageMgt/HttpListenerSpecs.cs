using System.Net;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.PackageMgt
{
    public class when_searching_for_an_free_port
    {
        static string _port;

        Because of = () => _port = HttpListenerHelper.getFirstFreePort();

        It should_find_a_port = () => _port.ShouldNotBeEmpty();
    }

    [Tags("HTTP")]
    public class when_starting_the_http_server
    {
        protected const string ServerName = "localhost";
        const string Port = "*";
        protected static HttpListenerHelper.Listener Listener;

        Cleanup after = () => Listener.Cancel();

        Establish context = () => Listener = HttpListenerHelper.startWithConsoleLogger(ServerName, Port, HttpListenerHelper.CreateDefaultRequestMap());
    }

    [Tags("HTTP")]
    public class when_retrieving_the_status : when_starting_the_http_server
    {
        static string _text;
        Because of = () => _text = new WebClient().DownloadString(Listener.RootUrl);

        It should_serve_the_status_page = () => _text.ShouldEqual("Http listener is running");
    }

    [Tags("HTTP")]
    public class when_retrieving_a_unknown_route : when_starting_the_http_server
    {
        static string _text;
        Because of = () => _text = new WebClient().DownloadString(Listener.RootUrl + "somthingsilly/");

        It should_serve_the_status_page = () => _text.ShouldStartWith("Unknown route");
    }
}