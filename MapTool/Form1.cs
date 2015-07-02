using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoordinateHelper;

namespace MapTool
{
    public partial class Form1 : Form
    {
        private Area _Map = new Area();

        private Size RoomSize = new Size(32, 32);
        private const int WallWidth = 4;
        private const int HalfWallWidth = WallWidth / 2;
        private const int DoorBorder = 1;

        private Point Origin = new Point(80, 80);
        private Color WallColor = Color.White;

        private Rectangle GetCanvasRectForRoom(Point startPt, Point? endPt = null)
        {
            Point useEndPt = endPt ?? startPt;
            Rectangle roomRect = RectHelper.FromPoints(startPt, useEndPt);
            // Inflate the bottom-right of the rect so both points are included in its area.
            roomRect.Size += new Size(1, 1);

            return GetCanvasRectForRoom(roomRect);
        }

        private Rectangle GetCanvasRectForRoom(Rectangle roomRect, Boolean includeBorders = false)
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
        private Point GetRoomPtForCanvasPoint(Point canvasPoint)
        {
            // This is just the reverse of GetCanvasRectForRoom, really.
            Point boundsPoint = Point.Subtract(canvasPoint, (Size)Origin);
            return new Point(
                (int)Math.Floor((double)boundsPoint.X / RoomSize.Width),
                (int)Math.Floor((double)boundsPoint.Y / RoomSize.Height));
        }

        private void MapBoundsChanged(object sender, Rectangle oldBounds)
        {
            // The areas that have changed are the union of the two bounds,
            // excluding the intersection. ie. the xor of the two rects.
            Region invalidRegion = new Region(GetCanvasRectForRoom(oldBounds, true));
            invalidRegion.Xor(GetCanvasRectForRoom(_Map.Bounds, true));
            Invalidate(invalidRegion);
        }

        public Form1()
        {
            InitializeComponent();

            _Map.BoundsChanged += MapBoundsChanged;

            // We shouldn't need to explicitly set the bounds anymore;
            // CreateRoom should extend the boundary as necessary.
            //_Map.Bounds = new Rectangle(-1, -1, 5, 5);

            // Make a room for the top-left and bottom-right corners
            Room newRoom = _Map.CreateRoom(new Point(-1, -1));
            newRoom.Color = Color.MediumBlue;
            newRoom.Walls[Direction.Top].Type = WallType.OpenDoor;
            newRoom.Walls[Direction.Left].Type = WallType.OpenDoor;

            newRoom = _Map.CreateRoom(new Point(3, 3));
            newRoom.Color = Color.MediumBlue;
            newRoom.Walls[Direction.Bottom].Type = WallType.OpenDoor;
            newRoom.Walls[Direction.Right].Type = WallType.OpenDoor;

            // Fill in a few rooms to start with.
            newRoom = _Map.CreateRoom(new Point(2, 1));
            newRoom.Color = Color.ForestGreen;
            newRoom.Walls[Direction.Top].Type = WallType.Solid;
            newRoom.Walls[Direction.Right].Type = WallType.Solid;
            newRoom.Walls[Direction.Bottom].Type = WallType.Solid;
            newRoom.Walls[Direction.Left].Type = WallType.ClosedDoor;
            newRoom.Walls[Direction.Left].DoorColor = Color.DeepSkyBlue;

            newRoom = _Map.CreateRoom(new Point(0, 2));
            newRoom.Color = Color.MediumBlue;
            newRoom.Walls[Direction.Top].Type = WallType.OpenDoor;
            newRoom.Walls[Direction.Left].Type = WallType.Solid;
            newRoom.Walls[Direction.Bottom].Type = WallType.Solid;
            // Let's overwrite the left wall and see what happens.
            newRoom.Walls[Direction.Right].Type = WallType.ClosedDoor;
            newRoom.Walls[Direction.Right].DoorColor = Color.OrangeRed;

            newRoom = _Map.CreateRoom(new Point(0, 1));
            newRoom.Color = Color.MediumBlue;
            newRoom.Walls[Direction.Top].Type = WallType.Solid;
            newRoom.Walls[Direction.Left].Type = WallType.Solid;
            // Bottom should be defined by the connection already
            // Right we'll leave open.

            newRoom = _Map.CreateRoom(new Point(1, 1));
            newRoom.Color = Color.MediumBlue;
            newRoom.Walls[Direction.Top].Type = WallType.Solid;
            newRoom.Walls[Direction.Bottom].Type = WallType.Solid;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Size areaSize = _Map.Bounds.Size;
            areaSize.Width *= RoomSize.Width;
            areaSize.Height *= RoomSize.Height;

            Point areaLocation = _Map.Bounds.Location;
            areaLocation.X *= RoomSize.Width;
            areaLocation.Y *= RoomSize.Height;
            areaLocation.Offset(Origin);

            Rectangle areaRect = new Rectangle(areaLocation, areaSize);
            areaRect.Inflate(HalfWallWidth, HalfWallWidth);

            if (!areaRect.IntersectsWith(e.ClipRectangle))
                return;

            Rectangle paintRect = Rectangle.Intersect(areaRect, e.ClipRectangle);

            e.Graphics.FillRectangle(new SolidBrush(Color.DimGray), paintRect);

            Pen wallPen = new Pen(WallColor, WallWidth);
            Pen errorPen = null;
            // e.Graphics.DrawRectangle(wallPen, areaRect);

            Point roomPt = new Point();

            for (roomPt.Y = _Map.Bounds.Top; roomPt.Y < _Map.Bounds.Bottom; roomPt.Y++)
                for (roomPt.X = _Map.Bounds.Left; roomPt.X < _Map.Bounds.Right; roomPt.X++)
                {
                    Room curRoom = _Map.GetRoom(roomPt);
                    if (curRoom != null)
                    {
                        Rectangle roomRect = GetCanvasRectForRoom(roomPt);

                        if (Rectangle.Inflate(roomRect, HalfWallWidth, HalfWallWidth).IntersectsWith(e.ClipRectangle))
                        {
                            // Fill in the room area.
                            e.Graphics.FillRectangle(new SolidBrush(curRoom.Color), roomRect);

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

                                // Optimize: skip drawing the bottom and right walls if there is another room on that side.
                                // This also means that walls will only be drawn after both rooms have been filled, since we
                                // iterate top->bottom, left->right, so one room's fill can't overwrite the wall/door area.
                                if (((i == Direction.Bottom) || (i == Direction.Right)) && (_Map.GetAdjacentRoom(roomPt, i) != null))
                                    continue;

                                switch (curWall.Type)
                                {
                                    case WallType.Undefined:
                                        errorPen = errorPen ?? new Pen(new HatchBrush(HatchStyle.BackwardDiagonal, Color.Red), WallWidth);
                                        e.Graphics.DrawLine(errorPen, corners[i], corners[i + 1]);
                                        break;
                                    // case WallType.Open: do nothing
                                    case WallType.Solid:
                                        e.Graphics.DrawLine(wallPen, corners[i], corners[i + 1]);
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

                                        e.Graphics.DrawLine(wallPen, corners[i], doorStart);
                                        e.Graphics.DrawLine(wallPen, doorEnd, corners[i + 1]);

                                        // If the door is closed then paint in a door using the door color,
                                        // otherwise leave it unpainted.
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
                                            e.Graphics.FillRectangle(doorBrush, doorRect);
                                            e.Graphics.DrawRectangle(doorPen, doorRect);
                                        }
                                        break;
                                    default:
                                        break;
                                } // switch (curWall.Type)
                            } // for i in directions do
                        } // if room intersects cliprect then
                    } // if room isn't null then
                } // for x, y in bounds do
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            Point roomPt = GetRoomPtForCanvasPoint(e.Location);
            if (_Map.Bounds.Contains(roomPt))
            {
                label1.Text = "X: " + roomPt.X;
                label2.Text = "Y: " + roomPt.Y;
            }
            else
            {
                label1.Text = "";
                label2.Text = "";
            }
        }

        private MouseEventArgs MouseDownArgs;

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            MouseDownArgs = e;
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point startPt = GetRoomPtForCanvasPoint(MouseDownArgs.Location);
                Point endPt = GetRoomPtForCanvasPoint(e.Location);

                Rectangle roomRect = RectHelper.FromPoints(startPt, endPt);
                // Inflate the bottom-right so both points are included.
                roomRect.Size += new Size(1, 1);

                // Always preserve existing doors on the outer edge of the new room.
                Area.DrawWallMode wallMode = Area.DrawWallMode.Overwrite;
                // Default room color for new/changed rooms.
                Color roomColor = Color.DeepSkyBlue;

                // Some things depend on the room that the drag started in.
                Room startRoom = _Map.GetRoom(startPt);
                if (startRoom != null)
                {
                    // Also preserve existing open walls if the drawing started inside an existing room.
                    wallMode = Area.DrawWallMode.Extend;
                    // Use the starting room's color when extending it this way.
                    roomColor = startRoom.Color;
                }

                _Map.DrawRectangle(roomRect, roomColor, wallMode);

                // Invalidate the changed rooms directly.
                // If the bounds changed as a result of DrawRectangle, the BoundsChanged event handler
                // will deal with invalidating the fallout from that.
                Invalidate(GetCanvasRectForRoom(roomRect, true));
            }

            MouseDownArgs = null;
        }
    }
}
