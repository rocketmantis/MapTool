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

namespace MapTool
{
    public partial class Form1 : Form
    {
        private Map Map { get; set; }
        private MapPainter Painter { get; set; }
        private string Filename { get; set; }
        private EditTool ActiveTool { get; set; }

        private void MapBoundsChanged(object sender, Rectangle oldBounds)
        {
            // The Maps that have changed are the union of the two bounds,
            // excluding the intersection. ie. the xor of the two rects.
            Region invalidRegion = new Region(Painter.GetCanvasRectForRoom(oldBounds, true));
            invalidRegion.Xor(Painter.GetCanvasRectForRoom(Map.Bounds, true));
            Invalidate(invalidRegion);
        }

        public Form1()
        {
            InitializeComponent();

            // Initialize private properties.
            Filename = null;

            Map = new Map();
            Map.BoundsChanged += MapBoundsChanged;

            Painter = new MapPainter();

            // Eventually we will switch the active tool based on what they pick from the toolbar.
            // For now there's only one hard-coded tool.
            ActiveTool = new ExtendRoomEditTool();
            ActiveTool.Map = Map;
            ActiveTool.Painter = Painter;

            //CreateTestRooms();
        }

        private void CreateTestRooms()
        {
            // We shouldn't need to explicitly set the bounds anymore;
            // CreateRoom should extend the boundary as necessary.
            //Map.Bounds = new Rectangle(-1, -1, 5, 5);

            // Make a room for the top-left and bottom-right corners
            Room newRoom = Map.CreateRoom(new Point(-1, -1));
            newRoom.Color = Color.MediumBlue;
            newRoom.Walls[Direction.Top].Type = WallType.OpenDoor;
            newRoom.Walls[Direction.Left].Type = WallType.OpenDoor;

            newRoom = Map.CreateRoom(new Point(3, 3));
            newRoom.Color = Color.MediumBlue;
            newRoom.Walls[Direction.Bottom].Type = WallType.OpenDoor;
            newRoom.Walls[Direction.Right].Type = WallType.OpenDoor;

            // Fill in a few rooms to start with.
            newRoom = Map.CreateRoom(new Point(2, 1));
            newRoom.Color = Color.ForestGreen;
            newRoom.Walls[Direction.Top].Type = WallType.Solid;
            newRoom.Walls[Direction.Right].Type = WallType.Solid;
            newRoom.Walls[Direction.Bottom].Type = WallType.Solid;
            newRoom.Walls[Direction.Left].Type = WallType.ClosedDoor;
            newRoom.Walls[Direction.Left].DoorColor = Color.DeepSkyBlue;

            newRoom = Map.CreateRoom(new Point(0, 2));
            newRoom.Color = Color.MediumBlue;
            newRoom.Walls[Direction.Top].Type = WallType.OpenDoor;
            newRoom.Walls[Direction.Left].Type = WallType.Solid;
            newRoom.Walls[Direction.Bottom].Type = WallType.Solid;
            // Let's overwrite the left wall and see what happens.
            newRoom.Walls[Direction.Right].Type = WallType.ClosedDoor;
            newRoom.Walls[Direction.Right].DoorColor = Color.OrangeRed;

            newRoom = Map.CreateRoom(new Point(0, 1));
            newRoom.Color = Color.MediumBlue;
            newRoom.Walls[Direction.Top].Type = WallType.Solid;
            newRoom.Walls[Direction.Left].Type = WallType.Solid;
            // Bottom should be defined by the connection already
            // Right we'll leave open.

            newRoom = Map.CreateRoom(new Point(1, 1));
            newRoom.Color = Color.MediumBlue;
            newRoom.Walls[Direction.Top].Type = WallType.Solid;
            newRoom.Walls[Direction.Bottom].Type = WallType.Solid;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Painter.Paint(Map, e.Graphics, e.ClipRectangle);
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            Point roomPt = Painter.GetRoomPtForCanvasPoint(e.Location);
            if (Map.Bounds.Contains(roomPt))
            {
                toolStripStatusLabel1.Text = "X: " + roomPt.X;
                toolStripStatusLabel2.Text = "Y: " + roomPt.Y;
            }
            else
            {
                toolStripStatusLabel1.Text = "";
                toolStripStatusLabel2.Text = "";
            }

            if (ActiveTool != null)
                ActiveTool.MouseMove(e.Location);
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                ActiveTool.MouseDown(e.Location);
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            // Sometimes there can be a mouseup without a relevant mousedown, just ignore it
            // if that happens.
            if (e.Button == MouseButtons.Left)
            {
                Rectangle? invalidRect = ActiveTool.MouseUp(e.Location);

                if (invalidRect.HasValue)
                    Invalidate(invalidRect.Value);
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                // unhook event from old map
                Map.BoundsChanged -= MapBoundsChanged;

                Stream loadStream = File.OpenRead(openFileDialog1.FileName);
                BinaryFormatter deserializer = new BinaryFormatter();
                Map = (Map)deserializer.Deserialize(loadStream);

                // hook up event to new map
                Map.BoundsChanged += MapBoundsChanged;
                Invalidate();

                Filename = openFileDialog1.FileName;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Filename != null)
                SaveMapToFile(Filename);
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
            serializer.Serialize(saveStream, Map);
            saveStream.Close();

            Filename = filename;
        }
    }
}
