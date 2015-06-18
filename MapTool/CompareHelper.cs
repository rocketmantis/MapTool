using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapTool
{
    public static class CompareHelper<T> where T: IComparable
    {
        public static T Min(T a, T b)
        {
            return a.CompareTo(b) <= 0 ? a : b;
        }
        public static T Max(T a, T b)
        {
            return a.CompareTo(b) >= 0 ? a : b;
        }
    }
}
