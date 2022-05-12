using NUnit.Framework;
using ComGeom;
using ComGeom.Meshes;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace ComGeomTest
{
    public class GetIntersectionPointOfSegmentsTests
    {
        private static readonly double epsilon = 1E-7;
        private static readonly double sqrEpsilon = epsilon * epsilon;

        [Test]
        public void SegmentsBelongToDifferentPlanes()
        {
            Vector3D start1 = Vector3D.Zero;
            Vector3D line1 = Vector3D.UnitX;

            Vector3D start2 = Vector3D.UnitZ;
            Vector3D line2 = new Vector3D(1, 1, 0);

            Vector3D? result = ComGeomAlgorithms.GetIntersectionPointOfSegments(start1, line1, start2, line2, epsilon, out bool multipleIntersection);

            Assert.IsTrue(result == null && !multipleIntersection);
        }

        [Test]
        public void ParallelSegments()
        {
            Vector3D start1 = Vector3D.Zero;
            Vector3D line1 = Vector3D.UnitX;

            Vector3D start2 = Vector3D.UnitY;
            Vector3D line2 = line1;

            Vector3D? result = ComGeomAlgorithms.GetIntersectionPointOfSegments(start1, line1, start2, line2, epsilon, out bool multipleIntersection);
            Assert.IsTrue(result == null && !multipleIntersection);
        }

        [Test]
        public void SegmentsBelongToOneLineAndDontIntersect()
        {
            Vector3D start1 = Vector3D.Zero;
            Vector3D line1 = Vector3D.UnitX;

            Vector3D start2 = start1 + 2 * line1;
            Vector3D line2 = line1;

            Vector3D? result = ComGeomAlgorithms.GetIntersectionPointOfSegments(start1, line1, start2, line2, epsilon, out bool multipleIntersection);
            Assert.IsTrue(result == null && !multipleIntersection);
        }

        [Test]
        public void SegmentsDontIntersect()
        {
            Vector3D start1 = Vector3D.Zero;
            Vector3D line1 = Vector3D.UnitX;

            Vector3D start2 = Vector3D.UnitY;
            Vector3D line2 = Vector3D.UnitY;

            Vector3D? result = ComGeomAlgorithms.GetIntersectionPointOfSegments(start1, line1, start2, line2, epsilon, out bool multipleIntersection);
            Assert.IsTrue(result == null && !multipleIntersection);
        }

        [Test]
        public void SegmentsAreEqual()
        {
            Vector3D start1 = Vector3D.Zero;
            Vector3D line1 = Vector3D.UnitX;

            Vector3D? result = ComGeomAlgorithms.GetIntersectionPointOfSegments(start1, line1, start1, line1, epsilon, out bool multipleIntersection);
            Assert.IsTrue(result == null && multipleIntersection);
        }

        [Test]
        public void SegmentsHaveMultipleIntersection()
        {
            Vector3D start1 = Vector3D.Zero;
            Vector3D line1 = 2 * Vector3D.UnitX;

            Vector3D start2 = Vector3D.UnitX;
            Vector3D line2 = line1;

            Vector3D? result = ComGeomAlgorithms.GetIntersectionPointOfSegments(start1, line1, start2, line2, epsilon, out bool multipleIntersection);
            Assert.IsTrue(result == null && multipleIntersection);
        }

        [Test]
        public void SegmentsBelongToOneLineAndFirstSegmentStartEqualsToSecondSegmentEnd()
        {
            Vector3D start1 = Vector3D.Zero;
            Vector3D line1 = Vector3D.UnitX;

            Vector3D start2 = -Vector3D.UnitX;
            Vector3D line2 = line1;

            Vector3D? result = ComGeomAlgorithms.GetIntersectionPointOfSegments(start1, line1, start2, line2, epsilon, out bool multipleIntersection);
            Assert.IsTrue(result.HasValue && result.Value.SqrDistance(start1) < sqrEpsilon && !multipleIntersection);
        }

        [Test]
        public void SegmentsBelongToOneLineAndFirstSegmentEndEqualsToSecondSegmentStart()
        {
            Vector3D start1 = Vector3D.Zero;
            Vector3D line1 = Vector3D.UnitX;

            Vector3D start2 = Vector3D.UnitX;
            Vector3D line2 = line1;

            Vector3D? result = ComGeomAlgorithms.GetIntersectionPointOfSegments(start1, line1, start2, line2, epsilon, out bool multipleIntersection);
            Assert.IsTrue(result.HasValue && result.Value.SqrDistance(start2) < sqrEpsilon && !multipleIntersection);
        }

        [Test]
        public void SegmentsIntersect()
        {
            Vector3D start1 = Vector3D.Zero;
            Vector3D line1 = Vector3D.UnitX;

            Vector3D start2 = new Vector3D(0.5, 0.5, 0);
            Vector3D line2 = -Vector3D.UnitY;

            Vector3D theory = 0.5 * Vector3D.UnitX;

            Vector3D? result = ComGeomAlgorithms.GetIntersectionPointOfSegments(start1, line1, start2, line2, epsilon, out bool multipleIntersection);
            Assert.IsTrue(result.HasValue && result.Value.SqrDistance(theory) < sqrEpsilon && !multipleIntersection);
        }
    }
}