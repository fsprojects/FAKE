namespace Test.Deployment
{
    public class TestData
    {        
        public static string OutputDir
        {
            get { return "output\\"; }
        }

        public static string GetPackageDir(string package)
        {
            return string.Format("packages\\{0}\\", package);
        }
    }
}