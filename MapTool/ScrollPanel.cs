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
    public partial class ScrollPanel : Panel
    {
        public ScrollPanel()
        {
            InitializeComponent();
        }

        // Override: Don't autoscroll when focus changes. This means that tabbing to a control
        // that is scrolled out of visibility won't work, but also means that the scroll won't
        // reset when focus changes to a control that's already partially visible.
        protected override System.Drawing.Point ScrollToControl(Control activeControl)
        {
            return this.DisplayRectangle.Location;
        }
    }
}
