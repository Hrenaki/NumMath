using System;
using System.Collections.Generic;
using System.Text;

namespace ComGeom
{
    public interface IMesh3D
    {
        IReadOnlyList<Vector3D> Vertices { get; }
        IReadOnlyList<Element> Elements { get; }
        IReadOnlyDictionary<int, string> BoundaryMaterialNames { get; }
        IReadOnlyDictionary<int, string> VolumeMaterialNames { get; }

        IMesh3D MergeWith(IMesh3D other, int boundaryMaterialIndex, int otherBoundaryMaterialIndex);
        bool Equals(IMesh3D other, double epsilon);
    }
}
