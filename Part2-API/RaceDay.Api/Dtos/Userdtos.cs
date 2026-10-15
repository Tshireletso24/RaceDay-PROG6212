using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>Fields a user can change on their own profile.</summary>
public class UpdateProfileRequest
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(500)]
    public string? ProfilePictureUrl { get; set; }
}