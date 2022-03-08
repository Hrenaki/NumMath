using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath.Splines
{
    public interface ISpline1D
    {
        double GetValue(double x);
    }
}
