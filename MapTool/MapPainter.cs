using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using CoordinateHelper;

namespace MapTool
{
    class MapPainter
    {
        private Size RoomSize;
        private const int WallWidth = 4;
        private const int HalfWallWidth = WallWidth / 2;
        private const int DoorBorder = 1;

        private Point Origin;
        private Color WallColor;
        private List<Point> DirtyRooms;

        // I'll worry about thread safety later.
        private LockHandler UpdateLock;

        public MapPainter()
        {
            RoomSize = new Size(32, 32);
            Origin = new Point(80, 80);
            WallColor = Color.White;
            DirtyRooms = new List<Point>();
            UpdateLock = new LockHandler();
            UpdateLock.LockChanged += UpdateChanged;
        }

        private void UpdateChanged(object sender, bool locked)
        {
            if (!locked)
            {
                // todo: invalidate/repaint dirty rooms
            }
        }

        public void BeginUpdate()
        {
            UpdateLock.Lock();
        }
        public void EndUpdate()
        {
            UpdateLock.Unlock();
        }

        public void DoUpdate(bool UpdateStarting)
        { }

        public Rectangle GetCanvasRectForRoom(Point startPt, Point? endPt = null)
        {
            Point useEndPt = endPt ?? startPt;
            Rectangle roomRect = RectHelper.FromPoints(startPt, useEndPt);
            // Inflate the bottom-right of the rect so both points are included in its Map.
            roomRect.Size += new Size(1, 1);

            return GetCanvasRectForRoom(roomRect);
        }

        public Rectangle GetCanvasRectForRoom(Rectangle roomRect, Boolean includeBorders = false)
        {
            roomRect = RectHelper.Normalize(roomRect);

            Rectangle canvasRect = new Rectangle(
                Origin.Y + roomRect.Left * RoomSize.Width,
                Origin.Y + roomRect.Top * RoomSize.Height,
                roomRect.Width * RoomSize.Width,
                roomRect.Height * RoomSize.Height);
            if (includeBorders)
                canvasRect.Inflate(HalfWallWidth, HalfWallWidth);

            return canvasRect;
        }
        public Point GetRoomPtForCanvasPoint(Point canvasPoint)
        {
            // This is just the reverse of GetCanvasRectForRoom, really.
            Point boundsPoint = Point.Subtract(canvasPoint, (Size)Origin);
            return new Point(
                (int)Math.Floor((double)boundsPoint.X / RoomSize.Width),
                (int)Math.Floor((double)boundsPoint.Y / RoomSize.Height));
        }

        public void Paint(Map map, Graphics graphics, Rectangle clipRect)
        {
            Size MapSize = map.Bounds.Size;
            MapSize.Width *= RoomSize.Width;
            MapSize.Height *= RoomSize.Height;

            Point MapLocation = map.Bounds.Location;
            MapLocation.X *= RoomSize.Width;
            MapLocation.Y *= RoomSize.Height;
            MapLocation.Offset(Origin);

            Rectangle MapRect = new Rectangle(MapLocation, MapSize);
            MapRect.Inflate(HalfWallWidth, HalfWallWidth);

            if (!MapRect.IntersectsWith(clipRect))
                return;

            Rectangle paintRect = Rectangle.Intersect(MapRect, clipRect);

            // Fill in the default background.
            graphics.FillRectangle(new SolidBrush(Color.DimGray), paintRect);

            // Get the list of rooms that need to be repainted, given the cliprect.
            List<Point> dirtyRooms = GetDirtyRooms(map, clipRect);

            // first pass across rooms: fill in the background
            PaintRoomBGs(map, graphics, dirtyRooms);

            // second pass across rooms: draw the walls and doors
            PaintRoomWalls(map, graphics, dirtyRooms);
        }

        private void PaintRoomBGs(Map map, Graphics graphics, List<Point> dirtyRooms)
        {
            foreach (Point roomPt in dirtyRooms)
            {
                Room curRoom = map.GetRoom(roomPt);
                Rectangle roomRect = GetCanvasRectForRoom(roomPt);
                // Fill in the room area.
                graphics.FillRectangle(new SolidBrush(curRoom.Color), roomRect);
            }
        }

        private void PaintRoomWalls(Map map, Graphics graphics, List<Point> dirtyRooms)
        {
            Pen wallPen = new Pen(WallColor, WallWidth);
            Pen errorPen = null;
            foreach (Point roomPt in dirtyRooms)
            {
                Room curRoom = map.GetRoom(roomPt);
                Rectangle roomRect = GetCanvasRectForRoom(roomPt);

                // Now figure out where the walls will be drawn.
                // These are picked such that for direction i, draw a line from point i to i+1 in this array.
                Point[] corners = {
                                    new Point(roomRect.Left, roomRect.Bottom),
                                    roomRect.Location,
                                    new Point(roomRect.Right, roomRect.Top),
                                    new Point(roomRect.Right, roomRect.Bottom),
                                    new Point(roomRect.Left, roomRect.Bottom) };

                // Loop through and draw walls as necessary.
                for (int i = Direction.First; i < Direction.Count; i++)
                {
                    Wall curWall = curRoom.Walls[i];

                    switch (curWall.Type)
                    {
                        case WallType.Undefined:
                            errorPen = errorPen ?? new Pen(new HatchBrush(HatchStyle.BackwardDiagonal, Color.Red), WallWidth);
                            graphics.DrawLine(errorPen, corners[i], corners[i + 1]);
                            break;
                        // case WallType.Open: do nothing
                        case WallType.Solid:
                            graphics.DrawLine(wallPen, corners[i], corners[i + 1]);
                            break;
                        case WallType.OpenDoor:
                        case WallType.ClosedDoor:
                            // Doors run from 1/4 to 3/4 of the wall.
                            const int DoorScale = 4;

                            Point doorStart = new Point(
                                    (corners[i].X * (DoorScale - 1) + corners[i + 1].X) / DoorScale,
                                    (corners[i].Y * (DoorScale - 1) + corners[i + 1].Y) / DoorScale);
                            Point doorEnd = new Point(
                                    (corners[i].X + corners[i + 1].X * (DoorScale - 1)) / DoorScale,
                                    (corners[i].Y + corners[i + 1].Y * (DoorScale - 1)) / DoorScale);

                            graphics.DrawLine(wallPen, corners[i], doorStart);
                            graphics.DrawLine(wallPen, doorEnd, corners[i + 1]);

                            // If the door is closed then paint in a door using the door color,
                            // otherwise leave it unpainted.
                            // todo: fix this to work when the adjacent room's wall doesn't match.
                            // Room adjacentRoom = map.GetAdjacentRoom(roomPt, i);
                            if (curWall.Type == WallType.ClosedDoor)
                            {
                                Rectangle doorRect = new Rectangle(
                                    Math.Min(doorStart.X, doorEnd.X),
                                    Math.Min(doorStart.Y, doorEnd.Y),
                                    Math.Abs(doorEnd.X - doorStart.X),
                                    Math.Abs(doorEnd.Y - doorStart.Y));
                                // One or the other of the dimensions will be 0
                                if (doorRect.Width == 0)
                                    doorRect.Inflate(WallWidth, 1);
                                else
                                    doorRect.Inflate(1, WallWidth);

                                Brush doorBrush = new SolidBrush(curWall.DoorColor);
                                Pen doorPen = new Pen(WallColor, DoorBorder);
                                graphics.FillRectangle(doorBrush, doorRect);
                                graphics.DrawRectangle(doorPen, doorRect);
                            }
                            break;
                        default:
                            break;
                    } // switch (curWall.Type)
                } // for i in directions do
            }
        }

        private List<Point> GetDirtyRooms(Map map, Rectangle clipRect)
        {
            // graphics.DrawRectangle(wallPen, MapRect);

            // First loop: figure out which rooms need to be repainted
            List<Point> dirtyRooms = new List<Point>();

            {
                // declare this inside a local scope since it's effectively a loop variable.
                Point roomPt = new Point();
                for (roomPt.Y = map.Bounds.Top; roomPt.Y < map.Bounds.Bottom; roomPt.Y++)
                    for (roomPt.X = map.Bounds.Left; roomPt.X < map.Bounds.Right; roomPt.X++)
                    {
                        Room curRoom = map.GetRoom(roomPt);
                        if (curRoom != null)
                        {
                            Rectangle roomRect = GetCanvasRectForRoom(roomPt);

                            if (Rectangle.Inflate(roomRect, HalfWallWidth, HalfWallWidth).IntersectsWith(clipRect))
                                dirtyRooms.Add(roomPt);
                        } // if room isn't null then
                    } // for x, y in bounds do
            }

            return dirtyRooms;
        }
    }
}
