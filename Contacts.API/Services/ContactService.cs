using Contacts.API.DTOs;
using Contacts.API.Exceptions;
using Contacts.API.Mappers;
using Contacts.API.Models;
using Contacts.API.Repositories.Interfaces;

namespace Contacts.API.Services;

public class ContactService : Services.Interfaces.IContactService
{
    private readonly IContactRepository _repository;

    public ContactService(IContactRepository repository)
    {
        _repository = repository;
    }

    public async Task<ContactResponseDto> CreateAsync(CreateContactDto dto)
    {
        var errors = new List<string>();

        ValidateName(dto.Name, errors);
        ValidateBirthdate(dto.Birthdate, errors);

        if (errors.Count > 0)
            throw new ValidationException(errors);

        var contact = ContactMapper.ToEntity(dto);
        
        await _repository.AddAsync(contact);
        await _repository.SaveChangesAsync();

        return ContactMapper.ToResponseDto(contact);
    }

    public async Task<List<ContactResponseDto>> GetAllAsync(ContactQueryDto query)
    {
        var contacts = await _repository.GetAllActiveAsync(query);
        return contacts.Select(ContactMapper.ToResponseDto).ToList();
    }

    public async Task<ContactResponseDto> GetByIdAsync(Guid id)
    {
        var contact = await _repository.GetByIdAsync(id);
        
        if (contact is null || !contact.IsActive)
            throw new NotFoundException($"Contact with ID {id} not found.");

        return ContactMapper.ToResponseDto(contact);
    }

    public async Task<ContactResponseDto> UpdateAsync(Guid id, UpdateContactDto dto)
    {
        var contact = await _repository.GetActiveByIdAsync(id);
        
        if (contact is null)
            throw new NotFoundException($"Contact with ID {id} not found.");

        var errors = new List<string>();

        if (dto.Name is not null)
            ValidateName(dto.Name, errors);

        if (dto.Birthdate is not null)
            ValidateBirthdate(dto.Birthdate.Value, errors);

        if (errors.Count > 0)
            throw new ValidationException(errors);

        ContactMapper.ApplyUpdate(dto, contact);
        contact.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveChangesAsync();

        return ContactMapper.ToResponseDto(contact);
    }

    public async Task ActivateAsync(Guid id)
    {
        var contact = await _repository.GetByIdAsync(id);
        
        if (contact is null)
            throw new NotFoundException($"Contact with ID {id} not found.");

        if (contact.IsActive)
            throw new ConflictException("Contact is already active.");

        contact.IsActive = true;
        contact.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveChangesAsync();
    }

    public async Task DeactivateAsync(Guid id)
    {
        var contact = await _repository.GetByIdAsync(id);
        
        if (contact is null)
            throw new NotFoundException($"Contact with ID {id} not found.");

        if (!contact.IsActive)
            throw new ConflictException("Contact is already inactive.");

        contact.IsActive = false;
        contact.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(contact);
        await _repository.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var contact = await _repository.GetByIdAsync(id);
        
        if (contact is null)
            throw new NotFoundException($"Contact with ID {id} not found.");

        await _repository.DeleteAsync(contact);
        await _repository.SaveChangesAsync();
    }

    private static void ValidateName(string? name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Name is required.");
    }

    private static void ValidateBirthdate(DateOnly birthdate, List<string> errors)
    {
        if (birthdate > DateOnly.FromDateTime(DateTime.Today))
            errors.Add("Birthdate cannot be in the future.");

        var age = CalculateAge(birthdate);
        
        if (age <= 0)
            errors.Add("Birthdate must result in a valid age.");

        if (age < 18)
            errors.Add("Contact must be at least 18 years old.");
    }

    private static int CalculateAge(DateOnly birthdate)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - birthdate.Year;
        
        if (today < new DateOnly(today.Year, birthdate.Month, birthdate.Day))
            age--;
        
        return age;
    }
}