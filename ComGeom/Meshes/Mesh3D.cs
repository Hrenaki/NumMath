using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ComGeom.Meshes
{
    internal class Mesh3D : IMesh3D
    {
        private List<Vector3D> vertices;
        public IReadOnlyList<Vector3D> Vertices => vertices.AsReadOnly();

        private List<Element> elements;
        public IReadOnlyList<Element> Elements => elements.AsReadOnly();

        private IDictionary<int, string> boundaryMaterialNames;
        public IReadOnlyDictionary<int, string> BoundaryMaterialNames => new ReadOnlyDictionary<int, string>(boundaryMaterialNames);

        private IDictionary<int, string> volumeMaterialNames;
        public IReadOnlyDictionary<int, string> VolumeMaterialNames => new ReadOnlyDictionary<int, string>(volumeMaterialNames);

        public Mesh3D(IEnumerable<Vector3D> vertices, IEnumerable<Element> elements, IDictionary<int, string> boundaryMaterialNames, IDictionary<int, string> volumeMaterialNames)
        {
            if(vertices == null)
                throw new ArgumentNullException(nameof(vertices));

            if(elements == null)
                throw new ArgumentNullException(nameof(elements));

            if(boundaryMaterialNames == null)
                throw new ArgumentNullException(nameof(boundaryMaterialNames));

            if(volumeMaterialNames == null)
                throw new ArgumentNullException(nameof(volumeMaterialNames));

            this.vertices = vertices.ToList();
            this.elements = elements.ToList();
            this.boundaryMaterialNames = boundaryMaterialNames;
            this.volumeMaterialNames = volumeMaterialNames;
        }
    }
}
