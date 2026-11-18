using System.Windows;

namespace SSEN_Seat_Reservation.Services;

public sealed class MessageBoxConfirmationService : IConfirmationService
{
    public bool ConfirmReservationCancellation(int seatNumber)
    {
        MessageBoxResult result = MessageBox.Show(
            $"{seatNumber}번 좌석 예약을 취소할까요?",
            "예약 취소",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        return result == MessageBoxResult.Yes;
    }
}
