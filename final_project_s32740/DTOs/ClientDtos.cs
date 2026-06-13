using System.ComponentModel.DataAnnotations;

namespace final_project_s32740.Dtos;

public class CreateIndividualClientDto
{
    [Required] public string FirstName { get; set; } = string.Empty;
    [Required] public string LastName { get; set; } = string.Empty;
    [Required] public string Address { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Phone { get; set; } = string.Empty;
    [Required, Length(11, 11)] public string PESEL { get; set; } = string.Empty;
}

public class CreateCorporateClientDto
{
    [Required] public string CompanyName { get; set; } = string.Empty;
    [Required] public string Address { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Phone { get; set; } = string.Empty;
    [Required] public string KRS { get; set; } = string.Empty;
}

public class UpdateIndividualClientDto
{
    [Required] public string FirstName { get; set; } = string.Empty;
    [Required] public string LastName { get; set; } = string.Empty;
    [Required] public string Address { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Phone { get; set; } = string.Empty;
}

public class UpdateCorporateClientDto
{
    [Required] public string CompanyName { get; set; } = string.Empty;
    [Required] public string Address { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Phone { get; set; } = string.Empty;
}

public class IndividualClientResponseDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PESEL { get; set; } = string.Empty;
}

public class CorporateClientResponseDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string KRS { get; set; } = string.Empty;
}