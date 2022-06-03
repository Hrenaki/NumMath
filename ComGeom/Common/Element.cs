using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace ComGeom
{
    public enum ElementType
    {
        Triangle,
        Tetrahedron
    }

    internal static class ElementInfo
    {
        public static IReadOnlyDictionary<ElementType, int> ElementIndexCount = new ReadOnlyDictionary<ElementType, int>(new Dictionary<ElementType, int>()
        { 
            {ElementType.Triangle, 3 }, 
            {ElementType.Tetrahedron, 4} 
        });
    }

    public class Element
    {
        public ElementType Type { get; private set; }
        public int MaterialNumber { get; set; }
        public int[] Indices { get; private set; }

        public Element(ElementType type, int materialNumber, int[] indices)
        {
            Guard.AgainstNull(indices, nameof(indices));

            Type = type;
            Indices = indices;
            MaterialNumber = materialNumber;

            ValidateIndices();
        }

        private void ValidateIndices()
        {
            if (Indices == null)
                return;

            if(Indices.Any(index => index < 0))
                throw new ArgumentException("Index can't be negative", nameof(Indices));
        }

        public Element Copy()
        {
            int[] indices = new int[Indices.Length];
            Array.Copy(Indices, indices, Indices.Length);
            return CopyWithNewIndices(indices);
        }

        public Element CopyWithNewIndices(int[] newIndices)
        {
            return new Element(Type, MaterialNumber, newIndices);
        }

        public bool Equals(Element other)
        {
            return ReferenceEquals(this, other) || 
                   Type == other.Type && MaterialNumber == other.MaterialNumber && !Indices.Except(other.Indices).Any() && !other.Indices.Except(Indices).Any();
        }

        public override string ToString()
        {
            return string.Join(" ", Type.ToString(), MaterialNumber.ToString(), string.Join(" ", Indices));
        }
    }
}
