using HardwareStore.Domain.Common;
using HardwareStore.Domain.ValueObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace HardwareStore.Domain.Entities;

[BsonCollection("customers")]
public class Customer : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Email Email { get; set; } = null!;
    public string Phone { get; set; } = string.Empty;
    public Address Address { get; set; } = null!;

    [BsonIgnore]
    public string FullName => $"{FirstName} {LastName}";
}
