using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace MapTool
{
    enum WallType
    {
        None,
        Solid,
        OpenDoor,
        ClosedDoor
    }

    class Wall
    {
        WallType _Type = WallType.None;
        Color _DoorColor = Color.Fuchsia;

        public WallType Type
        {
            get { return _Type; }
            set { _Type = value; }
        }
        public Color DoorColor
        {
            get { return _DoorColor; }
            set { _DoorColor = value; }
        }

        public Wall Split()
        {
            Wall newWall = new Wall();
            newWall.Type = Type;
            newWall.DoorColor = DoorColor;
            return newWall;
        }

        public void Merge(Wall otherWall)
        {
            // Only merge the color from closed doors.
            // If both walls have closed doors, arbitrarily keep my own color.
            if ((Type != WallType.ClosedDoor) && (otherWall.Type == WallType.ClosedDoor))
                DoorColor = otherWall.DoorColor;

            // WallTypes are ordered by strength of wall, and generally we want
            // to use whichever wall is stronger when merging two walls together.
            Type = CompareHelper<WallType>.Max(Type, otherWall.Type);
        }
    }
}
