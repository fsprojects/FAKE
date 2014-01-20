using System.IO;
using System.Linq;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.Globbing.TestSample5
{
    public class when_scanning_for_an_absolute_folder : when_extracting_zip
    {
        Because of = () => Files = FileSystem.Include(FullPath("/**/TextFiles/")).ToArray();

        It should_find_the_file =
            () => Files[0].ShouldEndWith(string.Format("Folder1{0}Subfolder1{0}SubFolder2{0}TextFiles", Path.DirectorySeparatorChar));
        
        It should_find_the_file_with_absolute_path =
            () => Files[0].ShouldStartWith(TempDir);
        
        It should_match_1_file = () => Files.Length.ShouldEqual(1);
    }
 }
