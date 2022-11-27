using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.Models
{
    public class MapPoint
    {
        public System.Windows.Shapes.Ellipse? Circle { get; set; }

        public double PercentageX { get; set; }

        public double PercentageY { get; set; }

        /// <summary>
        /// The in-game X-coordinate.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// The in-game Y-coordinate.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// The in-game Z-coordinate.
        /// </summary>
        public double Z { get; set; }

        public int AssociatedAreaID { get; set; } = -1;

        public double PercentageScale { get; set; }
    }
}
