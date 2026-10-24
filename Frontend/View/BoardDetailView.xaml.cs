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
using Frontend.Model;
using Frontend.ViewModel;

namespace Frontend.View
{
    /// <summary>
    /// Interaction logic for BoardDetailView.xaml
    /// </summary>
    public partial class BoardDetailView : Window
    {
        private BoardDetailVM boardDetail;

        internal BoardDetailView(BoardModel board)
        {
            InitializeComponent();
            boardDetail = new BoardDetailVM(board);
            this.DataContext = boardDetail;
        }

        private void Close_Button(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
    }
}
