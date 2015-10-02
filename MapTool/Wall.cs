using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace MapTool
{
    [FlagsAttribute]
    enum WallType
    {
        Undefined = 0,
        Open = 1,
        Solid = 2,
        OpenDoor = 4,
        ClosedDoor = 8,
        AnyDoor = OpenDoor | ClosedDoor
    }

    class Wall
    {
        WallType _Type = WallType.Undefined;
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

        public Wall Clone()
        {
            Wall newWall = new Wall();
            newWall.Type = Type;
            newWall.DoorColor = DoorColor;
            return newWall;
        }

        public void Merge(Wall otherWall)
        {
            /*  The rules here are:
                1. replace undefined with whatever the other wall is
                2. open walls get replaced by solid or doors
                3. solid gets replaced by any door
                4. if both are doors, leave them alone

                This boils down to just using the stronger wall, unless they're both doors,
                in which case do nothing.
             */
            if ( (Type < WallType.ClosedDoor) & (otherWall.Type < WallType.ClosedDoor) )
            {
                // Merge the color across from any closed doors.
                if (Type == WallType.ClosedDoor)
                    otherWall.DoorColor = DoorColor;
                else if (otherWall.Type == WallType.ClosedDoor)
                    DoorColor = otherWall.DoorColor;

                // Merge the wall type.
                Type = CompareHelper<WallType>.Max(Type, otherWall.Type);
                otherWall.Type = Type;
            }
        }
    }
}
