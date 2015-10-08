using System.Drawing;
using CoordinateHelper;

namespace MapTool
{
    abstract class EditTool
    {
        private Point? downPt;

        // This is the canvas/map the tool is operating on.
        public MapBox MapBox { get; set; }

        public virtual void Cancel()
        {
            downPt = null;
        }
        public virtual void MouseDown(Point location)
        {
            downPt = location;
        }

        protected abstract Rectangle? MouseUpInternal(Point upPoint, Point downPoint);
        public void MouseUp(Point location)
        {
            // Sometimes there can be a mouseup without a relevant mousedown, just ignore it
            // if that happens.
            if (downPt.HasValue)
            {
                Rectangle? result = MouseUpInternal(location, downPt.Value);

                // this is kind of hacky, the mapbox should invalidate itself in response to changes.
                // todo: move this into an OnRoomChange or something in the map and have the mapbox handle it.
                downPt = null;
                if (result.HasValue)
                    MapBox.Invalidate(result.Value);
            }
        }
        public virtual void MouseMove(Point location)
        {
            // nothing for now
        }
    }

    abstract class RoomEditTool : EditTool
    {
        protected abstract void FillRoomRect(Rectangle roomRect, Point startRoomPt);
        protected override Rectangle? MouseUpInternal(Point upPoint, Point downPoint)
        {
            Point startRoomPt = MapBox.GetRoomPtForCanvasPoint(downPoint);
            Point endRoomPt = MapBox.GetRoomPtForCanvasPoint(upPoint);
            Rectangle roomRect = RectHelper.FromPoints(startRoomPt, endRoomPt);

            // Inflate the bottom-right so both points are included.
            roomRect.Size += new Size(1, 1);

            // Some things depend on the actual starting room, so pass that in too.
            FillRoomRect(roomRect, startRoomPt);

            // It may be better to move this into the FillRoomRect call eventually,
            // but for now just assume that descendants will always change the whole rect.
            return MapBox.GetCanvasRectForRoom(roomRect, true);
        }
    }

    class ExtendRoomEditTool: RoomEditTool
    {
        protected override void FillRoomRect(Rectangle roomRect, Point startRoomPt)
        {
            // convenience variable.
            Map map = MapBox.Map;
            // Always preserve existing doors on the outer edge of the new room.
            Map.DrawWallMode wallMode = Map.DrawWallMode.Overwrite;
            // Default room color for new/changed rooms.
            Color roomColor = Color.DeepSkyBlue;

            // Some things depend on the room that the drag started in.
            Room startRoom = map.GetRoom(startRoomPt);
            if (startRoom != null)
            {
                // Also preserve existing open walls if the drawing started inside an existing room.
                wallMode = Map.DrawWallMode.Extend;
                // Use the starting room's color when extending it this way.
                roomColor = startRoom.Color;
            }

            map.DrawRectangle(roomRect, roomColor, wallMode);
        }
    }
}
