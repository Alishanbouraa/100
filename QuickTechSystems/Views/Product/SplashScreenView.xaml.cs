// QuickTechSystems.WPF/Views/SplashScreenView.xaml.cs
using System.Windows;
using QuickTechSystems.ViewModels.Login;

namespace QuickTechSystems.WPF.Views
{
    public partial class SplashScreenView : Window
    {
        public SplashScreenView(SplashScreenViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}