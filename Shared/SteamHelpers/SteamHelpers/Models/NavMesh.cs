using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class NavMesh
    {
        public NavHeader? Header { get; set; } = new NavHeader();

        public float? MinZ { get; set; }

        public float? MaxZ { get; set; }
    }
}
