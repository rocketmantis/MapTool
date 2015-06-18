using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace CoordinateHelper
{
    public static class RectHelper
    {
        public static Rectangle FromPoints(Point topLeft, Point bottomRight)
        {
            return Normalize(Rectangle.FromLTRB(
                topLeft.X, topLeft.Y,
                bottomRight.X, bottomRight.Y));
        }
        public static Rectangle Normalize(Rectangle rect)
        {
            if (rect.Height < 0)
            {
                rect.Y = rect.Bottom;
                rect.Height = -rect.Height;
            }
            if (rect.Width < 0)
            {
                rect.X = rect.Right;
                rect.Width = -rect.Width;
            }
            return rect;
        }
    }
}
