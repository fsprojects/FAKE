using System;
using System.Reflection;
using System.Web.UI;

namespace Fake_WebSite
{
    public partial class _Default : Page
    {
        protected string Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}