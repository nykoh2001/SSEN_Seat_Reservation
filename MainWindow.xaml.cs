using System.Windows;
using SSEN_Seat_Reservation.ViewModels;

namespace SSEN_Seat_Reservation;

public partial class MainWindow : Window
{
    private readonly MainViewModel viewModel;

    public MainWindow()
    {
        InitializeComponent();
        viewModel = new MainViewModel();
        DataContext = viewModel;
    }

    protected override void OnClosed(System.EventArgs e)
    {
        viewModel.Dispose();
        base.OnClosed(e);
    }
}
