using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamShared.Models
{
    public class MapPoint
    {
        /// <summary>
        ///     The actual UI circle element of this point displayed on the map.
        /// </summary>
        public System.Windows.Shapes.Ellipse? Circle { get; set; }

        /// <summary>
        ///     The percentage that this point is at on the map's X axis (0% is left, 100% is right).
        /// </summary>
        public double PercentageX { get; set; }

        /// <summary>
        ///     The percentage that this point is at on the map's Y axis (0% is top, 100% is bottom).
        /// </summary>
        public double PercentageY { get; set; }

        /// <summary>
        ///     The in-game X-coordinate.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        ///     The in-game Y-coordinate.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        ///     The in-game Z-coordinate, if any.
        /// </summary>
        public double? Z { get; set; }

        /// <summary>
        ///     The ID of the area that this point was put on.
        /// </summary>
        /// <remarks>
        ///     If there is no area associated, it will be negative.
        /// </remarks>
        public int AssociatedAreaID { get; set; } = -1;

        /// <summary>
        ///     The percentage of how wide this circle is relative to the map's width or height.
        /// </summary>
        /// <remarks>
        ///     Width or height doesn't matter as maps are square shaped.
        /// </remarks>
        public double PercentageScale { get; set; }
    }
}
