using System;
using System.Collections.Generic;
using System.Drawing;

namespace MapTool
{
    public delegate void BoundsChangedEventHandler(object sender, Rectangle oldBounds);

    [Serializable()]
    public class Map
    {
        // Fields
        RoomGrid _Grid = new RoomGrid();
        public RoomGrid Grid {
            get { return _Grid; }
            set { _Grid = value; }
        }
        //Rectangle _Bounds = new Rectangle(0, 0, 0, 0);

        // Generally this won't be set directly; go through Bounds instead.
        // But if you want to just change the offset and move everything around,
        // instead of deleting things that are outside the new area, this is a way to do it.
        Point _Offset = new Point(0, 0);
        public Point Offset {
            get { return _Offset; }
            set {
                if (_Offset != value)
                {
                    Rectangle oldBounds = Bounds;
                    _Offset = value;
                    OnBoundsChanged(oldBounds);
                }
            }
        }

        // Private utility methods
        private Point GetGridPoint(Point boundsPoint)
        { return Point.Subtract(boundsPoint, (Size)Bounds.Location); }

        // Expand the bounds to make room for rect, if needed.
        private void ExtendBoundsIfNeeded(Rectangle rect)
        {
            if (!Bounds.Contains(rect))
                Bounds = Rectangle.Union(Bounds, rect);
        }
        private void ExtendBoundsIfNeeded(Point pt)
        {
            if (!Bounds.Contains(pt))
                Bounds = Rectangle.Union(Bounds, new Rectangle(pt, new Size(1, 1)));
        }

        // Pass-through methods into the grid.
        public Room GetRoom(Point roomPt)
        {
            // The grid will crash if you ask it for a room out of range.
            // By contrast, Maps just have implicit null rooms for everything outside the current bounds.
            if (Bounds.Contains(roomPt))
                return _Grid.GetRoom(GetGridPoint(roomPt));
            else
                return null;
        }
        public Room CreateRoom(Point roomPt)
        {
            ExtendBoundsIfNeeded(roomPt);
            return _Grid.CreateRoom(GetGridPoint(roomPt));
        }
        public Room GetOrCreateRoom(Point roomPt)
        {
            Room result = GetRoom(roomPt);
            return result ?? CreateRoom(roomPt);
        }
        public Room GetAdjacentRoom(Point roomPt, int direction)
        {
            return _Grid.GetAdjacentRoom(GetGridPoint(roomPt), direction);
        }
        public IEnumerator<IEnumerable<Room>> GetGridEnumerator()
        { return _Grid.GetEnumerator(); }

        // Bounds-related properties
        [field:NonSerializedAttribute()]
        public event BoundsChangedEventHandler BoundsChanged;
        protected virtual void OnBoundsChanged(Rectangle oldBounds)
        {
            if (BoundsChanged != null)
                BoundsChanged(this, oldBounds);
        }
        public Rectangle Bounds
        {
            set
            {
                // Changing the bounds also moves around the contents of the grid,
                // adding new null rooms as needed and discarding rooms that are
                // outside the new rect.
                if (Bounds != value)
                {
                    Rectangle oldBounds = Bounds;

                    // Adjust the width first, so any new rows added get the right number of rooms to start.
                    if (oldBounds.Left != value.Left)
                    {
                        int delta = Math.Abs(oldBounds.Left - value.Left);
                        if (oldBounds.Left > value.Left)
                            _Grid.InsertColumns(0, delta);
                        else
                            _Grid.RemoveColumns(0, delta);
                    }
                    if (oldBounds.Right != value.Right)
                    {
                        int delta = Math.Abs(oldBounds.Right - value.Right);
                        if (oldBounds.Right < value.Right)
                            _Grid.AddColumns(delta);
                        else
                            _Grid.RemoveColumns(_Grid.Width - delta, delta);
                    }

                    // Now adjust the height.
                    if (oldBounds.Top != value.Top)
                    {
                        int delta = Math.Abs(oldBounds.Top - value.Top);
                        if (oldBounds.Top > value.Top)
                            _Grid.InsertRows(0, delta);
                        else
                            _Grid.RemoveRows(0, delta);
                    }
                    if (oldBounds.Bottom != value.Bottom)
                    {
                        int delta = Math.Abs(oldBounds.Bottom - value.Bottom);
                        if (oldBounds.Bottom < value.Bottom)
                            _Grid.AddRows(delta);
                        else
                            _Grid.RemoveRows(_Grid.Height - delta, delta);
                    }

                    // The rooms have been updated, so now we can set the field and be done.
                    Offset = value.Location;
                    OnBoundsChanged(oldBounds);
                }
            }
            get { return new Rectangle(_Offset, _Grid.Size);  }
        }

        public enum DrawWallMode
        {
            Overwrite,
            Extend
        }

        public void DrawRectangle(Rectangle rect, Color color, DrawWallMode drawMode)
        {

            if (rect.IsEmpty)
                return;

            ExtendBoundsIfNeeded(rect);

            Point roomPt = new Point();
            Boolean[] isEdge = new Boolean[Direction.Count];

            for (roomPt.Y = rect.Top; roomPt.Y < rect.Bottom; roomPt.Y++)
            {
                isEdge[Direction.Top] = (roomPt.Y == rect.Top);
                isEdge[Direction.Bottom] = (roomPt.Y == rect.Bottom - 1);

                for (roomPt.X = rect.Left; roomPt.X < rect.Right; roomPt.X++)
                {
                    isEdge[Direction.Left] = (roomPt.X == rect.Left);
                    isEdge[Direction.Right] = (roomPt.X == rect.Right - 1);

                    Room newRoom = GetOrCreateRoom(roomPt);
                    newRoom.Color = color;

                    // Set up the walls.
                    for (int i = Direction.First; i < Direction.Count; i++)
                    {
                        if (isEdge[i])
                        {
                            Boolean overwriteWall;

                            switch (newRoom.Walls[i].Type)
                            {
                                case WallType.Undefined:
                                    overwriteWall = true;
                                    break;
                                case WallType.Open:
                                    // If we are in extend mode then only fill in open walls along the edge
                                    // if the adjacent room is a different color (or null).
                                    if (drawMode == DrawWallMode.Extend)
                                    {
                                        Room adjRoom = GetAdjacentRoom(roomPt, i);
                                        overwriteWall = (adjRoom == null) || (adjRoom.Color != color);
                                    }
                                    else
                                        overwriteWall = true;

                                    break;
                                default:
                                    // leave doors alone, and solid walls can be ignored because we're drawing as solid anyway.
                                    overwriteWall = false;
                                    break;
                            }

                            if (overwriteWall)
                                newRoom.Walls[i].Type = WallType.Solid;
                        }
                        else
                            // Interior walls are all set to none.
                            newRoom.Walls[i].Type = WallType.Open;
                    }
                }
            }
        }
    }
}
