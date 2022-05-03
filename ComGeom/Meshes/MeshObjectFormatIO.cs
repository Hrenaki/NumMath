using System;
using System.Collections.Generic;
using System.Text;

namespace ComGeom.Meshes
{
    internal class MeshObjectFormatIO : IMeshIO
    {
        private static string VertexPrefix = "v";
        private static string ElementPrefix = "e";
        private static string boundaryMaterialPrefix = "bm";
        private static string volumeMaterialPrefix = "vm";

        public IMesh3D Read(string filename)
        {
            if(!FileSystem.IsFileExist(filename))
                throw new FileNotFoundException(filename);

            if(!FileSystem.IsFileHasCorrectExtension(filename, FileExtensions.Object))
                throw new ArgumentException("Invalid file format" , nameof(filename));

            List<Vector3D> vertices = new List<Vector3D>();
            List<Element> elements = new List<Element>();
        }

        public void Write(string filename)
        {
            throw new NotImplementedException();
        }
    }
}
