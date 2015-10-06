using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoordinateHelper;

namespace MapTool
{
    abstract class EditTool
    {
        private Point? downPt;

        // Need this to convert between painted pixels and map coords
        public MapPainter Painter { get; set; }
        // Need this to make changes to the map.
        public Map Map { get; set; }

        public virtual void Cancel()
        {
            downPt = null;
        }
        public virtual void MouseDown(Point location)
        {
            downPt = location;
        }

        protected abstract Rectangle? MouseUpInternal(Point upPoint, Point downPoint);
        public Rectangle? MouseUp(Point location)
        {
            // Sometimes there can be a mouseup without a relevant mousedown, just ignore it
            // if that happens.
            if (downPt.HasValue)
            {
                Rectangle? result = MouseUpInternal(location, downPt.Value);
                // Clear the down point before returning.
                downPt = null;
                return result;
            }
            else return null;
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
            Point startRoomPt = Painter.GetRoomPtForCanvasPoint(downPoint);
            Point endRoomPt = Painter.GetRoomPtForCanvasPoint(upPoint);
            Rectangle roomRect = RectHelper.FromPoints(startRoomPt, endRoomPt);

            // Inflate the bottom-right so both points are included.
            roomRect.Size += new Size(1, 1);

            // Some things depend on the actual starting room, so pass that in too.
            FillRoomRect(roomRect, startRoomPt);

            // It may be better to move this into the FillRoomRect call eventually,
            // but for now just assume that descendants will always change the whole rect.
            return Painter.GetCanvasRectForRoom(roomRect, true);
        }
    }

    class ExtendRoomEditTool: RoomEditTool
    {
        protected override void FillRoomRect(Rectangle roomRect, Point startRoomPt)
        {
            // Always preserve existing doors on the outer edge of the new room.
            Map.DrawWallMode wallMode = Map.DrawWallMode.Overwrite;
            // Default room color for new/changed rooms.
            Color roomColor = Color.DeepSkyBlue;

            // Some things depend on the room that the drag started in.
            Room startRoom = Map.GetRoom(startRoomPt);
            if (startRoom != null)
            {
                // Also preserve existing open walls if the drawing started inside an existing room.
                wallMode = Map.DrawWallMode.Extend;
                // Use the starting room's color when extending it this way.
                roomColor = startRoom.Color;
            }

            Map.DrawRectangle(roomRect, roomColor, wallMode);
        }
    }
}
