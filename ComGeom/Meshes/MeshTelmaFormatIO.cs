using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

using ComGeom.Common;

namespace ComGeom.Meshes
{
    internal class MeshTelmaFormatIO : IMeshIO
    {
        private static string[] separators = new string[] { " ", "\t" };

        public IMesh3D Read(string filename)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            if (!FileSystem.IsFileExist(filename))
                throw new FileNotFoundException(filename);

            if (!FileSystem.IsFileHasCorrectExtension(filename, FileExtensions.Expmesh))
                throw new ArgumentException("Invalid file format", nameof(filename));

            List<Vector3D> vertices = new List<Vector3D>();
            List<Element> elements = new List<Element>();
            Dictionary<int, string> volumeMaterials = new Dictionary<int, string>();
            Dictionary<int, string> boundaryMaterials = new Dictionary<int, string>();

            string[] splittedLine;
            using (StreamReader sr = new StreamReader(filename))
            {
                // Skipping telma version line
                sr.ReadLine();

                int vertexCount = int.Parse(sr.ReadLine());
                for(int i = 0; i < vertexCount; i++)
                {
                    splittedLine = sr.ReadLine().Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    Vector3D vertex = new Vector3D(splittedLine.Select(v => double.Parse(v)).ToArray());
                    vertices.Add(vertex);
                }

                int elementCount = int.Parse(sr.ReadLine());
                for(int i = 0; i < elementCount; i++)
                {
                    splittedLine = sr.ReadLine().Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    ElementType type = Enum.Parse<ElementType>(splittedLine[0]);
                    Element element = new Element(type, int.Parse(splittedLine[3]), splittedLine.Skip(5).Take(ElementInfo.ElementIndexCount[type]).Select(v => int.Parse(v)).ToArray());
                    elements.Add(element);
                }

                int materialCount = int.Parse(sr.ReadLine());
                int materialIndex;
                for(int i = 0; i < materialCount; i++)
                {
                    splittedLine = sr.ReadLine().Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    materialIndex = int.Parse(splittedLine[0]);
                    if (volumeMaterials.ContainsKey(materialIndex))
                        throw new ArgumentException($"Volume material with index {materialIndex} already exists");

                    volumeMaterials.Add(materialIndex, string.Join(separators[0], splittedLine.Skip(1)));
                }

                materialCount = int.Parse(sr.ReadLine());
                for (int i = 0; i < materialCount; i++)
                {
                    splittedLine = sr.ReadLine().Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    materialIndex = int.Parse(splittedLine[0]);
                    if (boundaryMaterials.ContainsKey(materialIndex))
                        throw new ArgumentException($"Volume material with index {materialIndex} already exists");

                    boundaryMaterials.Add(materialIndex, string.Join(separators[0], splittedLine.Skip(1)));
                }
            }

            return MeshFactory.Create3DMesh(vertices, elements, boundaryMaterials, volumeMaterials);
        }

        public void Write(IMesh3D mesh, string filename)
        {
            throw new NotImplementedException();
        }
    }
}
