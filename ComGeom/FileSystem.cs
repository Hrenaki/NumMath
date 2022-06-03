using System;
using System.Collections.Generic;
using System.Text;

namespace ComGeom
{
    internal enum FileExtensions
    {
        Object,
        Expmesh
    }

    internal static class FileSystem
    {
        private static Dictionary<FileExtensions, string> fileExtensions = new Dictionary<FileExtensions, string>() 
        { 
            { FileExtensions.Object, ".obj" },
            { FileExtensions.Expmesh, ".expmesh" }
        };

        public static bool IsFileExist(string filename)
        {
            return File.Exists(filename);
        }

        public static bool IsFileHasCorrectExtension(string filename, FileExtensions extension)
        {
            return Path.GetExtension(filename) == fileExtensions[extension];
        }

        public static void CreateFile(string filename)
        {
            FileStream fs = File.Create(filename);
            fs.Close();
        }

        public static void CreateDirectory(string directory)
        {
            Directory.CreateDirectory(directory);
        }

        public static string CombinePath(string path1, string path2)
        {
            return Path.Combine(path1, path2);
        }
    }
}
