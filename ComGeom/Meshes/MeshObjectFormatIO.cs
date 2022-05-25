using System;
using System.Collections.Generic;
using System.Text;

using ComGeom.Common;

namespace ComGeom.Meshes
{
    internal class MeshObjectFormatIO : IMeshIO
    {
        private const string VertexPrefix = "v";
        private const string ElementPrefix = "e";
        private const string BoundaryMaterialPrefix = "bm";
        private const string VolumeMaterialPrefix = "vm";
        private static string[] separators = new[] { " " };

        public IMesh3D Read(string filename)
        {
            if(!FileSystem.IsFileExist(filename))
                throw new FileNotFoundException(filename);

            if(!FileSystem.IsFileHasCorrectExtension(filename, FileExtensions.Object))
                throw new ArgumentException("Invalid file format" , nameof(filename));

            List<Vector3D> vertices = new List<Vector3D>();
            List<Element> elements = new List<Element>();
            Dictionary<int, string> volumeMaterials = new Dictionary<int, string>();
            Dictionary<int, string> boundaryMaterials = new Dictionary<int, string>();

            using(StreamReader sr = new StreamReader(filename))
            {
                int lineIndex = 0;
                int materialIndex;

                while(!sr.EndOfStream)
                {
                    lineIndex++;
                    string[] splittedLine = sr.ReadLine().Split(separators, StringSplitOptions.RemoveEmptyEntries);

                    if(splittedLine.Length < 1)
                        continue;

                    switch(splittedLine[0])
                    {
                        case VertexPrefix:
                            if (splittedLine.Length < 1 + Vector3D.Dimension)
                                throw new ArgumentException($"Can't parse vertex, line {lineIndex}");
                            Vector3D vertex = new Vector3D(splittedLine.Skip(1).Take(3).Select(coord => double.Parse(coord)).ToArray());
                            vertices.Add(vertex);
                            break;
                        case ElementPrefix:
                            if (splittedLine.Length < 2)
                                throw new ArgumentException($"Can't parse element, line {lineIndex}");

                            try
                            {
                                ElementType type = Enum.Parse<ElementType>(splittedLine[1]);
                                if (splittedLine.Length < 2 + ElementInfo.ElementIndexCount[type])
                                    throw new ArgumentException($"Can't parse element, line {lineIndex}");

                                Element element = new Element(type, int.Parse(splittedLine[2]), splittedLine.Skip(2).Take(ElementInfo.ElementIndexCount[type]).Select(v => int.Parse(v)).ToArray());
                                elements.Add(element);
                            }
                            catch(ArgumentException)
                            {
                                throw new ArgumentException($"Invalid element type, line {lineIndex}");
                            }

                            break;
                        case BoundaryMaterialPrefix:
                            if (splittedLine.Length < 3)
                                throw new ArgumentException($"Can't parse boundary material, line {lineIndex}");

                            materialIndex = int.Parse(splittedLine[1]);
                            if (boundaryMaterials.ContainsKey(materialIndex))
                                throw new ArgumentException($"Material with index {materialIndex} already exists, line {lineIndex}");

                            boundaryMaterials.Add(materialIndex, splittedLine[2]);
                            break;
                        case VolumeMaterialPrefix:
                            if (splittedLine.Length < 3)
                                throw new ArgumentException($"Can't parse volume material, line {lineIndex}");

                            materialIndex = int.Parse(splittedLine[1]);
                            if (boundaryMaterials.ContainsKey(materialIndex))
                                throw new ArgumentException($"Material with index {materialIndex} already exists, line {lineIndex}");

                            boundaryMaterials.Add(materialIndex, splittedLine[2]);
                            break;
                        default: continue;
                    }
                    
                }

                int vertexCount = vertices.Count;
                foreach(var element in elements)
                {
                    if (!boundaryMaterials.ContainsKey(element.MaterialNumber) && !volumeMaterials.ContainsKey(element.MaterialNumber))
                        throw new ArgumentException($"Element [{element}] has invalid material index");

                    if (element.Indices.Any(index => index >= vertexCount))
                        throw new ArgumentException($"Element [{element}] has invalid vertex index");
                }

                return MeshFactory.Create3DMesh(vertices, elements, boundaryMaterials, volumeMaterials);
            }
        }

        public void Write(IMesh3D mesh, string filename)
        {
            Guard.AgainstNull(mesh, nameof(mesh));

            if(!FileSystem.IsFileExist(filename))
                throw new FileNotFoundException(filename);

            using(StreamWriter sw = new StreamWriter(filename, false))
            {
                foreach (var vertex in mesh.Vertices)
                {
                    sw.WriteLine(string.Join(separators[0], VertexPrefix, vertex.ToString()));
                }

                foreach(var element in mesh.Elements)
                {
                    sw.WriteLine(string.Join(separators[0], ElementPrefix, element.ToString()));
                }

                foreach(var materialPair in mesh.BoundaryMaterialNames)
                {
                    sw.WriteLine(string.Join(separators[0], BoundaryMaterialPrefix, materialPair.Key, materialPair.Value));
                }

                foreach (var materialPair in mesh.VolumeMaterialNames)
                {
                    sw.WriteLine(string.Join(separators[0], BoundaryMaterialPrefix, materialPair.Key, materialPair.Value));
                }
            }
        }
    }
}