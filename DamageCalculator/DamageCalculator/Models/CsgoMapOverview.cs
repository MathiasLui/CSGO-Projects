using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Damage_Calculator.Models
{
    public class CsgoMapOverview
    {
        public BitmapSource MapImage { get; set; }

        public string MapImagePath { get; set; }

        public string MapFileName { get; set; }

        public float MapSizeMultiplier { get; set; }

        public float UpperLeftWorldXCoordinate { get; set; } = -1;

        public float UpperLeftWorldYCoordinate { get; set; } = -1;

        public float CTSpawnMultiplierX { get; set; } = -1;

        public float CTSpawnMultiplierY { get; set; } = -1;

        public float TSpawnMultiplierX { get; set; } = -1;

        public float TSpawnMultiplierY { get; set; } = -1;

        public float BombAX { get; set; } = -1;

        public float BombAY { get; set; } = -1;

        public float BombBX { get; set; } = -1;

        public float BombBY { get; set; } = -1;
    }
}
