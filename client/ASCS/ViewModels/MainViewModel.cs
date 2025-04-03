using ReactiveUI;

namespace MyASCS.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        public RtspStreamViewModel RtspStreamViewModel { get; }

        public MainViewModel()
        {
            RtspStreamViewModel = new RtspStreamViewModel();
        }
    }
}