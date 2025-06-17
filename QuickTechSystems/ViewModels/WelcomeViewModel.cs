using System.Threading.Tasks;
using System.Windows;
using QuickTechSystems.Application.Events;
using QuickTechSystems.WPF.ViewModels;

namespace QuickTechSystems.ViewModels.Welcome
{
    public class WelcomeViewModel : ViewModelBase
    {
        private string _companyName = "QuickTech Systems";
        private string _phoneNumber = "Phone nb: 71526575";
        private string _welcomeMessage = "Welcome to QuickTech Systems";
        private string _systemStatus = "System Ready";

        public string CompanyName
        {
            get => _companyName;
            set => SetProperty(ref _companyName, value);
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }

        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public string SystemStatus
        {
            get => _systemStatus;
            set => SetProperty(ref _systemStatus, value);
        }

        public WelcomeViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
        {
            LoadWelcomeData();
        }

        private void LoadWelcomeData()
        {
            var currentUser = System.Windows.Application.Current.Properties["CurrentUser"];
            if (currentUser != null)
            {
                WelcomeMessage = $"Welcome to QuickTech Systems";
                SystemStatus = "System Ready - All Services Online";
            }
        }

        protected override async Task LoadDataAsync()
        {
            await Task.Run(() =>
            {
                LoadWelcomeData();
            });
        }
    }
}