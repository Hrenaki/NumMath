using System;
using System.Collections.Generic;
using System.Text;

namespace ComGeom.Common
{
    public interface IMeshIO
    {
        public IMesh3D Read(string filename);
        public void Write(IMesh3D mesh, string filename);
    }
}
