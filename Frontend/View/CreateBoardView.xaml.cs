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

namespace Frontend.View
{
    /// <summary>
    /// Interaction logic for CreateBoardView.xaml
    /// </summary>
    public partial class CreateBoardView : Window
    {
        public CreateBoardView()
        {
            InitializeComponent();
        }

        // Expose the TextBox.Text for callers in BoardView
        public string BoardName => BoardNameTextBox.Text;

        private void Ok_Button(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Cancel_Button(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
