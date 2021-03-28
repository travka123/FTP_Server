using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FTP_Server
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Logger.SetTextBlock(tbLog);
            AccountManager.Add("admin", "admin");
        }

        private void tbCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if ((sender as TextBox).Text.Length > 0)
                {
                    FTPServer.ExecuteServerCommand((sender as TextBox).Text);
                    (sender as TextBox).Clear();
                }
            }
        }
    }
}
