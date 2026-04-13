using Contacts.API.Types;

namespace Contacts.API.Models;

public class Contact
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly Birthdate { get; set; }
    public Gender Gender { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Calculated — not persisted
    public int Age =>
        DateTime.Today.Year - Birthdate.Year -
        (DateTime.Today < new DateTime(DateTime.Today.Year, Birthdate.Month, Birthdate.Day) ? 1 : 0);
}
