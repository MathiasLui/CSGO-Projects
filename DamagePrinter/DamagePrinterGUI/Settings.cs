using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DamagePrinterGUI
{
    public class Settings : DependencyObject, ICloneable
    {
        public int MinimumDealtDamage
        {
            get { return this.Dispatcher.Invoke(() => (int)GetValue(MinimumDealtDamageProperty)); }
            set { SetValue(MinimumDealtDamageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MinimumDealtDamage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinimumDealtDamageProperty =
            DependencyProperty.Register("MinimumDealtDamage", typeof(int), typeof(Settings), new PropertyMetadata(20));


        public int MinimumReceivedDamage
        {
            get { return this.Dispatcher.Invoke( () => (int)GetValue(MinimumReceivedDamageProperty)); }
            set { SetValue(MinimumReceivedDamageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MinimumReceivedDamage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinimumReceivedDamageProperty =
            DependencyProperty.Register("MinimumReceivedDamage", typeof(int), typeof(Settings), new PropertyMetadata(100));


        public bool PrintDeadPlayers
        {
            get { return this.Dispatcher.Invoke( () => (bool)GetValue(PrintDeadPlayersProperty)); }
            set { SetValue(PrintDeadPlayersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PrintDeadPlayers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PrintDeadPlayersProperty =
            DependencyProperty.Register("PrintDeadPlayers", typeof(bool), typeof(Settings), new PropertyMetadata(false));


        public bool WithholdDuplicateConsoleOutputs
        {
            get { return this.Dispatcher.Invoke( () => (bool)GetValue(WithholdDuplicateConsoleOutputsProperty)); }
            set { SetValue(WithholdDuplicateConsoleOutputsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WithholdDuplicateConsoleOutputs.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WithholdDuplicateConsoleOutputsProperty =
            DependencyProperty.Register("WithholdDuplicateConsoleOutputs", typeof(bool), typeof(Settings), new PropertyMetadata(true));


        public bool PrintAmountOfShots
        {
            get { return this.Dispatcher.Invoke( () => (bool)GetValue(PrintAmountOfShotsProperty)); }
            set { SetValue(PrintAmountOfShotsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PrintAmountOfShots.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PrintAmountOfShotsProperty =
            DependencyProperty.Register("PrintAmountOfShots", typeof(bool), typeof(Settings), new PropertyMetadata(false));


        public bool UseSpecificTerms
        {
            get { return this.Dispatcher.Invoke( () => (bool)GetValue(UseSpecificTermsProperty)); }
            set { SetValue(UseSpecificTermsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UseSpecificTerms.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UseSpecificTermsProperty =
            DependencyProperty.Register("UseSpecificTerms", typeof(bool), typeof(Settings), new PropertyMetadata(true));


        public bool PrintIngameChat
        {
            get { return this.Dispatcher.Invoke( () => (bool)GetValue(PrintIngameChatProperty)); }
            set { SetValue(PrintIngameChatProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PrintIngameChat.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PrintIngameChatProperty =
            DependencyProperty.Register("PrintIngameChat", typeof(bool), typeof(Settings), new PropertyMetadata(true));


        public bool PrintTeamChat
        {
            get { return this.Dispatcher.Invoke( () => (bool)GetValue(PrintTeamChatProperty)); }
            set { SetValue(PrintTeamChatProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PrintTeamChat.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PrintTeamChatProperty =
            DependencyProperty.Register("PrintTeamChat", typeof(bool), typeof(Settings), new PropertyMetadata(true));

        public void ApplySettingsFrom(Settings settings)
        {
            foreach (System.Reflection.PropertyInfo property in typeof(Settings).GetProperties().Where(p => p.CanWrite))
            {
                property.SetValue(this, property.GetValue(settings, null), null);
            }
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
