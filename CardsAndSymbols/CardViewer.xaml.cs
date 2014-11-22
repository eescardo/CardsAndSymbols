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
    /// Interaction logic for CardViewer.xaml
    /// </summary>
    public partial class CardViewer : UserControl
    {
        public static DependencyProperty CardDataProperty = DependencyProperty.Register(
            "CardData",
            typeof(CardData),
            typeof(CardViewer),
            new PropertyMetadata());

        public CardViewer()
        {
            this.InitializeComponent();
        }

        public CardData CardData
        {
            get
            {
                return (CardData)this.GetValue(CardDataProperty);
            }

            set
            {
                this.SetValue(CardDataProperty, value);
            }
        }
    }
}
