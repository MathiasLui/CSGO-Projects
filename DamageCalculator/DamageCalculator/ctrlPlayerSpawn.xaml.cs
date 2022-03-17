using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Damage_Calculator
{
    /// <summary>
    /// Interaction logic for ctrlPlayerSpawn.xaml
    /// </summary>
    public partial class ctrlPlayerSpawn : UserControl
    {
        public ctrlPlayerSpawn()
        {
            InitializeComponent();
        }

        public void SetColour(Color colour)
        {
            this.ellipse.Stroke = this.rectangle.Fill = new SolidColorBrush(colour);
        }

        public void SetEllipseFill(Color colour)
        {
            this.ellipse.Fill = new SolidColorBrush(colour);
        }

        public void SetRotation(double angle)
        {
            gridControl.RenderTransform = new RotateTransform(angle);
        }
    }
}
