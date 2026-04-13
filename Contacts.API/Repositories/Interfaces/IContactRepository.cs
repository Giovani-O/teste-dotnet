using Contacts.API.DTOs;
using Contacts.API.Models;

namespace Contacts.API.Repositories.Interfaces;

public interface IContactRepository
{
    /// <summary>Returns ANY contact by Id regardless of active status. Null if not found.</summary>
    Task<Contact?> GetByIdAsync(Guid id);

    /// <summary>Returns only an ACTIVE contact by Id. Null if not found or inactive.</summary>
    Task<Contact?> GetActiveByIdAsync(Guid id);

    /// <summary>Returns all active contacts, filtered by non-null fields in query.</summary>
    Task<List<Contact>> GetAllActiveAsync(ContactQueryDto query);

    /// <summary>Persists a new contact (not yet saved — caller must call SaveChangesAsync).</summary>
    Task AddAsync(Contact contact);

    /// <summary>Marks a tracked contact as modified (not yet saved — caller must call SaveChangesAsync).</summary>
    Task UpdateAsync(Contact contact);

    /// <summary>Removes a contact (not yet saved — caller must call SaveChangesAsync).</summary>
    Task DeleteAsync(Contact contact);

    /// <summary>Flushes pending changes to the database.</summary>
    Task SaveChangesAsync();
}
