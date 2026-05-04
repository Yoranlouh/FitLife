using SharedLibrary.Domain.Entities;
using SharedLibrary.DTOs.Responses;

/// <summary>
/// Maps Arrangement domain entities to response DTOs.
/// </summary>
namespace API.Mappers
{
    public static class ArrangementMapper
    {
        /// <summary>
        /// Maps a collection of arrangements to response DTOs.
        /// </summary>
        public static IReadOnlyList<ArrangementResponse> ToResponses(IEnumerable<Arrangement> arrangements)
        {
            return arrangements.Select(ToResponse).ToList();
        }

        /// <summary>
        /// Maps a single arrangement to a response DTO.
        /// </summary>
        public static ArrangementResponse ToResponse(Arrangement arrangement)
        {
            return new ArrangementResponse
            {
                Id = arrangement.Id,
                Name = arrangement.Name,
                Price = arrangement.Price,
                Items = arrangement.Items.Select(i => new ArrangementItemResponse
                {
                    Type = i.Type.ToString(),
                    Name = i.Name,
                    Quantity = i.Quantity
                }).ToList()
            };
        }
    }
}