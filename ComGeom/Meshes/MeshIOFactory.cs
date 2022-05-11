using System;
using System.Collections.Generic;
using System.Text;

namespace ComGeom.Meshes
{
    public static class MeshIOFactory
    {
        private static MeshObjectFormatIO meshObjectFormatIO = new MeshObjectFormatIO();
        private static MeshTelmaFormatIO meshTelmaFormatIO = new MeshTelmaFormatIO();

        public static IMeshIO CreateObjectFormatIO()
        {
            return meshObjectFormatIO;
        }

        public static IMeshIO CreateTelmaFormatIO()
        {
            return meshTelmaFormatIO;
        }
    }
}