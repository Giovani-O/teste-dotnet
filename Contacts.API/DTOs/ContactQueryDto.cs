using Contacts.API.Types;

namespace Contacts.API.DTOs;

public class ContactQueryDto
{
    public string? Name { get; set; }
    public Gender? Gender { get; set; }
    public DateOnly? BirthdateFrom { get; set; }
    public DateOnly? BirthdateTo { get; set; }
}
