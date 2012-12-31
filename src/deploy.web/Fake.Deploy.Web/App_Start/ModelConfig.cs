using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Raven.Client.Indexes;

namespace Fake.Deploy.Web
{
    public class ModelConfig
    {
        public static void Init()
        {
            Fake.Deploy.Web.Model.Init(new[] { typeof(ModelConfig).Assembly });
        }

    }
}