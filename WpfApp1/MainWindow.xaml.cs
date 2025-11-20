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
using WpfApp1.OlimpSort;
using WpfApp1.SLAY;

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

        private void Window_Closed(object sender, EventArgs e)
        {
            this.Show();
        }

        private void Button_Click_Dichotomy(object sender, RoutedEventArgs e)
        {
            BisectionMethodWindow objBisectionMethod = new BisectionMethodWindow();
            objBisectionMethod.Closed += Window_Closed;
            this.Hide();
            objBisectionMethod.Show();
        }

        private void Button_Click_SLAY(object sender, RoutedEventArgs e)
        {
            SlayWindow objSlayMethod = new SlayWindow();
            objSlayMethod.Closed += Window_Closed;
            this.Hide();
            objSlayMethod.Show();
        }

        private void Button_Click_Golden(object sender, RoutedEventArgs e)
        {
            GoldenRatio objGoldenMethod = new GoldenRatio();
            objGoldenMethod.Closed += Window_Closed;
            this.Hide();
            objGoldenMethod.Show();
        }

        private void Button_Click_Newton(object sender, RoutedEventArgs e)
        {
            NewtonMethodWindow objNewtonMethod = new NewtonMethodWindow();
            objNewtonMethod.Closed += Window_Closed;
            this.Hide();
            objNewtonMethod.Show();
        }

        private void Button_Click_OlimpSort(object sender, RoutedEventArgs e)
        {
            OlimpSortWindow objOlimpSort = new OlimpSortWindow();
            objOlimpSort.Closed += Window_Closed;
            this.Hide();
            objOlimpSort.Show();
        }
    }
}