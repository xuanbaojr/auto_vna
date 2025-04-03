using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MyASCS.ViewModels;

namespace MyASCS.Views;

public partial class OnboardingWindow : Window
{
    private ProgressBar _loadingBar;
    public OnboardingWindow()
    {
        InitializeComponent();
        DataContext = new OnboardingWindowViewModel(this);
    }
}