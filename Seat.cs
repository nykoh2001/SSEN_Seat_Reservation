using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace SSEN_Seat_Reservation
{
    // 좌석 상태: 빈자리 / 예약중 / 이용중
    public enum SeatStatus
    {
        Available,
        Reserved,
        InUse
    }

    // 좌석 한 개를 나타내는 클래스
    // INotifyPropertyChanged 를 구현해서 값이 바뀌면 화면이 자동으로 갱신됨
    public class Seat : INotifyPropertyChanged
    {
        // 예약 후 이 시간 안에 입실하지 않으면 예약이 자동 취소됨
        public static readonly TimeSpan ReserveLimit = TimeSpan.FromMinutes(10);

        public int Number { get; set; }

        private SeatStatus _status = SeatStatus.Available;
        public SeatStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(TimeText));
            }
        }

        private string _userName = "";
        public string UserName
        {
            get => _userName;
            set { _userName = value; OnPropertyChanged(); }
        }

        public DateTime? ReservedAt { get; set; }   // 예약한 시각
        public DateTime? StartedAt { get; set; }    // 입실한 시각

        // ---- 아래는 화면 표시용 (파일에 저장하지 않음) ----

        private bool _isSelected;
        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public string StatusText => Status switch
        {
            SeatStatus.Reserved => "예약중",
            SeatStatus.InUse => "이용중",
            _ => "빈자리"
        };

        [JsonIgnore]
        public string TimeText
        {
            get
            {
                if (Status == SeatStatus.Reserved && ReservedAt.HasValue)
                {
                    TimeSpan left = ReserveLimit - (DateTime.Now - ReservedAt.Value);
                    if (left < TimeSpan.Zero) left = TimeSpan.Zero;
                    return $"입실까지 {left:mm\\:ss}";
                }
                if (Status == SeatStatus.InUse && StartedAt.HasValue)
                {
                    TimeSpan used = DateTime.Now - StartedAt.Value;
                    return $"이용 {used:hh\\:mm\\:ss}";
                }
                return "";
            }
        }

        // 예약 시간이 지났는지
        [JsonIgnore]
        public bool IsReservationExpired =>
            Status == SeatStatus.Reserved &&
            ReservedAt.HasValue &&
            DateTime.Now - ReservedAt.Value > ReserveLimit;

        // ---- 동작 ----

        public void Reserve(string name)
        {
            UserName = name;
            ReservedAt = DateTime.Now;
            StartedAt = null;
            Status = SeatStatus.Reserved;
        }

        public void CheckIn(string name)
        {
            UserName = name;
            StartedAt = DateTime.Now;
            ReservedAt = null;
            Status = SeatStatus.InUse;
        }

        public void Clear()
        {
            UserName = "";
            ReservedAt = null;
            StartedAt = null;
            Status = SeatStatus.Available;
        }

        // 타이머가 1초마다 호출 → 남은 시간/이용 시간 글자 갱신
        public void RefreshTime() => OnPropertyChanged(nameof(TimeText));

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
