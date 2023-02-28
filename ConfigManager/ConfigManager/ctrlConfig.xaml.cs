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
using System.IO;

namespace ConfigManager
{
    /// <summary>
    /// Interaction logic for ctrlConfig.xaml
    /// </summary>
    public partial class ctrlConfig : UserControl
    {
        public ctrlConfig()
        {
            InitializeComponent();
        }
        public delegate void EditConfig(object sender, string sPath);
        public event EditConfig OnEditConfig;
        public delegate void DeleteConfig(object sender, string sPath);
        public event DeleteConfig OnDeleteConfig;
        public delegate void ToggleFavouriteConfig(object sender, bool bFavourite);
        public event ToggleFavouriteConfig OnToggleFavouriteConfig;
        private string sPath;

        public string Path
        {
            get
            {
                return sPath;
            }
            set
            {
                sPath = value;
            }
        }
        public bool IsFavourite
        {
            get { return (bool)GetValue(IsFavouriteProperty); }
            set { SetValue(IsFavouriteProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsFavourite.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsFavouriteProperty =
            DependencyProperty.Register("IsFavourite", typeof(bool), typeof(ctrlConfig), new PropertyMetadata(false));



        private void btnEditConfig_Click(object sender, RoutedEventArgs e)
        {
            if (OnEditConfig != null)
            {
                OnEditConfig(this, this.Path);
            }
        }

        private void rectMouseOver_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as Rectangle).Opacity = 1;
        }

        private void rectMouseOver_MouseLeave(object sender, MouseEventArgs e)
        {
            (sender as Rectangle).Fill = new SolidColorBrush(Color.FromArgb(77, 195, 195, 195));
            (sender as Rectangle).Opacity = 0;
        }

        private void rectMouseOver_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            (sender as Rectangle).Fill = new SolidColorBrush(Color.FromArgb(77, 30, 30, 30));
        }

        private void rectMouseOver_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as Rectangle).Fill = new SolidColorBrush(Color.FromArgb(77, 195, 195, 195));
            switch((sender as Rectangle).Name)
            {
                case "rectTrashbinMouseOver":
                    if (OnDeleteConfig != null)
                    {
                        OnDeleteConfig(this, this.Path);
                    }
                    break;
                case "rectFavouritesMouseOver":
                    this.IsFavourite = !this.IsFavourite;
                    if (OnToggleFavouriteConfig != null)
                    {
                        OnToggleFavouriteConfig(this, this.IsFavourite);
                    }
                    break;
                default:
                    MessageBox.Show("The clicked Button is not configured in the Switch statement", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }

        private void rectMouseOver_Loaded(object sender, RoutedEventArgs e)
        {
            //Set ToolTip to "Delete "ConfigName""
            (sender as Rectangle).ToolTip = $"Delete \"{(Application.Current.MainWindow as MainWindow).getConfigNameFromPath(Path)}\"";
        }

        private void rectFavouritesMouseOver_Loaded(object sender, RoutedEventArgs e)
        {
            //Set ToolTip
            (sender as Rectangle).ToolTip = $"Add/Remove \"{(Application.Current.MainWindow as MainWindow).getConfigNameFromPath(Path)}\" from your favourites";
        }
    }
}
