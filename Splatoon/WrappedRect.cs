using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon
{
    public class WrappedRect
    {
        internal string GUID = Guid.NewGuid().ToString();
        public Rectangle Rect;

        public WrappedRect(int x, int y, int width, int height)
        {
            Rect = new(x, y, width, height);
        }
    }
}
