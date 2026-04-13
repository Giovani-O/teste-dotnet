using Contacts.API.DTOs;
using Contacts.API.Models;

namespace Contacts.API.Mappers;

public static class ContactMapper
{
    public static Contact ToEntity(CreateContactDto dto) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Birthdate = dto.Birthdate,
            Gender = dto.Gender,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    public static ContactResponseDto ToResponseDto(Contact entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Birthdate = entity.Birthdate,
            Gender = entity.Gender,
            IsActive = entity.IsActive,
            Age = entity.Age,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };

    public static void ApplyUpdate(UpdateContactDto dto, Contact entity)
    {
        if (dto.Name is not null)
            entity.Name = dto.Name;

        if (dto.Birthdate is not null)
            entity.Birthdate = dto.Birthdate.Value;

        if (dto.Gender is not null)
            entity.Gender = dto.Gender.Value;
    }
}
