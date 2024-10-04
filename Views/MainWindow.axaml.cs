using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ZhoConverterAvaMvvm.ViewModels;

namespace ZhoConverterAvaMvvm.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void TbSource_TextChanged(object? _1, EventArgs _2)
    {
        if (DataContext is MainWindowViewModel viewModel) viewModel.TbSourceTextChanged();
    }

    private void BtnExit_Click(object? _1, RoutedEventArgs _2)
    {
        Close();
    }
}