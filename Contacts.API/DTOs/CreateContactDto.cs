using Contacts.API.Types;

namespace Contacts.API.DTOs;

public class CreateContactDto
{
    public string Name { get; set; } = string.Empty;
    public DateOnly Birthdate { get; set; }
    public Gender Gender { get; set; }
}
