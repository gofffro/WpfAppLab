using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Button_Click_Dichotomy(object sender, RoutedEventArgs e)
        {
            BisectionMethodWindow objBisectionMethod = new BisectionMethodWindow();
            objBisectionMethod.Closed += BisectionWindow_Closed;
            this.Hide();
            objBisectionMethod.Show();
        }
        private void BisectionWindow_Closed(object sender, EventArgs e)
        {
            this.Show(); 
        }

        private void Button_Click_SLAY(object sender, RoutedEventArgs e)
        {

        }
    }
}