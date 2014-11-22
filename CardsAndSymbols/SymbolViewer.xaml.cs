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

namespace CardsAndSymbols
{
    /// <summary>
    /// Interaction logic for Symbol.xaml
    /// </summary>
    public partial class SymbolViewer : UserControl
    {
        public static DependencyProperty SymbolDataProperty = DependencyProperty.Register(
            "SymbolData",
            typeof(SymbolData),
            typeof(SymbolViewer),
            new PropertyMetadata());

        public SymbolViewer()
        {
            this.InitializeComponent();
        }

        public SymbolData SymbolData
        {
            get
            {
                return (SymbolData)this.GetValue(SymbolDataProperty);
            }

            set
            {
                this.SetValue(SymbolDataProperty, value);
            }
        }
    }
}
