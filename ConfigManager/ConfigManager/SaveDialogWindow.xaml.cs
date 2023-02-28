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
using System.Windows.Shapes;

namespace ConfigManager
{
    /// <summary>
    /// Interaction logic for SaveDialogWindow.xaml
    /// </summary>
    public partial class SaveDialogWindow : Window
    {
        public SaveDialogWindow()
        {
            InitializeComponent();
        }

        bool? m_bReturnValue;
        public bool? ReturnValue
        {
            get { return m_bReturnValue; }
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            m_bReturnValue = true;
            DialogResult = true;
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            m_bReturnValue = false;
            DialogResult = false;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            m_bReturnValue = false;
            m_bReturnValue = null;
            this.Close();
        }

        private void lblCloseWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            m_bReturnValue = false;
            m_bReturnValue = null;
            this.Close();
        }
    }
}
