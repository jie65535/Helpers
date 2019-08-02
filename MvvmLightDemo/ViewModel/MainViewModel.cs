using System.Windows.Input;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace MvvmLightDemo.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private string _Title;

        public string Title
        {
            get => _Title;
            set => Set(ref _Title, value);
        }

        public ICommand ChangeTitleCommand { get; set; }

        public MainViewModel()
        {
            Title = "Hello World!";
            ChangeTitleCommand = new RelayCommand(ChangeTitle);
        }

        private void ChangeTitle()
        {
            Title = "Hello MvvmLight!";
        }
    }
}