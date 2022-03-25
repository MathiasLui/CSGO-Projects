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

namespace ConfigManagerV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region events
        private void changeTheme_Click(object sender, RoutedEventArgs e)
        {
            switch (int.Parse(((MenuItem)sender).Uid))
            {
                case 0:
                    REghZyFramework.Themes.ThemesController.SetTheme(REghZyFramework.Themes.ThemesController.ThemeTypes.Dark);
                    break;
                case 1:
                    REghZyFramework.Themes.ThemesController.SetTheme(REghZyFramework.Themes.ThemesController.ThemeTypes.Light);
                    break;
            }
            e.Handled = true;
        }
        #endregion
    }
}
