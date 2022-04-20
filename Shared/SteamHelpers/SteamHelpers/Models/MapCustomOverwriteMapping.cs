using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class MapCustomOverwriteMapping
    {
        public string? DDSFileName { get; set; } = string.Empty;
        public System.Windows.Point CoordOffset { get; set; }
        public float MapScale { get; set; } = 0;
    }
}
