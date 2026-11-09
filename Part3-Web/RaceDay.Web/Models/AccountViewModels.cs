using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That doesn't look like a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(150)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That doesn't look like a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Phone(ErrorMessage = "That doesn't look like a valid phone number.")]
    [Display(Name = "Phone number (optional)")]
    public string? PhoneNumber { get; set; }

    [Required]
    public string Role { get; set; } = "Participant";

    [Display(Name = "Organiser invite code")]
    public string? OrganiserCode { get; set; }
}
