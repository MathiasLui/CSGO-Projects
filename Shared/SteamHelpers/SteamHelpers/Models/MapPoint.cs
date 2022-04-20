using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class MapPoint
    {
        public System.Windows.Shapes.Ellipse? Circle { get; set; }

        public double PercentageX { get; set; }

        public double PercentageY { get; set; }

        public double Z { get; set; }

        public int AssociatedAreaID { get; set; } = -1;

        public double PercentageScale { get; set; }
    }
}
