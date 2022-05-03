using System;
using System.Collections.Generic;
using System.Text;

namespace ComGeom
{
    internal enum FileExtensions
    {
        Object
    }

    internal static class FileSystem
    {
        private static Dictionary<FileExtensions, string> fileExtensions = new Dictionary<FileExtensions, string>() { { FileExtensions.Object, ".obj" } };

        public static bool IsFileExist(string filename)
        {
            return File.Exists(filename);
        }

        public static bool IsFileHasCorrectExtension(string filename, FileExtensions extension)
        {
            return Path.GetExtension(filename) == fileExtensions[extension];
        }
    }
}
