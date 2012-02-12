namespace Test.Deployment
{
    public class TestData
    {        
        public static string OutputDir
        {
            get { return @"output\"; }
        }

        public static string GetPackageDir(string package)
        {
            return string.Format("packages\\{0}\\", package);
        }

        public static string GetPackageFile(string version, string package)
        {
            return string.Format("packages\\{0}\\{1}.fakepkg", version, package);
        }
    }
}