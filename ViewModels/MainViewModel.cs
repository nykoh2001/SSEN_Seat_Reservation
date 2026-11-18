using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using SSEN_Seat_Reservation.Services;

namespace SSEN_Seat_Reservation.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private const int SeatCount = 31;
    private readonly SeatStorageService storage;
    private readonly IConfirmationService confirmationService;
    private readonly DispatcherTimer timer;
    private Seat? selectedSeat;
    private string name = string.Empty;
    private string statusMessage = string.Empty;

    public ObservableCollection<Seat> Row1Section1 { get; } = [];
    public ObservableCollection<Seat> Row1Section2 { get; } = [];
    public ObservableCollection<Seat> Row1Section3 { get; } = [];
    public ObservableCollection<Seat> Row2Section1 { get; } = [];
    public ObservableCollection<Seat> Row2Section2 { get; } = [];
    public ObservableCollection<Seat> Row2Section3 { get; } = [];
    public ObservableCollection<Seat> Row3Section1 { get; } = [];
    public ObservableCollection<Seat> Row3Section2 { get; } = [];
    public ObservableCollection<Seat> Row3Section3 { get; } = [];
    public ObservableCollection<Seat> Row4Section1 { get; } = [];
    public ObservableCollection<Seat> Row4Section2 { get; } = [];
    public ObservableCollection<Seat> Row5Section1 { get; } = [];
    public ObservableCollection<Seat> Row5Section2 { get; } = [];

    public RelayCommand SelectSeatCommand { get; }
    public RelayCommand ReserveCommand { get; }
    public RelayCommand CheckInCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand CheckOutCommand { get; }

    public Seat? SelectedSeat
    {
        get => selectedSeat;
        private set
        {
            if (selectedSeat == value) return;
            if (selectedSeat is not null)
            {
                selectedSeat.IsSelected = false;
                selectedSeat.PropertyChanged -= SelectedSeat_PropertyChanged;
            }

            selectedSeat = value;
            if (selectedSeat is not null)
            {
                selectedSeat.IsSelected = true;
                selectedSeat.PropertyChanged += SelectedSeat_PropertyChanged;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedSeatText));
            OnPropertyChanged(nameof(SelectedSeatDetails));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string Name
    {
        get => name;
        set
        {
            if (name == value) return;
            name = value;
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set
        {
            if (statusMessage == value) return;
            statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string SeatCountsText =>
        $"빈자리 {AllSeats.Count(seat => seat.Status == SeatStatus.Available)} · " +
        $"예약중 {AllSeats.Count(seat => seat.Status == SeatStatus.Reserved)} · " +
        $"이용중 {AllSeats.Count(seat => seat.Status == SeatStatus.InUse)}";

    public string SelectedSeatText => SelectedSeat is null
        ? "좌석을 선택하세요"
        : $"{SelectedSeat.Number}번 좌석 · {SelectedSeat.StatusText}" +
          (SelectedSeat.Status == SeatStatus.Available ? string.Empty : $"\n사용자: {SelectedSeat.UserName}");

    public string SelectedSeatDetails => SelectedSeat?.TimeText ?? string.Empty;

    private Seat[] AllSeats { get; }

    public MainViewModel() : this(new SeatStorageService(), new MessageBoxConfirmationService()) { }

    public MainViewModel(SeatStorageService storage, IConfirmationService confirmationService)
    {
        this.storage = storage;
        this.confirmationService = confirmationService;
        AllSeats = storage.Load(SeatCount).ToArray();

        Fill(Row1Section1, 0, 2);
        Fill(Row1Section2, 2, 2);
        Fill(Row1Section3, 4, 3);
        Fill(Row2Section1, 7, 2);
        Fill(Row2Section2, 9, 2);
        Fill(Row2Section3, 11, 3);
        Fill(Row3Section1, 14, 2);
        Fill(Row3Section2, 16, 2);
        Fill(Row3Section3, 18, 3);
        Fill(Row4Section1, 21, 2);
        Fill(Row4Section2, 23, 3);
        Fill(Row5Section1, 26, 2);
        Fill(Row5Section2, 28, 3);

        foreach (Seat seat in AllSeats)
            seat.PropertyChanged += Seat_PropertyChanged;

        SelectSeatCommand = new RelayCommand(SelectSeat);
        ReserveCommand = new RelayCommand(_ => Reserve(), _ => CanReserve);
        CheckInCommand = new RelayCommand(_ => CheckIn(), _ => CanCheckIn);
        CancelCommand = new RelayCommand(_ => Cancel(), _ => SelectedSeat?.Status == SeatStatus.Reserved);
        CheckOutCommand = new RelayCommand(_ => CheckOut(), _ => SelectedSeat?.Status == SeatStatus.InUse);

        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private bool CanReserve => SelectedSeat?.Status == SeatStatus.Available && !string.IsNullOrWhiteSpace(Name);
    private bool CanCheckIn => SelectedSeat is not null &&
        (SelectedSeat.Status == SeatStatus.Reserved ||
         (SelectedSeat.Status == SeatStatus.Available && !string.IsNullOrWhiteSpace(Name)));

    private void Fill(ObservableCollection<Seat> target, int start, int count)
    {
        foreach (Seat seat in AllSeats.Skip(start).Take(count))
            target.Add(seat);
    }

    private void SelectSeat(object? parameter)
    {
        if (parameter is not Seat seat) return;
        SelectedSeat = seat;
        Name = seat.Status == SeatStatus.Reserved ? seat.UserName : string.Empty;
        StatusMessage = string.Empty;
    }

    private void Reserve()
    {
        if (SelectedSeat is null) return;
        if (!CanUseName(Name)) return;

        SelectedSeat.Reserve(Name.Trim());
        SaveSeats();
        StatusMessage = $"{SelectedSeat.Number}번 좌석을 예약했습니다. 10분 안에 입실해 주세요.";
    }

    private void CheckIn()
    {
        if (SelectedSeat is null) return;

        if (SelectedSeat.Status == SeatStatus.Reserved && Name != SelectedSeat.UserName)
        {
            StatusMessage = "예약자 이름과 일치하지 않습니다.";
            return;
        }

        if (SelectedSeat.Status == SeatStatus.Available && !CanUseName(Name)) return;
        if (SelectedSeat.Status == SeatStatus.InUse) return;

        SelectedSeat.CheckIn(Name.Trim());
        SaveSeats();
        StatusMessage = $"{SelectedSeat.Number}번 좌석에 입실했습니다.";
    }

    private void Cancel()
    {
        if (SelectedSeat?.Status != SeatStatus.Reserved) return;
        int number = SelectedSeat.Number;
        if (!confirmationService.ConfirmReservationCancellation(number)) return;

        SelectedSeat.Clear();
        SaveSeats();
        StatusMessage = $"{number}번 좌석 예약을 취소했습니다.";
    }

    private void CheckOut()
    {
        if (SelectedSeat?.Status != SeatStatus.InUse || SelectedSeat.StartedAt is null) return;
        TimeSpan used = DateTime.Now - SelectedSeat.StartedAt.Value;
        int number = SelectedSeat.Number;
        SelectedSeat.Clear();
        SaveSeats();
        StatusMessage = $"{number}번 좌석 퇴실을 처리했습니다. 이용 시간: {(int)used.TotalHours}시간 {used.Minutes}분";
    }

    private bool CanUseName(string value)
    {
        string trimmedName = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            StatusMessage = "이름을 입력하세요.";
            return false;
        }

        Seat? otherSeat = AllSeats.FirstOrDefault(seat =>
            seat != SelectedSeat && seat.Status != SeatStatus.Available && seat.UserName == trimmedName);
        if (otherSeat is not null)
        {
            StatusMessage = $"{trimmedName}님은 이미 {otherSeat.Number}번 좌석을 사용 중입니다.";
            return false;
        }

        return true;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        bool changed = false;
        foreach (Seat seat in AllSeats)
        {
            if (seat.IsReservationExpired)
            {
                seat.Clear();
                changed = true;
            }
            seat.RefreshTime();
        }

        if (SelectedSeat is not null)
            OnPropertyChanged(nameof(SelectedSeatDetails));

        if (changed)
        {
            SaveSeats();
            StatusMessage = "입실 시간이 지난 예약을 자동 취소했습니다.";
        }
    }

    private void Seat_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Seat.Status))
        {
            OnPropertyChanged(nameof(SeatCountsText));
            OnPropertyChanged(nameof(SelectedSeatText));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void SelectedSeat_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Seat.Status) or nameof(Seat.UserName) or nameof(Seat.TimeText))
        {
            OnPropertyChanged(nameof(SelectedSeatText));
            OnPropertyChanged(nameof(SelectedSeatDetails));
        }
    }

    private void SaveSeats() => storage.Save(AllSeats);

    public void Dispose()
    {
        timer.Stop();
        timer.Tick -= Timer_Tick;
        foreach (Seat seat in AllSeats)
            seat.PropertyChanged -= Seat_PropertyChanged;
        if (SelectedSeat is not null)
            SelectedSeat.PropertyChanged -= SelectedSeat_PropertyChanged;
    }
}
