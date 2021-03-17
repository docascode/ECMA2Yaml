using System;
using System.IO;
using System.Text.RegularExpressions;

namespace FixCodeSnippet
{
    class Program
    {
        static Regex smallOpenRegex = new Regex("<snippet", RegexOptions.Compiled);
        static Regex smallCloseRegex = new Regex("</snippet", RegexOptions.Compiled);
        static Regex bigOpenRegex = new Regex("<Snippet", RegexOptions.Compiled);
        static Regex bigCloseRegex = new Regex("</Snippet", RegexOptions.Compiled);

        static void Main(string[] args)
        {
            System.IO.DirectoryInfo info = new DirectoryInfo(@"E:\mdoc\ECMA2YamlTestRepo2\fulldocset\add\codesnippet");
            WalkDirectoryTree(info);
            Console.WriteLine();
        }

        static void WalkDirectoryTree(System.IO.DirectoryInfo root)
        {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            files = root.GetFiles("*.*");

            if (files != null)
            {
                foreach (System.IO.FileInfo fi in files)
                {
                    var text = File.ReadAllText(fi.FullName);
                    int smallOpen = smallOpenRegex.Matches(text).Count;
                    int smallClose = smallCloseRegex.Matches(text).Count;
                    int bigOpen = bigOpenRegex.Matches(text).Count;
                    int bigClose = bigCloseRegex.Matches(text).Count;
                    if ((smallOpen != smallClose || bigOpen != bigClose))
                    {
                        Console.WriteLine(fi.FullName + string.Format(" : {0},{1},{2},{3}", smallOpen, smallClose, bigOpen, bigClose));
                    }
                }

                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    // Resursive call for each subdirectory.
                    WalkDirectoryTree(dirInfo);
                }
            }
        }
    }
}
