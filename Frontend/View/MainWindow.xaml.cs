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
using Frontend.ViewModel;


namespace Frontend.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowVM userInfo;

        public MainWindow()
        {
            InitializeComponent();
            this.userInfo = new MainWindowVM();
            this.DataContext = userInfo;
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var user = userInfo.Login(); // Call the login method in the ViewModel

            if (user != null)
            {
                BoardView boardView = new(user);
                boardView.Show(); // Open the board view for the logged-in user
                this.Close(); // Close the login window
            }
            else
            {
                FeedbackText.Text = "Login failed, invalid mail or password or try again later.";
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            var user = userInfo.Register();

            if (user != null)
            {
                BoardView boardView = new(user);
                boardView.Show();
                this.Close();
            }
            else
            {
                FeedbackText.Text = "Registration failed.";
            }
        }
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) // TODO: change this to a better way of handling password input with binding
        {
            if (this.DataContext != null)
            {
                ((MainWindowVM)this.DataContext).Password = ((PasswordBox)sender).Password;
            }
        }
    }
}

