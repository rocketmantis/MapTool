using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace MapTool
{
    public partial class MapEditForm : Form
    {
        private string Filename { get; set; }
        private EditTool ActiveTool { get; set; }

        public MapEditForm()
        {
            InitializeComponent();

            // Initialize private properties.
            Filename = null;

            // Eventually we will switch the active tool based on what they pick from the toolbar.
            // For now there's only one hard-coded tool.
            ActiveTool = new ExtendRoomEditTool();
            ActiveTool.MapBox = mapBox1;

            //CreateTestRooms();
        }

        private void CreateTestRooms()
        {
            // We shouldn't need to explicitly set the bounds anymore;
            // CreateRoom should extend the boundary as necessary.
            //mapBox1.Map.Bounds = new Rectangle(-1, -1, 5, 5);

            // Make a room for the top-left and bottom-right corners
            Room newRoom = mapBox1.Map.CreateRoom(new Point(-1, -1));
            newRoom.Color = Color.MediumBlue;
            newRoom.Walls[Direction.Top].Type = WallType.OpenDoor;
            newRoom.Walls[Direction.Left].Type = WallType.OpenDoor;

            newRoom = mapBox1.Map.CreateRoom(new Point(3, 3));
            newRoom.Color = Color.MediumBlue;
            newRoom.Walls[Direction.Bottom].Type = WallType.OpenDoor;
            newRoom.Walls[Direction.Right].Type = WallType.OpenDoor;

            // Fill in a few rooms to start with.
            newRoom = mapBox1.Map.CreateRoom(new Point(2, 1));
            newRoom.Color = Color.ForestGreen;
            newRoom.Walls[Direction.Top].Type = WallType.Solid;
            newRoom.Walls[Direction.Right].Type = WallType.Solid;
            newRoom.Walls[Direction.Bottom].Type = WallType.Solid;
            newRoom.Walls[Direction.Left].Type = WallType.ClosedDoor;
            newRoom.Walls[Direction.Left].DoorColor = Color.DeepSkyBlue;

            newRoom = mapBox1.Map.CreateRoom(new Point(0, 2));
            newRoom.Color = Color.MediumBlue;
            newRoom.Walls[Direction.Top].Type = WallType.OpenDoor;
            newRoom.Walls[Direction.Left].Type = WallType.Solid;
            newRoom.Walls[Direction.Bottom].Type = WallType.Solid;
            // Let's overwrite the left wall and see what happens.
            newRoom.Walls[Direction.Right].Type = WallType.ClosedDoor;
            newRoom.Walls[Direction.Right].DoorColor = Color.OrangeRed;

            newRoom = mapBox1.Map.CreateRoom(new Point(0, 1));
            newRoom.Color = Color.MediumBlue;
            newRoom.Walls[Direction.Top].Type = WallType.Solid;
            newRoom.Walls[Direction.Left].Type = WallType.Solid;
            // Bottom should be defined by the connection already
            // Right we'll leave open.

            newRoom = mapBox1.Map.CreateRoom(new Point(1, 1));
            newRoom.Color = Color.MediumBlue;
            newRoom.Walls[Direction.Top].Type = WallType.Solid;
            newRoom.Walls[Direction.Bottom].Type = WallType.Solid;
        }

        private void mapBox1_MouseMove(object sender, MouseEventArgs e)
        {
            Point roomPt = mapBox1.GetRoomPtForCanvasPoint(e.Location);
            if (mapBox1.Map.Bounds.Contains(roomPt))
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

        private void mapBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                ActiveTool.MouseDown(e.Location);
        }

        private void mapBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                ActiveTool.MouseUp(e.Location);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                Stream loadStream = File.OpenRead(openFileDialog1.FileName);
                BinaryFormatter deserializer = new BinaryFormatter();
                mapBox1.Map = (Map)deserializer.Deserialize(loadStream);

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
            serializer.Serialize(saveStream, mapBox1.Map);
            saveStream.Close();

            Filename = filename;
        }

        private void panel1_Scroll(object sender, ScrollEventArgs e)
        {
            Filename = Filename;
        }
    }
}
