using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using QuickTechSystems.WPF.Views;

namespace QuickTechSystems.WPF.Services
{
    public interface IDialogService
    {
        Task<(bool Confirmed, decimal Value)> ShowCashAmountDialogAsync(string title, string prompt, Window owner = null);
        Task<(bool Confirmed, string Value)> ShowTextInputDialogAsync(string title, string prompt, Window owner = null);
        Task<(bool Confirmed, decimal Value, string Notes)> ShowCashAmountWithNotesDialogAsync(string title, string prompt, Window owner = null);
    }

    public class DialogService : IDialogService
    {
        private readonly IWindowService _windowService;

        public DialogService(IWindowService windowService)
        {
            _windowService = windowService;
        }

        public async Task<(bool Confirmed, decimal Value)> ShowCashAmountDialogAsync(string title, string prompt, Window owner = null)
        {
            var taskCompletionSource = new TaskCompletionSource<(bool Confirmed, decimal Value)>();

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                owner ??= _windowService.GetCurrentWindow();
                var dialog = new InputDialog(title, prompt)
                {
                    Owner = owner
                };

                var result = dialog.ShowDialog();

                if (result == true && decimal.TryParse(dialog.Input, out decimal amount))
                {
                    taskCompletionSource.SetResult((true, amount));
                }
                else
                {
                    taskCompletionSource.SetResult((false, 0));
                }
            });

            return await taskCompletionSource.Task;
        }

    

        public async Task<(bool Confirmed, string Value)> ShowTextInputDialogAsync(string title, string prompt, Window owner = null)
        {
            var taskCompletionSource = new TaskCompletionSource<(bool Confirmed, string Value)>();

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                owner ??= _windowService.GetCurrentWindow();
                var dialog = new InputDialog(title, prompt)
                {
                    Owner = owner
                };

                var result = dialog.ShowDialog();

                if (result == true)
                {
                    taskCompletionSource.SetResult((true, dialog.Input));
                }
                else
                {
                    taskCompletionSource.SetResult((false, string.Empty));
                }
            });

            return await taskCompletionSource.Task;
        }

        public async Task<(bool Confirmed, decimal Value, string Notes)> ShowCashAmountWithNotesDialogAsync(
            string title, string prompt, Window owner = null)
        {
            var amountResult = await ShowCashAmountDialogAsync(title, prompt, owner);

            if (!amountResult.Confirmed)
                return (false, 0, string.Empty);

            var notesResult = await ShowTextInputDialogAsync("Notes", "Enter any notes:", owner);

            return (true, amountResult.Value, notesResult.Value);
        }
    }
}