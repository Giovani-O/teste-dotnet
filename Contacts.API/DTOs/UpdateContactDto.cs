using Contacts.API.Types;

namespace Contacts.API.DTOs;

public class UpdateContactDto
{
    public string? Name { get; set; }
    public DateOnly? Birthdate { get; set; }
    public Gender? Gender { get; set; }
}
