using Contacts.API.DTOs;
using Contacts.API.Exceptions;
using Contacts.API.Models;
using Contacts.API.Repositories.Interfaces;
using Contacts.API.Services;
using Contacts.API.Types;
using NSubstitute;

namespace Contacts.Tests.Services;

public class ContactServiceTests
{
    protected readonly IContactRepository _repository;
    protected readonly ContactService _service;

    public ContactServiceTests()
    {
        _repository = Substitute.For<IContactRepository>();
        _service = new ContactService(_repository);
    }

    public class CreateAsync : ContactServiceTests
    {
        [Fact]
        public async Task CreateAsync_ShouldReturnContact_WhenValidInput()
        {
            var dto = new CreateContactDto
            {
                Name = "John Doe",
                Birthdate = new DateOnly(1990, 5, 15),
                Gender = Gender.Male
            };

            _repository.AddAsync(Arg.Any<Contact>()).Returns(Task.CompletedTask);
            _repository.SaveChangesAsync().Returns(Task.CompletedTask);

            var result = await _service.CreateAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("John Doe", result.Name);
            Assert.Equal(Gender.Male, result.Gender);
            await _repository.Received(1).AddAsync(Arg.Any<Contact>());
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowValidationException_WhenNameEmpty()
        {
            var dto = new CreateContactDto
            {
                Name = "",
                Birthdate = new DateOnly(1990, 5, 15),
                Gender = Gender.Male
            };

            await Assert.ThrowsAsync<ValidationException>(async () => await _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowValidationException_WhenNameWhitespace()
        {
            var dto = new CreateContactDto
            {
                Name = "   ",
                Birthdate = new DateOnly(1990, 5, 15),
                Gender = Gender.Male
            };

            await Assert.ThrowsAsync<ValidationException>(async () => await _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowValidationException_WhenNameNull()
        {
            var dto = new CreateContactDto
            {
                Name = null!,
                Birthdate = new DateOnly(1990, 5, 15),
                Gender = Gender.Male
            };

            await Assert.ThrowsAsync<ValidationException>(async () => await _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowValidationException_WhenBirthdateInFuture()
        {
            var dto = new CreateContactDto
            {
                Name = "John Doe",
                Birthdate = DateOnly.FromDateTime(DateTime.Today).AddDays(1),
                Gender = Gender.Male
            };

            await Assert.ThrowsAsync<ValidationException>(async () => await _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowValidationException_WhenAgeExactly0()
        {
            var dto = new CreateContactDto
            {
                Name = "John Doe",
                Birthdate = DateOnly.FromDateTime(DateTime.Today),
                Gender = Gender.Male
            };

            await Assert.ThrowsAsync<ValidationException>(async () => await _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowValidationException_WhenAgeLessThan18()
        {
            var dto = new CreateContactDto
            {
                Name = "John Doe",
                Birthdate = DateOnly.FromDateTime(DateTime.Today).AddYears(-17),
                Gender = Gender.Male
            };

            await Assert.ThrowsAsync<ValidationException>(async () => await _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_ShouldSucceed_WhenAgeExactly18()
        {
            var dto = new CreateContactDto
            {
                Name = "John Doe",
                Birthdate = DateOnly.FromDateTime(DateTime.Today).AddYears(-18),
                Gender = Gender.Male
            };

            _repository.AddAsync(Arg.Any<Contact>()).Returns(Task.CompletedTask);
            _repository.SaveChangesAsync().Returns(Task.CompletedTask);

            var result = await _service.CreateAsync(dto);

            Assert.NotNull(result);
        }

        [Theory]
        [InlineData(Gender.Male)]
        [InlineData(Gender.Female)]
        [InlineData(Gender.Other)]
        public async Task CreateAsync_ShouldSucceed_WhenValidGender(Gender gender)
        {
            var dto = new CreateContactDto
            {
                Name = "John Doe",
                Birthdate = new DateOnly(1990, 5, 15),
                Gender = gender
            };

            _repository.AddAsync(Arg.Any<Contact>()).Returns(Task.CompletedTask);
            _repository.SaveChangesAsync().Returns(Task.CompletedTask);

            var result = await _service.CreateAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(gender, result.Gender);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowValidationException_WhenMultipleErrors()
        {
            var dto = new CreateContactDto
            {
                Name = "",
                Birthdate = DateOnly.FromDateTime(DateTime.Today).AddYears(-17),
                Gender = Gender.Male
            };

            var ex = await Assert.ThrowsAsync<ValidationException>(async () => await _service.CreateAsync(dto));
            Assert.True(ex.Errors.Count >= 2);
        }
    }

    public class GetAllAsync : ContactServiceTests
    {
        [Fact]
        public async Task GetAllAsync_ShouldReturnActiveContacts()
        {
            var query = new ContactQueryDto();
            var contacts = new List<Contact>
            {
                new() { Id = Guid.NewGuid(), Name = "John", Birthdate = new DateOnly(1990, 1, 1), Gender = Gender.Male, IsActive = true },
                new() { Id = Guid.NewGuid(), Name = "Jane", Birthdate = new DateOnly(1992, 1, 1), Gender = Gender.Female, IsActive = true }
            };

            _repository.GetAllActiveAsync(Arg.Any<ContactQueryDto>()).Returns(contacts);

            var result = await _service.GetAllAsync(query);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_ShouldFilterByName_CaseInsensitivePartial()
        {
            var query = new ContactQueryDto { Name = "john" };
            var contacts = new List<Contact>
            {
                new() { Id = Guid.NewGuid(), Name = "John Doe", Birthdate = new DateOnly(1990, 1, 1), Gender = Gender.Male, IsActive = true }
            };

            _repository.GetAllActiveAsync(query).Returns(contacts);

            var result = await _service.GetAllAsync(query);

            Assert.Single(result);
        }

        [Fact]
        public async Task GetAllAsync_ShouldFilterByGender()
        {
            var query = new ContactQueryDto { Gender = Gender.Male };
            var contacts = new List<Contact>
            {
                new() { Id = Guid.NewGuid(), Name = "John", Birthdate = new DateOnly(1990, 1, 1), Gender = Gender.Male, IsActive = true }
            };

            _repository.GetAllActiveAsync(query).Returns(contacts);

            var result = await _service.GetAllAsync(query);

            Assert.Single(result);
            Assert.Equal(Gender.Male, result[0].Gender);
        }

        [Fact]
        public async Task GetAllAsync_ShouldFilterByBirthdateFrom()
        {
            var query = new ContactQueryDto { BirthdateFrom = new DateOnly(1990, 1, 1) };
            var contacts = new List<Contact>
            {
                new() { Id = Guid.NewGuid(), Name = "John", Birthdate = new DateOnly(1995, 1, 1), Gender = Gender.Male, IsActive = true }
            };

            _repository.GetAllActiveAsync(query).Returns(contacts);

            var result = await _service.GetAllAsync(query);

            Assert.Single(result);
        }

        [Fact]
        public async Task GetAllAsync_ShouldFilterByBirthdateTo()
        {
            var query = new ContactQueryDto { BirthdateTo = new DateOnly(1995, 12, 31) };
            var contacts = new List<Contact>
            {
                new() { Id = Guid.NewGuid(), Name = "John", Birthdate = new DateOnly(1990, 1, 1), Gender = Gender.Male, IsActive = true }
            };

            _repository.GetAllActiveAsync(query).Returns(contacts);

            var result = await _service.GetAllAsync(query);

            Assert.Single(result);
        }

        [Fact]
        public async Task GetAllAsync_ShouldFilterByBirthdateRange()
        {
            var query = new ContactQueryDto
            {
                BirthdateFrom = new DateOnly(1990, 1, 1),
                BirthdateTo = new DateOnly(1995, 12, 31)
            };
            var contacts = new List<Contact>
            {
                new() { Id = Guid.NewGuid(), Name = "John", Birthdate = new DateOnly(1992, 6, 15), Gender = Gender.Male, IsActive = true }
            };

            _repository.GetAllActiveAsync(query).Returns(contacts);

            var result = await _service.GetAllAsync(query);

            Assert.Single(result);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmpty_WhenNoMatches()
        {
            var query = new ContactQueryDto { Name = "NonExistent" };
            var contacts = new List<Contact>();

            _repository.GetAllActiveAsync(query).Returns(contacts);

            var result = await _service.GetAllAsync(query);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_ShouldApplyAllFilters()
        {
            var query = new ContactQueryDto
            {
                Name = "john",
                Gender = Gender.Male,
                BirthdateFrom = new DateOnly(1990, 1, 1),
                BirthdateTo = new DateOnly(1995, 12, 31)
            };
            var contacts = new List<Contact>
            {
                new() { Id = Guid.NewGuid(), Name = "John Doe", Birthdate = new DateOnly(1992, 6, 15), Gender = Gender.Male, IsActive = true }
            };

            _repository.GetAllActiveAsync(query).Returns(contacts);

            var result = await _service.GetAllAsync(query);

            Assert.Single(result);
        }
    }

    public class GetByIdAsync : ContactServiceTests
    {
        [Fact]
        public async Task GetByIdAsync_ShouldReturnContact_WhenActive()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = true
            };

            _repository.GetByIdAsync(id).Returns(contact);

            var result = await _service.GetByIdAsync(id);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenNeverExisted()
        {
            var id = Guid.NewGuid();
            _repository.GetByIdAsync(id).Returns((Contact?)null);

            await Assert.ThrowsAsync<NotFoundException>(async () => await _service.GetByIdAsync(id));
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenInactive()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = false
            };

            _repository.GetByIdAsync(id).Returns(contact);

            await Assert.ThrowsAsync<NotFoundException>(async () => await _service.GetByIdAsync(id));
        }
    }

    public class UpdateAsync : ContactServiceTests
    {
        [Fact]
        public async Task UpdateAsync_ShouldReturnUpdatedContact_WhenValidInput()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = true
            };
            var dto = new UpdateContactDto
            {
                Name = "John Doe",
                Birthdate = new DateOnly(1990, 5, 15),
                Gender = Gender.Male
            };

            _repository.GetActiveByIdAsync(id).Returns(contact);
            _repository.SaveChangesAsync().Returns(Task.CompletedTask);

            var result = await _service.UpdateAsync(id, dto);

            Assert.NotNull(result);
            Assert.Equal("John Doe", result.Name);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenNotExisted()
        {
            var id = Guid.NewGuid();
            var dto = new UpdateContactDto { Name = "John Doe" };

            _repository.GetActiveByIdAsync(id).Returns((Contact?)null);

            await Assert.ThrowsAsync<NotFoundException>(async () => await _service.UpdateAsync(id, dto));
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowValidationException_WhenNameEmpty()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = true
            };
            var dto = new UpdateContactDto { Name = "" };

            _repository.GetActiveByIdAsync(id).Returns(contact);

            await Assert.ThrowsAsync<ValidationException>(async () => await _service.UpdateAsync(id, dto));
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowValidationException_WhenNameWhitespace()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = true
            };
            var dto = new UpdateContactDto { Name = "   " };

            _repository.GetActiveByIdAsync(id).Returns(contact);

            await Assert.ThrowsAsync<ValidationException>(async () => await _service.UpdateAsync(id, dto));
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowValidationException_WhenBirthdateInFuture()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = true
            };
            var dto = new UpdateContactDto { Birthdate = DateOnly.FromDateTime(DateTime.Today).AddDays(1) };

            _repository.GetActiveByIdAsync(id).Returns(contact);

            await Assert.ThrowsAsync<ValidationException>(async () => await _service.UpdateAsync(id, dto));
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowValidationException_WhenAgeExactly0()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = true
            };
            var dto = new UpdateContactDto { Birthdate = DateOnly.FromDateTime(DateTime.Today) };

            _repository.GetActiveByIdAsync(id).Returns(contact);

            await Assert.ThrowsAsync<ValidationException>(async () => await _service.UpdateAsync(id, dto));
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowValidationException_WhenAgeLessThan18()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = true
            };
            var dto = new UpdateContactDto { Birthdate = DateOnly.FromDateTime(DateTime.Today).AddYears(-17) };

            _repository.GetActiveByIdAsync(id).Returns(contact);

            await Assert.ThrowsAsync<ValidationException>(async () => await _service.UpdateAsync(id, dto));
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateOnlyName_WhenPartialUpdate()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = true
            };
            var dto = new UpdateContactDto { Name = "John Updated" };

            _repository.GetActiveByIdAsync(id).Returns(contact);
            _repository.SaveChangesAsync().Returns(Task.CompletedTask);

            var result = await _service.UpdateAsync(id, dto);

            Assert.Equal("John Updated", result.Name);
            Assert.Equal(new DateOnly(1990, 1, 1), result.Birthdate);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateOnlyBirthdate_WhenPartialUpdate()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = true
            };
            var newBirthdate = new DateOnly(1992, 6, 15);
            var dto = new UpdateContactDto { Birthdate = newBirthdate };

            _repository.GetActiveByIdAsync(id).Returns(contact);
            _repository.SaveChangesAsync().Returns(Task.CompletedTask);

            var result = await _service.UpdateAsync(id, dto);

            Assert.Equal("John", result.Name);
            Assert.Equal(newBirthdate, result.Birthdate);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateOnlyGender_WhenPartialUpdate()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = true
            };
            var dto = new UpdateContactDto { Gender = Gender.Female };

            _repository.GetActiveByIdAsync(id).Returns(contact);
            _repository.SaveChangesAsync().Returns(Task.CompletedTask);

            var result = await _service.UpdateAsync(id, dto);

            Assert.Equal("John", result.Name);
            Assert.Equal(Gender.Female, result.Gender);
        }

        [Fact]
        public async Task UpdateAsync_ShouldNotUpdate_WhenNoFieldsProvided()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var dto = new UpdateContactDto();

            _repository.GetActiveByIdAsync(id).Returns(contact);
            _repository.SaveChangesAsync().Returns(Task.CompletedTask);

            var result = await _service.UpdateAsync(id, dto);

            Assert.Equal("John", result.Name);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowValidationException_WhenMultipleErrors()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = true
            };
            var dto = new UpdateContactDto
            {
                Name = "",
                Birthdate = DateOnly.FromDateTime(DateTime.Today).AddYears(-17)
            };

            _repository.GetActiveByIdAsync(id).Returns(contact);

            var ex = await Assert.ThrowsAsync<ValidationException>(async () => await _service.UpdateAsync(id, dto));
            Assert.True(ex.Errors.Count >= 2);
        }
    }

    public class ActivateAsync : ContactServiceTests
    {
        [Fact]
        public async Task ActivateAsync_ShouldActivateContact_WhenInactive()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = false
            };

            _repository.GetByIdAsync(id).Returns(contact);
            _repository.SaveChangesAsync().Returns(Task.CompletedTask);

            await _service.ActivateAsync(id);

            Assert.True(contact.IsActive);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task ActivateAsync_ShouldThrowNotFoundException_WhenNeverExisted()
        {
            var id = Guid.NewGuid();
            _repository.GetByIdAsync(id).Returns((Contact?)null);

            await Assert.ThrowsAsync<NotFoundException>(async () => await _service.ActivateAsync(id));
        }

        [Fact]
        public async Task ActivateAsync_ShouldThrowConflictException_WhenAlreadyActive()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = true
            };

            _repository.GetByIdAsync(id).Returns(contact);

            await Assert.ThrowsAsync<ConflictException>(async () => await _service.ActivateAsync(id));
        }

        [Fact]
        public async Task ActivateAsync_ShouldReactivate_WhenPreviouslyDeactivated()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = false
            };

            _repository.GetByIdAsync(id).Returns(contact);
            _repository.SaveChangesAsync().Returns(Task.CompletedTask);

            await _service.ActivateAsync(id);
            Assert.True(contact.IsActive);

            contact.IsActive = false;
            await _service.ActivateAsync(id);
            Assert.True(contact.IsActive);
        }
    }

    public class DeactivateAsync : ContactServiceTests
    {
        [Fact]
        public async Task DeactivateAsync_ShouldDeactivateContact_WhenActive()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = true
            };

            _repository.GetByIdAsync(id).Returns(contact);
            _repository.UpdateAsync(Arg.Any<Contact>()).Returns(Task.CompletedTask);
            _repository.SaveChangesAsync().Returns(Task.CompletedTask);

            await _service.DeactivateAsync(id);

            Assert.False(contact.IsActive);
            await _repository.Received(1).UpdateAsync(contact);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task DeactivateAsync_ShouldThrowNotFoundException_WhenNeverExisted()
        {
            var id = Guid.NewGuid();
            _repository.GetByIdAsync(id).Returns((Contact?)null);

            await Assert.ThrowsAsync<NotFoundException>(async () => await _service.DeactivateAsync(id));
        }

        [Fact]
        public async Task DeactivateAsync_ShouldThrowConflictException_WhenAlreadyInactive()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = false
            };

            _repository.GetByIdAsync(id).Returns(contact);

            await Assert.ThrowsAsync<ConflictException>(async () => await _service.DeactivateAsync(id));
        }

        [Fact]
        public async Task DeactivateAsync_ShouldDeactivate_WhenPreviouslyActivated()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = true
            };

            _repository.GetByIdAsync(id).Returns(contact);
            _repository.UpdateAsync(Arg.Any<Contact>()).Returns(Task.CompletedTask);
            _repository.SaveChangesAsync().Returns(Task.CompletedTask);

            await _service.DeactivateAsync(id);
            Assert.False(contact.IsActive);

            contact.IsActive = true;
            await _service.DeactivateAsync(id);
            Assert.False(contact.IsActive);
        }
    }

    public class DeleteAsync : ContactServiceTests
    {
        [Fact]
        public async Task DeleteAsync_ShouldDeleteActiveContact()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = true
            };

            _repository.GetByIdAsync(id).Returns(contact);
            _repository.DeleteAsync(Arg.Any<Contact>()).Returns(Task.CompletedTask);
            _repository.SaveChangesAsync().Returns(Task.CompletedTask);

            await _service.DeleteAsync(id);

            await _repository.Received(1).DeleteAsync(contact);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteInactiveContact()
        {
            var id = Guid.NewGuid();
            var contact = new Contact
            {
                Id = id,
                Name = "John",
                Birthdate = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                IsActive = false
            };

            _repository.GetByIdAsync(id).Returns(contact);
            _repository.DeleteAsync(Arg.Any<Contact>()).Returns(Task.CompletedTask);
            _repository.SaveChangesAsync().Returns(Task.CompletedTask);

            await _service.DeleteAsync(id);

            await _repository.Received(1).DeleteAsync(contact);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowNotFoundException_WhenNeverExisted()
        {
            var id = Guid.NewGuid();
            _repository.GetByIdAsync(id).Returns((Contact?)null);

            await Assert.ThrowsAsync<NotFoundException>(async () => await _service.DeleteAsync(id));
        }
    }
}
