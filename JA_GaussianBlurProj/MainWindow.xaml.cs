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
using System.Runtime.InteropServices;
using System.Diagnostics;
using Lab_CS;

namespace JA_GaussianBlurProj
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport(@"C:\Users\StdUser\source\repos\JA_GaussianBlurProj\x64\Debug\LibASM.dll")]
        static extern int MyProc1(int a, int b);

        [DllImport(@"C:\\Users\StdUser\source\repos\JA_GaussianBlurProj\x64\Debug\LibCpp.dll")]
        static extern int MyProcCPP(int a, int b);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int a = 2, b = 3;
                int c = MyProc1(a, b);
                c = MyProcCPP(a, b);
                c = Calculate.MyProc_CS(a, b);
                Debug.WriteLine(c);
            }
            catch (Exception) { }
        }
    }
}
