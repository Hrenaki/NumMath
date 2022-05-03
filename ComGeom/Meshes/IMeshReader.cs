using System;
using System.Collections.Generic;
using System.Text;

namespace ComGeom.Meshes
{
    public interface IMeshIO
    {
        public IMesh3D Read(string filename);
        public void Write(string filename);
    }
}
