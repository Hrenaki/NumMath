using System;
using System.Collections.Generic;
using System.Text;

namespace ComGeom.Meshes
{
    public static class MeshIOFactory
    {
        private static MeshObjectFormatIO meshObjectFormatIO = new MeshObjectFormatIO();

        public static IMeshIO CreateObjectFormatIO()
        {
            return meshObjectFormatIO;
        }
    }
}
