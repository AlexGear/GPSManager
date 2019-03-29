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

namespace GPSManager
{
    /// <summary>
    /// Логика взаимодействия для PolygonNameDialog.xaml
    /// </summary>
    public partial class PolygonNameDialog : Window
    {
        public string PolygonName { get; private set; }

        public PolygonNameDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Accept();
        }

        private void NameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Accept();
            }
        }

        private void Accept()
        {
            PolygonName = nameTextBox.Text;
            DialogResult = true;
            Close();
        }
    }
}
