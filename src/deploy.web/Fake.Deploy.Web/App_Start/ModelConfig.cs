using Raven.Database.Server;

namespace Fake.Deploy.Web
{
    public class ModelConfig
    {
        public static void Init()
        {
            NonAdminHttp.EnsureCanListenToWhenInNonAdminContext(8082);
            InitialData.Init();
        }
    }
}