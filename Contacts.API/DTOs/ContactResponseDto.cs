using Contacts.API.Types;

namespace Contacts.API.DTOs;

public class ContactResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly Birthdate { get; set; }
    public Gender Gender { get; set; }
    public bool IsActive { get; set; }
    public int Age { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
