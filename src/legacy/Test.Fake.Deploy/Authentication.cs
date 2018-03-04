using System;
using System.Collections.Generic;
using System.IO;
using Fake;
using Fake.Deploy;
using Machine.Specifications;
using Microsoft.FSharp.Core;
using Nancy;
using Nancy.Testing;
using Renci.SshNet;
using Test.FAKECore;

namespace Test.Fake.Deploy
{
    public class Keys
    {
        public static readonly PrivateKeyFile PrivateKey;

        public static readonly SshRsaModule.PublicKey[] PublicKeys;

        static Keys()
        {
            string basePath = Path.Combine(Path.GetDirectoryName(new Uri(typeof(Keys).Assembly.CodeBase).LocalPath), "TestData");
            PublicKeys = SshRsaModule.loadPublicKeys(Path.Combine(basePath, "authorized_keys"));
            PrivateKey = new PrivateKeyFile(Path.Combine(basePath, "test_rsa"), "test");
        }
    }

    public class when_successfully_authenticating_user
    {
        private static readonly Guid Ticket = Guid.NewGuid();

        It Should_return_a_session_ticket = () =>
        {
            Func<Unit, Guid> f = x => Ticket;
            var browser = new Browser(with =>
            {
                with.Module<Auth.AuthModule>();
                with.Dependency(new UserMapper(f.Convert()));
                with.Dependency(new List<SshRsaModule.PublicKey>(Keys.PublicKeys).ToFSharpList());
                with.Dependency(new Auth.LoginRequests());
            });

            var challengeResponse = browser.Get("/fake/login/Test@Fake.org");
            challengeResponse.StatusCode.ShouldEqual(HttpStatusCode.OK);
            var challenge = Convert.FromBase64String(challengeResponse.Body.AsString());
            var signature = Keys.PrivateKey.HostKey.Sign(challenge);
            var loginResponse = browser.Post("/fake/login", ctx =>
            {
                ctx.FormValue("challenge", Convert.ToBase64String(challenge));
                ctx.FormValue("signature", Convert.ToBase64String(signature));
            });
            loginResponse.StatusCode.ShouldEqual(HttpStatusCode.OK);
            loginResponse.Body.AsString().ShouldNotBeEmpty();
            loginResponse.Body.AsString().ShouldEqual("\"" + Ticket + "\"");
        };
    }
}