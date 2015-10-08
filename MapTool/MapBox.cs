using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.ComponentModel;
using CoordinateHelper;

namespace MapTool
{
    public partial class MapBox : Control
    {
        private Size RoomSize;
        private const int WallWidth = 4;
        private const int HalfWallWidth = WallWidth / 2;
        private const int DoorBorder = 1;

        private Point Origin;
        private Color WallColor;
        private List<Point> DirtyRooms;

        // inherited from Control
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override bool AutoSize { get; set; }

        [NonSerialized()]
        private Map map;
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Map Map {
            get { return map; }
            set
            {
                // unhook the event from the old map
                if (map != null)
                    map.BoundsChanged -= MapBoundsChanged;

                map = value;

                // hook up events to the new map
                if (map != null)
                    map.BoundsChanged += MapBoundsChanged;

                Invalidate();
            }
        }

        public MapBox()
        {
            InitializeComponent();

            RoomSize = new Size(32, 32);
            Origin = new Point(80, 80);
            WallColor = Color.White;
            DirtyRooms = new List<Point>();

            Map = new Map();
            // shouldn't need to set the event, the property setter will handle it.
            //Map.BoundsChanged += MapBoundsChanged;
        }

        private void MapBoundsChanged(object sender, Rectangle oldBounds)
        {
            // The Maps that have changed are the union of the two bounds,
            // excluding the intersection. ie. the xor of the two rects.
            Region invalidRegion = new Region(GetCanvasRectForRoom(oldBounds, true));
            invalidRegion.Xor(GetCanvasRectForRoom(Map.Bounds, true));
            Invalidate(invalidRegion);

            if (this.AutoSize)
                this.SetBoundsCore(this.Left, this.Top, this.Width, this.Height, BoundsSpecified.Size);
        }

        // This calculates the size to use when autosizing.
        private Size GetAutoSize()
        {
            if (Map != null)
                return GetCanvasRectForRoom(Map.Bounds, true).Size + (Size)Origin;
            else
                return new Size(100, 100);
        }

        // This is called when the parent control wants to know what size this control would like to be.
        public override Size GetPreferredSize(Size proposedSize)
        {
            return GetAutoSize();
        }

        // Override to force the size to use the autosize.
        // I'm not sure this is necessary...
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            if (this.AutoSize && (specified & BoundsSpecified.Size) != 0)
            {
                Size size = GetAutoSize();
                width = size.Width;
                height = size.Height;
            }
            base.SetBoundsCore(x, y, width, height, specified);
        }

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

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            if (Map == null)
                return;

            Size MapSize = Map.Bounds.Size;
            MapSize.Width *= RoomSize.Width;
            MapSize.Height *= RoomSize.Height;

            Point MapLocation = Map.Bounds.Location;
            MapLocation.X *= RoomSize.Width;
            MapLocation.Y *= RoomSize.Height;
            MapLocation.Offset(Origin);

            Rectangle MapRect = new Rectangle(MapLocation, MapSize);
            MapRect.Inflate(HalfWallWidth, HalfWallWidth);

            if (!MapRect.IntersectsWith(pe.ClipRectangle))
                return;

            Rectangle paintRect = Rectangle.Intersect(MapRect, pe.ClipRectangle);

            // Fill in the default background.
            // may be able to skip this by setting the BackColor
            //pe.Graphics.FillRectangle(new SolidBrush(Color.DimGray), paintRect);

            // Get the list of rooms that need to be repainted, given the pe.ClipRectangle.
            List<Point> dirtyRooms = GetDirtyRooms(Map, pe.ClipRectangle);

            // first pass across rooms: fill in the background
            PaintRoomBGs(Map, pe.Graphics, dirtyRooms);

            // second pass across rooms: draw the walls and doors
            PaintRoomWalls(Map, pe.Graphics, dirtyRooms);
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
