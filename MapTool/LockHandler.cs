using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapTool
{
    public delegate void LockChangeEventHandler(object sender, bool locked);
    class LockHandler
    {
        private int UpdateCount = 0;
        public event LockChangeEventHandler LockChanged;

        // nb. not thread safe, but that can be added later.
        public void Lock()
        {
            if (++UpdateCount == 1)
                LockChanged?.Invoke(this, true);
        }

        public void Unlock()
        {
            if (--UpdateCount == 0)
                LockChanged?.Invoke(this, false);
        }

        public bool Locked {
            get { return UpdateCount > 0; }
        }
    }
}
