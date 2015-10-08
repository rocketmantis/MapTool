using System;
using System.Collections.Generic;
using System.Drawing;

namespace MapTool
{
    [Serializable()]
    public class RoomRow : IEnumerable<Room>
    {
        List<Room> _Rooms = new List<Room>();
        public Room this[int i] {
            get { return _Rooms[i]; }
            set { _Rooms[i] = value; }
        }

        Boolean HasSharedLeftWall(int index)
        {
            return (index > 0) && (index < _Rooms.Count) &&
                (_Rooms[index - 1] != null) && (_Rooms[index] != null);
        }

        // Core functionality -- insert/delete rooms and update walls as appropriate.
        public void InsertRooms(int index, int numRooms)
        {
            //// Split the shared wall if necessary, so each room has a different copy.
            //if (HasSharedLeftWall(index))
            //    _Rooms[index].SplitWall(Direction.Left);

            for (int i = 0; i < numRooms; i++)
            {
                _Rooms.Insert(index + i, null);
            }
        }
        public void DeleteRooms(int index, int numRooms)
        {
            _Rooms.RemoveRange(index, numRooms);
            // Now we have to merge the newly shared walls.
            if (HasSharedLeftWall(index))
                _Rooms[index].MergeWalls(Direction.Left, _Rooms[index - 1]);
        }

        // Enumerating.
        public IEnumerator<Room> GetEnumerator() { return _Rooms.GetEnumerator(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return this.GetEnumerator(); }

        // For convenience.
        public void AddRooms(int numRooms)
        {
            InsertRooms(_Rooms.Count, numRooms);
        }
    };

    [Serializable()]
    public class RoomGrid : IEnumerable<IEnumerable<Room>>
    {
        List<RoomRow> _Rows = new List<RoomRow>();
        int _Width = 0;

        public int Width {
            get { return _Width; }
        }
        public int Height {
            get { return _Rows.Count; }
        }
        public Size Size {
            get { return new Size(Width, Height); }
        }

        // Lookup methods.
        public Room GetRoom(Point roomPt)
        {
            return _Rows[roomPt.Y][roomPt.X];
        }
        public Room GetAdjacentRoom(Point roomPt, int direction)
        {
            switch (direction)
            {
                case Direction.Left: roomPt.X--; break;
                case Direction.Top: roomPt.Y--; break;
                case Direction.Right: roomPt.X++; break;
                case Direction.Bottom: roomPt.Y++; break;
            }

            Rectangle boundsRect = new Rectangle(0, 0, Width, Height);
            return boundsRect.Contains(roomPt) ? GetRoom(roomPt) : null;
        }
        public IEnumerable<Room> GetRowRooms(int y)
        {
            return _Rows[y];
        }

        // Iterating.
        public IEnumerator<IEnumerable<Room>> GetEnumerator()
        {
            return _Rows.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return this.GetEnumerator(); }

        // Core functionality -- columns
        public void InsertColumns(int index, int numCols)
        {
            foreach (RoomRow row in _Rows)
                row.InsertRooms(index, numCols);
            _Width += numCols;
        }
        public void RemoveColumns(int index, int numCols)
        {
            foreach (RoomRow row in _Rows)
                row.DeleteRooms(index, numCols);
            _Width -= numCols;
        }

        // Adding new rooms.
        public Room CreateRoom(Point roomPt)
        {
            RoomRow row = _Rows[roomPt.Y];
            if (row[roomPt.X] != null)
                throw new ArgumentException("Room " + roomPt.X + " is already assigned", "roomPt.X");

            Room newRoom = new Room();

            // Connect or create the walls.
            Room adjacent = null;
            for (int i = Direction.First; i < Direction.Count; i++)
            {
                adjacent = GetAdjacentRoom(roomPt, i);
                newRoom.ConnectWall(i, adjacent);
            }

            row[roomPt.X] = newRoom;
            return newRoom;
        }

        // Core functionality -- rows
        public void InsertRows(int index, int rowCount)
        {
            //// First we need to split any shared walls along the insertion line.
            //if ((index > 0) && (index < Height))
            //{
            //    RoomRow prevRow = _Rows[index - 1];
            //    RoomRow curRow = _Rows[index];
            //    for (int i = 0; i < Width; i++)
            //    {
            //        // Split shared walls.
            //        if ((prevRow[i] != null) && (curRow[i] != null))
            //            curRow[i].SplitWall(Direction.Top);
            //    }
            //}

            // Now go ahead and insert new rows with null rooms.
            for (int i = 0; i < rowCount; i++)
            {
                RoomRow newRow = new RoomRow();
                newRow.AddRooms(Width);
                _Rows.Insert(index + i, newRow);
            }
        }
        public void RemoveRows(int index, int rowCount)
        {
            _Rows.RemoveRange(index, rowCount);

            // Merge any walls that now have to be shared.
            if ((index > 0) && (index < Height))
            {
                RoomRow prevRow = _Rows[index - 1];
                RoomRow curRow = _Rows[index];
                for (int i = 0; i < Width; i++)
                {
                    if ((prevRow[i] != null) && (curRow[i] != null))
                        curRow[i].MergeWalls(Direction.Top, prevRow[i]);
                }
            }
        }

        // Convenience methods
        public void AddColumns(int numCols)
        {
            InsertColumns(Width, numCols);
        }
        public void AddRows(int rowCount)
        {
            InsertRows(_Rows.Count, rowCount);
        }
        public RoomRow InsertRow(int index)
        {
            InsertRows(index, 1);
            return _Rows[index];
        }
        public RoomRow AddRow()
        {
            InsertRows(Height, 1);
            return _Rows[Height];
        }
    };
}