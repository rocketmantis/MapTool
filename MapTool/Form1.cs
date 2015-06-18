using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        public Form1()
        {
            InitializeComponent();
            _Map.Bounds = new Rectangle(-1, -1, 5, 5);

            Room newRoom = _Map.CreateRoom(new Point(-1, -1));
            newRoom.Color = Color.MediumBlue;
            newRoom.Walls[Direction.Top].Type = WallType.OpenDoor;
            newRoom.Walls[Direction.Left].Type = WallType.OpenDoor;

            newRoom = _Map.CreateRoom(new Point(3, 3));
            newRoom.Color = Color.MediumBlue;
            newRoom.Walls[Direction.Bottom].Type = WallType.OpenDoor;
            newRoom.Walls[Direction.Right].Type = WallType.OpenDoor;

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

        private Rectangle GetRoomRect(Point roomPt)
        {
            return new Rectangle(
                Origin.Y + roomPt.X * RoomSize.Width,
                Origin.Y + roomPt.Y * RoomSize.Height,
                RoomSize.Width,
                RoomSize.Height);
        }
        private Boolean HitTestRoom(Point canvasPoint, out Point roomPt)
        {
            // This is just the reverse of GetRoomRect, really.
            Point boundsPoint = Point.Subtract(canvasPoint, (Size)Origin);
            roomPt = new Point(
                (int)Math.Floor((double)boundsPoint.X / RoomSize.Width),
                (int)Math.Floor((double)boundsPoint.Y / RoomSize.Height));
            return _Map.Bounds.Contains(roomPt);
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
            // e.Graphics.DrawRectangle(wallPen, areaRect);

            Point roomPt = new Point();

            for (roomPt.Y = _Map.Bounds.Top; roomPt.Y < _Map.Bounds.Bottom; roomPt.Y += 1)
                for (roomPt.X = _Map.Bounds.Left; roomPt.X < _Map.Bounds.Right; roomPt.X += 1)
                {
                    Room curRoom = _Map.GetRoom(roomPt);
                    if (curRoom != null)
                    {
                        Rectangle roomRect = GetRoomRect(roomPt);

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

                                if (curWall.Type != WallType.None)
                                {
                                    // If there's a door, fill the door back in.
                                    if (curWall.Type == WallType.Solid)
                                        e.Graphics.DrawLine(wallPen, corners[i], corners[i + 1]);
                                    else
                                    {
                                        // Doors run from 1/4 to 3/4 of the wall.
                                        const int DoorScale = 4;

                                        Point doorStart = new Point(
                                                (corners[i].X * (DoorScale-1) + corners[i + 1].X) / DoorScale,
                                                (corners[i].Y * (DoorScale-1) + corners[i + 1].Y) / DoorScale);
                                        Point doorEnd = new Point(
                                                (corners[i].X + corners[i + 1].X * (DoorScale-1)) / DoorScale,
                                                (corners[i].Y + corners[i + 1].Y * (DoorScale-1)) / DoorScale);

                                        e.Graphics.DrawLine(wallPen, corners[i], doorStart);
                                        e.Graphics.DrawLine(wallPen, doorEnd, corners[i + 1]);

                                        // If the door is closed the paint it with the door color,
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
                                    }
                                }

                            }
                        }
                    }
                }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            Point roomPt;
            if (HitTestRoom(e.Location, out roomPt))
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
    }
}
