using SharedLibrary.Domain.Entities;
using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;

namespace API.Mappers
{
    public static class InstructorMapper
    {
        public static InstructorResponse ToResponse(Instructor instructor)
        {
            return new InstructorResponse
            {
                Id = instructor.Id,
                FirstName = instructor.FirstName,
                LastName = instructor.LastName,
                Email = instructor.Email,
                Specialization = instructor.Specialization,
                PhotoUrl = instructor.Photo?.Url
            };
        }

        public static IEnumerable<InstructorResponse> ToResponses(IEnumerable<Instructor> instructors)
        {
            return instructors.Select(ToResponse);
        }

        public static Instructor ToEntity(InstructorCreateRequest request)
        {
            return new Instructor
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Specialization = request.Specialization
            };
        }

        public static void UpdateEntity(Instructor instructor, InstructorUpdateRequest request)
        {
            instructor.FirstName = request.FirstName;
            instructor.LastName = request.LastName;
            instructor.Email = request.Email;
            instructor.Specialization = request.Specialization;
        }
    }
}
