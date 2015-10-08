using System;
using System.Drawing;

namespace MapTool
{
    static class Direction
    {
        public const int Left = 0;
        public const int Top = 1;
        public const int Right = 2;
        public const int Bottom = 3;

        public const int First = Left;
        public const int Last = Bottom;
        public const int Count = Last + 1;

        public static int Invert(int value)
        {
            switch (value)
            {
                case Left: return Right;
                case Top:     return Bottom;
                case Right:   return Left;
                case Bottom: return Top;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }

    [Serializable()]
    public class Room
    {
        // Properties
        private StaticArrayProperty<Wall> _Walls = new StaticArrayProperty<Wall>(Direction.Count);
        public StaticArrayProperty<Wall> Walls {
            get { return _Walls; }
            set { _Walls = value; }
        }

        private Color _Color;
        public Color Color {
            get { return _Color; }
            set { _Color = value; }
        }

        private Boolean _MergeAsNull;
        public Boolean MergeAsNull {
            get { return _MergeAsNull; }
            set { _MergeAsNull = value; }
        }


        //public void SplitWall(int side)
        //{
        //    Walls[side] = Walls[side].Clone();
        //}

        public void MergeWalls(int side, Room adjacentRoom)
        {
            int otherSide = Direction.Invert(side);
            // Merge my wall with the other room's wall.
            // (this doesn't actually combine the objects, just sets them to be equal using certain merge-type rules.
            Walls[side].Merge(adjacentRoom.Walls[otherSide]);

            //// Replace the other room's wall with my (merged) wall.
            //adjacentRoom.Walls[otherSide] = Walls[side];
        }

        public void ConnectWall(int side, Room adjacentRoom)
        {
            Walls[side] = (adjacentRoom != null) ? adjacentRoom.Walls[Direction.Invert(side)].Clone() : new Wall();
        }
    }
}
