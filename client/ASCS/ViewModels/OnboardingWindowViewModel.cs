using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using MyASCS.Views;

namespace MyASCS.ViewModels;

public class OnboardingWindowViewModel
{
    private readonly Window _window;
    private readonly ProgressBar _loadingBar;

    public OnboardingWindowViewModel(Window window)
    {
        _window = window;
        _loadingBar = _window.FindControl<ProgressBar>("LoadingBar")!;
        StartOnboarding();
    }

    private async void StartOnboarding()
    {
        // Animate progress bar from 0% to 100%
        for (var i = 0; i <= 100; i++)
        {
            Dispatcher.UIThread.Post(() => _loadingBar.Value = i);
            await Task.Delay(10);
        }
        
        await Task.Delay(100); // Wait for 1.5 seconds

        // Open IntroductionWindow after loading completes
        Dispatcher.UIThread.Post(() =>
        {
            var mainWindow = new IntroductionWindow();
            mainWindow.Show();
            _window.Close();
        });
    }
}