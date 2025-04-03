using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MyASCS.Services;
using MyASCS.Services.Implementations;
using MyASCS.ViewModels;

namespace MyASCS.Views;

public partial class IntroductionWindow : Window
{
    public IntroductionWindow()
    {
        InitializeComponent();
        // Subscribe to the Click event
        StartButton.Click += OnStartIdentifyingClick;
    }
    
    private void OnStartIdentifyingClick(object? sender, EventArgs e)
    {
        // Get MainViewModel from ServiceLocator
        var mainViewModel = ServiceLocator.GetService<MainViewModel>();
        
        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };
        mainWindow.Show(); // Open main window
        this.Close(); // Close the welcome screen
    }
}