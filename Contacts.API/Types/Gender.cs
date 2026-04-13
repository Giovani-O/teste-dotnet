using System.Text.Json.Serialization;

namespace Contacts.API.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Gender
{
    Male,
    Female,
    Other
}
