using System;
using System.Collections.Generic;
using System.Text;

namespace ComGeom.Meshes
{
    public static class MeshFactory
    {
        public static IMesh3D Create3DMesh(IEnumerable<Vector3D> vertices, IEnumerable<Element> elements, IDictionary<int, string> boundaryMaterialNames, IDictionary<int, string> volumeMaterialNames)
        {
            return new Mesh3D(vertices, elements, boundaryMaterialNames, volumeMaterialNames);
        }
    }
}
