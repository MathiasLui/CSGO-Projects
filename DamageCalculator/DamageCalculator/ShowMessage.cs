using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Damage_Calculator
{
    internal static class ShowMessage
    {
        public static void Error(string message) => MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        public static void Warning(string message) => MessageBox.Show(message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        public static void Info(string message) => MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
