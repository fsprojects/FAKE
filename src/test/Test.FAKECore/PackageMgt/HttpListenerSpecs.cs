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

    public class when_starting_the_http_server
    {
        const string ServerName = "localhost";
        static HttpListenerHelper.Listener _listener;

        Cleanup after = () => _listener.Cancel();

        Because of = () => _listener = HttpListenerHelper.startWithConsoleLogger(ServerName, "*", HttpListenerHelper.StatusRequestMap);

        It should_serve_the_status_page =
            () => new WebClient().DownloadString(string.Format("http://{0}:{1}/fake/", ServerName, _listener.Port))
                      .ShouldEqual("Http listener is running");
    }
}