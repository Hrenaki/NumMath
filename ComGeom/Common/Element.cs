using System;
using System.Collections.Generic;
using System.Text;

namespace ComGeom
{
    public enum ElementType
    {
        Triangle,
        Tetrahedron
    }

    public struct Element
    {
        public ElementType Type { get; private set; }
        public int MaterialNumber { get; set; }
        public int[] Indices { get; private set; }

        public Element(ElementType type, int materialNumber, int[] indices)
        {
            Type = type;
            Indices = indices;
            MaterialNumber = materialNumber;
        }

        public Element CopyWithNewIndices(int[] newIndices)
        {
            return new Element(Type, MaterialNumber, newIndices);
        }
    }
}
