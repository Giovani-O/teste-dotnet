using Contacts.API.Data;
using Contacts.API.DTOs;
using Contacts.API.Models;
using Contacts.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Contacts.API.Repositories;

public class ContactRepository : IContactRepository
{
    private readonly AppDbContext _db;

    public ContactRepository(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<Contact?> GetByIdAsync(Guid id)
    {
        return await _db.Contacts
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <inheritdoc/>
    public async Task<Contact?> GetActiveByIdAsync(Guid id)
    {
        return await _db.Contacts
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
    }

    /// <inheritdoc/>
    public async Task<List<Contact>> GetAllActiveAsync(ContactQueryDto query)
    {
        IQueryable<Contact> q = _db.Contacts
            .AsNoTracking()
            .Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Name))
            q = q.Where(c => c.Name.Contains(query.Name));

        if (query.Gender.HasValue)
            q = q.Where(c => c.Gender == query.Gender.Value);

        if (query.BirthdateFrom.HasValue)
            q = q.Where(c => c.Birthdate >= query.BirthdateFrom.Value);

        if (query.BirthdateTo.HasValue)
            q = q.Where(c => c.Birthdate <= query.BirthdateTo.Value);

        return await q.ToListAsync();
    }

    /// <inheritdoc/>
    public async Task AddAsync(Contact contact)
    {
        await _db.Contacts.AddAsync(contact);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(Contact contact)
    {
        _db.Contacts.Update(contact);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteAsync(Contact contact)
    {
        _db.Contacts.Remove(contact);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
