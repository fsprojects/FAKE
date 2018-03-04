namespace Fake.Deploy.Web

module Global =

    let Version = 
        typeof<Fake.Deploy.Web.AuthenticatedUser>.Assembly.GetName().Version.ToString()

