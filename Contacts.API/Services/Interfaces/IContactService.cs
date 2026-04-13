using Contacts.API.DTOs;

namespace Contacts.API.Services.Interfaces;

public interface IContactService
{
    Task<ContactResponseDto> CreateAsync(CreateContactDto dto);
    Task<List<ContactResponseDto>> GetAllAsync(ContactQueryDto query);
    Task<ContactResponseDto> GetByIdAsync(Guid id);
    Task<ContactResponseDto> UpdateAsync(Guid id, UpdateContactDto dto);
    Task ActivateAsync(Guid id);
    Task DeactivateAsync(Guid id);
    Task DeleteAsync(Guid id);
}