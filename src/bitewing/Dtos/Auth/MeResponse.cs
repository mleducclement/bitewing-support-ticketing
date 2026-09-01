using Microsoft.AspNetCore.Identity;

namespace bitewing.Dtos.Auth;

public record MeResponse(string Email, string FirstName, string LastName, IList<string> Roles, string Id);