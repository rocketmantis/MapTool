using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapTool
{
    // In order to make an indexed property Room.Walls[], we have to delegate
    // the indexer into a subclass.
    [Serializable()]
    class StaticArrayProperty<T>
    {
        // This has to be public so we can stream it.
        // Generally outside code won't go to the array directly though.
        public T[] arr = null;

        // And a default indexer to go right into the array.
        public T this[int i]
        {
            get { return arr[i]; }
            set { arr[i] = value; }
        }

        // Constructors.
        public StaticArrayProperty(int Count)
        {
            arr = new T[Count];
        }
        public StaticArrayProperty() : this(0)
        { }
    }
}
