using API.Repositories.Interfaces;
using API.Services;
using API.Services.Implementations;
using API.Services.Interfaces;
using SharedLibrary.Domain.Entities;
using SharedLibrary.DTOs.Models;
using SharedLibrary.Logic.Algorithm;
using System.Threading.Tasks;

namespace API.Mappers
{
    public static class ShowingMapper
    {
        // Synchronous wrapper for compatibility
        public static ShowingStateDto ToStateDto(this Showing showing, IReservationRepository reservationRepository)
        {
            return ToStateDtoAsync(showing, reservationRepository).GetAwaiter().GetResult();
        }

        // Async implementation that properly awaits the repository call
        public static async Task<ShowingStateDto> ToStateDtoAsync(this Showing showing, IReservationRepository reservationRepository)
        {
            var rows = showing.GetLayoutSnapshot();
            var allSeats = ZoneCalculator.BuildSeatMap(rows);
            var occupied = await reservationRepository.GetOccupiedKeysAsync(showing.Id) ?? new HashSet<string>();


            return new ShowingStateDto(
                showing,
                allSeats,
                occupied
            );
        }
    }

}
