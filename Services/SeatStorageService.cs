using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SSEN_Seat_Reservation;

namespace SSEN_Seat_Reservation.Services;

public sealed class SeatStorageService
{
    private readonly string filePath = Path.Combine(AppContext.BaseDirectory, "seats.json");

    public List<Seat> Load(int seatCount)
    {
        try
        {
            if (File.Exists(filePath))
            {
                List<Seat>? savedSeats = JsonSerializer.Deserialize<List<Seat>>(File.ReadAllText(filePath));
                if (savedSeats is { Count: > 0 } && savedSeats.Count <= seatCount)
                {
                    while (savedSeats.Count < seatCount)
                        savedSeats.Add(new Seat { Number = savedSeats.Count + 1 });

                    return savedSeats;
                }
            }
        }
        catch (JsonException)
        {
            // 손상된 저장 파일은 새 좌석 목록으로 대체합니다.
        }
        catch (IOException)
        {
            // 저장 파일을 읽을 수 없으면 새 좌석 목록으로 시작합니다.
        }

        return Enumerable.Range(1, seatCount)
            .Select(number => new Seat { Number = number })
            .ToList();
    }

    public void Save(IEnumerable<Seat> seats)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(filePath, JsonSerializer.Serialize(seats, options));
    }
}
