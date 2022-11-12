using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath.Vectors
{
   public class Vector3 : Vector
   {
      public double X { get => values[0]; set => values[0] = value; }
      public double Y { get => values[1]; set => values[1] = value; }
      public double Z { get => values[2]; set => values[2] = value; }

      public Vector3()
      {
         values = new double[3];
      }
   }
}
