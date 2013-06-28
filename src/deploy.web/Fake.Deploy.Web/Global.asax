<%@ Application Inherits="Fake.Deploy.Web.Global, Fake.Deploy.Web.App" Language="C#" %>
<script Language="C#" RunAt="server">

  protected void Application_Start(Object sender, EventArgs e) {
      base.Start(); 
  }

  protected void Application_AuthenticateRequest(Object sender, EventArgs e)
  {
      base.AuthenticateRequest();
  }

  protected void Application_End(Object sender, EventArgs e)
  {
      base.End();
  }

</script>
