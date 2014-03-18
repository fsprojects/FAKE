namespace Fake.Deploy.Web

module Global =

    let Version = 
        typeof<Global>.Assembly.GetName().Version.ToString()

