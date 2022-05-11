using System;
using System.Collections.Generic;
using System.Text;

namespace ComGeom
{
    internal static class Guard
    {
        public static void AgainstNull(object obj, string name)
        {
            if (obj == null)
                throw new ArgumentNullException(name);
        }
    }
}
