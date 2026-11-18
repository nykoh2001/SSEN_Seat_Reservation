namespace SSEN_Seat_Reservation.Services;

public interface IConfirmationService
{
    bool ConfirmReservationCancellation(int seatNumber);
}
