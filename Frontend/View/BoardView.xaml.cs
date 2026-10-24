using Backend.ServiceLayer;
using Frontend.Model;
using Frontend.ViewModel;
using IntroSE.Kanban.Backend.ServiceLayer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity.Migrations.Model;
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
    /// Interaction logic for BoardView.xaml
    /// </summary>
    public partial class BoardView : Window
    {
        private BoardViewModel boardViewModel;
        private UserModel currentUser;
       
        internal BoardView(UserModel user)
        {
            InitializeComponent();
            boardViewModel = new BoardViewModel(user);
            this.DataContext = boardViewModel;
            this.currentUser = user;
        }

        private void Create_Board_Button(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new CreateBoardView
                {
                    Owner = this
                };
                bool? result = dlg.ShowDialog(); // Show the dialog and wait for user input, OK = true, Cancel = false
                
                if (result == true)
                {
                    string newName = dlg.BoardName?.Trim();
                    boardViewModel.CreateBoard(newName);
                    
                    FeedbackText.Text = $"Board \"{newName}\" created successfully.";
                }
                else
                {
                    FeedbackText.Text = "Board creation cancelled.";
                }
            }
            catch (Exception ex)
            {
                FeedbackText.Text = $"Failed to create board: {ex.Message}";
            }
        }

        private void Delete_Board_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var board = (BoardModel)button.DataContext;
            if (board == null) return;

            // Ask the user to confirm, mentioning the board name
            var msg = $"Are you sure you want to delete board \"{board.BoardName}\"?";
            var result = MessageBox.Show(
                this,                    // owner window
                msg,
                "Confirm Delete",        // window title
                MessageBoxButton.YesNo,  // Yes = delete, No = cancel
                MessageBoxImage.Warning  // a warning icon
            );

            if (result == MessageBoxResult.Yes)
            {
                boardViewModel.DeleteBoard(board);
                FeedbackText.Text = $"Board \"{board.BoardName}\" deleted.";
            }
        }

        private void Logout_Button(object sender, RoutedEventArgs e)
        {
            try
            {
                boardViewModel.CurrentUser.Controller.Logout(boardViewModel.CurrentUser.Email); // backend log out
                // Return to login window
                MainWindow loginWindow = new MainWindow();
                loginWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                FeedbackText.Text = $"Logout failed: \" {ex.Message} \"";
            }
        }

        private void View_Board_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((sender as Button)?.DataContext is BoardModel selectedBoard)
                {
                    // Open detail view and pass the selected board model
                    BoardDetailView detailView = new(selectedBoard);
                    detailView.Owner = this;
                    detailView.ShowDialog(); // Model window
                }
                else
                {
                    FeedbackText.Text = "Unable to view board details. Try again.";
                }
            }
            catch(Exception ex)
            {
                FeedbackText.Text = $"Error viewing board: {ex.Message}";
            }
        }
    }
}
