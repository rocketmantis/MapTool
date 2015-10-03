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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using CoordinateHelper;

namespace MapTool
{
    public partial class Form1 : Form
    {
        private Map _Map = new Map();
        private MapPainter _Painter = new MapPainter();
        private string _Filename = null;

        private void MapBoundsChanged(object sender, Rectangle oldBounds)
        {
            // The Maps that have changed are the union of the two bounds,
            // excluding the intersection. ie. the xor of the two rects.
            Region invalidRegion = new Region(_Painter.GetCanvasRectForRoom(oldBounds, true));
            invalidRegion.Xor(_Painter.GetCanvasRectForRoom(_Map.Bounds, true));
            Invalidate(invalidRegion);
        }

        public Form1()
        {
            InitializeComponent();

            _Map.BoundsChanged += MapBoundsChanged;

            //CreateTestRooms();
        }

        private void CreateTestRooms()
        {
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
            _Painter.Paint(_Map, e.Graphics, e.ClipRectangle);
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            Point roomPt = _Painter.GetRoomPtForCanvasPoint(e.Location);
            if (_Map.Bounds.Contains(roomPt))
            {
                toolStripStatusLabel1.Text = "X: " + roomPt.X;
                toolStripStatusLabel2.Text = "Y: " + roomPt.Y;
            }
            else
            {
                toolStripStatusLabel1.Text = "";
                toolStripStatusLabel2.Text = "";
            }
        }

        private MouseEventArgs MouseDownArgs;

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            MouseDownArgs = e;
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            // Sometimes there can be a mouseup without a relevant mousedown, just ignore it
            // if that happens.
            if ((MouseDownArgs != null) && (e.Button == MouseButtons.Left))
            {
                Point startPt = _Painter.GetRoomPtForCanvasPoint(MouseDownArgs.Location);
                Point endPt = _Painter.GetRoomPtForCanvasPoint(e.Location);

                Rectangle roomRect = RectHelper.FromPoints(startPt, endPt);
                // Inflate the bottom-right so both points are included.
                roomRect.Size += new Size(1, 1);

                // Always preserve existing doors on the outer edge of the new room.
                Map.DrawWallMode wallMode = Map.DrawWallMode.Overwrite;
                // Default room color for new/changed rooms.
                Color roomColor = Color.DeepSkyBlue;

                // Some things depend on the room that the drag started in.
                Room startRoom = _Map.GetRoom(startPt);
                if (startRoom != null)
                {
                    // Also preserve existing open walls if the drawing started inside an existing room.
                    wallMode = Map.DrawWallMode.Extend;
                    // Use the starting room's color when extending it this way.
                    roomColor = startRoom.Color;
                }

                _Map.DrawRectangle(roomRect, roomColor, wallMode);

                // Invalidate the changed rooms directly.
                // If the bounds changed as a result of DrawRectangle, the BoundsChanged event handler
                // will deal with invalidating the fallout from that.
                Invalidate(_Painter.GetCanvasRectForRoom(roomRect, true));
            }

            MouseDownArgs = null;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                // unhook event from old map
                _Map.BoundsChanged -= MapBoundsChanged;

                Stream loadStream = File.OpenRead(openFileDialog1.FileName);
                BinaryFormatter deserializer = new BinaryFormatter();
                _Map = (Map)deserializer.Deserialize(loadStream);

                // hook up event to new map
                _Map.BoundsChanged += MapBoundsChanged;
                Invalidate();

                _Filename = openFileDialog1.FileName;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_Filename != null)
                SaveMapToFile(_Filename);
            else
                saveAsToolStripMenuItem_Click(sender, e);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog(this) == DialogResult.OK)
                SaveMapToFile(saveFileDialog1.FileName);
        }

        private void SaveMapToFile(string filename)
        {
            Stream saveStream = File.Create(saveFileDialog1.FileName);
            BinaryFormatter serializer = new BinaryFormatter();
            serializer.Serialize(saveStream, _Map);
            saveStream.Close();

            _Filename = filename;
        }
    }
}
