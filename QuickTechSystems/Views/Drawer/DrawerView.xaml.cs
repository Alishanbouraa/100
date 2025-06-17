using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using QuickTechSystems.Application.DTOs;
using System.Threading.Tasks;
using QuickTechSystems.ViewModels.Drawer;

namespace QuickTechSystems.WPF.Views
{
    public partial class DrawerView : UserControl
    {
        private DrawerViewModel ViewModel => DataContext as DrawerViewModel;
        private readonly HashSet<string> _asyncOperations;

        public DrawerView()
        {
            InitializeComponent();
            this.Loaded += DrawerView_Loaded;
            _asyncOperations = new HashSet<string>();
        }

        private async void DrawerView_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                await ExecuteAsyncOperation("LoadSessions", () =>
                {
                    ViewModel.LoadDrawerSessionsCommand.Execute(null);
                    return ViewModel.RefreshDrawerDataAsync();
                });

                ViewModel.LoadFinancialDataCommand.Execute(null);
            }
        }

        private void ActionsButton_Click(object sender, RoutedEventArgs e)
        {
            ActionsPopup.IsOpen = true;
        }

        private void OpenDrawerCommand_Execute(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            OpenDrawerPopup.IsOpen = true;
        }

        private void AddCashCommand_Execute(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            AddCashPopup.IsOpen = true;
        }

        private void RemoveCashCommand_Execute(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            RemoveCashPopup.IsOpen = true;
        }

        private async void CloseDrawerCommand_Execute(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();

            if (ViewModel?.CurrentDrawer != null)
            {
                var currentBalance = ViewModel.CurrentDrawer.CurrentBalance;
                var result = MessageBox.Show(
                    $"Current cash in drawer: {currentBalance:C2}\n\nAre you sure you want to close the drawer?",
                    "Close Drawer",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await ExecuteAsyncOperation("CloseDrawer", () =>
                        ViewModel.CloseDrawerWithAmount(currentBalance));
                }
            }
        }

        private void PrintReportCommand_Execute(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            PrintReportPopup.IsOpen = true;
        }

        private void RefreshDataCommand_Execute(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
            ViewModel?.LoadFinancialDataCommand.Execute(null);
        }

        private async void OpenDrawerConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                await ExecuteAsyncOperation("OpenDrawer", () =>
                    ViewModel.OpenDrawerWithAmount(ViewModel.InitialCashAmount));
                OpenDrawerPopup.IsOpen = false;
            }
        }

        private async void AddCashConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                await ExecuteAsyncOperation("AddCash", () =>
                    ViewModel.AddCashWithDetails(ViewModel.CashAmount, ViewModel.CashDescription));
                AddCashPopup.IsOpen = false;
            }
        }

        private async void RemoveCashConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                await ExecuteAsyncOperation("RemoveCash", () =>
                    ViewModel.RemoveCashWithDetails(ViewModel.CashAmount, ViewModel.CashDescription));
                RemoveCashPopup.IsOpen = false;
            }
        }

        private async void CloseDrawerConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                await ExecuteAsyncOperation("CloseDrawerFinal", () =>
                    ViewModel.CloseDrawerWithAmount(ViewModel.FinalCashAmount));
                CloseDrawerPopup.IsOpen = false;
            }
        }

        private async void PrintReportConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                await ExecuteAsyncOperation("PrintReport", () =>
                    ViewModel.PrintReportWithOptions(
                        ViewModel.IncludeTransactionDetails,
                        ViewModel.IncludeFinancialSummary,
                        ViewModel.PrintCashierCopy));
                PrintReportPopup.IsOpen = false;
            }
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.DataContext is DrawerTransactionDTO item)
            {
                var transactionTypes = new HashSet<string> { "cash sale", "return", "expense", "supplier payment" };
                if (transactionTypes.Contains(item.Type?.ToLower()))
                {
                    e.Row.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightPink);
                }
            }
        }

        private void ClosePopup_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPopups();
        }

        private void CloseAllPopups()
        {
            var popups = new[] { ActionsPopup, OpenDrawerPopup, AddCashPopup, RemoveCashPopup, CloseDrawerPopup, PrintReportPopup };
            foreach (var popup in popups)
                popup.IsOpen = false;
        }

        private async Task ExecuteAsyncOperation(string operationName, Func<Task> operation)
        {
            if (_asyncOperations.Contains(operationName))
                return;

            _asyncOperations.Add(operationName);
            try
            {
                await operation();
            }
            finally
            {
                _asyncOperations.Remove(operationName);
            }
        }
    }
}