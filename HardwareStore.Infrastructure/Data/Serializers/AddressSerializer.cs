using HardwareStore.Domain.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace HardwareStore.Infrastructure.Data.Serializers;

public class AddressSerializer : SerializerBase<Address>
{
    public override Address Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonReader = context.Reader;
        bsonReader.ReadStartDocument();
        
        string street = string.Empty;
        string city = string.Empty;
        string state = string.Empty;
        string zipCode = string.Empty;
        string country = string.Empty;

        while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var name = bsonReader.ReadName(Utf8NameDecoder.Instance);
            
            switch (name)
            {
                case "Street":
                    street = bsonReader.ReadString();
                    break;
                case "City":
                    city = bsonReader.ReadString();
                    break;
                case "State":
                    state = bsonReader.ReadString();
                    break;
                case "ZipCode":
                    zipCode = bsonReader.ReadString();
                    break;
                case "Country":
                    country = bsonReader.ReadString();
                    break;
                default:
                    bsonReader.SkipValue();
                    break;
            }
        }

        bsonReader.ReadEndDocument();
        return new Address(street, city, state, zipCode, country);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Address value)
    {
        if (value == null)
        {
            context.Writer.WriteNull();
            return;
        }

        context.Writer.WriteStartDocument();
        context.Writer.WriteName("Street");
        context.Writer.WriteString(value.Street);
        context.Writer.WriteName("City");
        context.Writer.WriteString(value.City);
        context.Writer.WriteName("State");
        context.Writer.WriteString(value.State ?? string.Empty);
        context.Writer.WriteName("ZipCode");
        context.Writer.WriteString(value.ZipCode ?? string.Empty);
        context.Writer.WriteName("Country");
        context.Writer.WriteString(value.Country);
        context.Writer.WriteEndDocument();
    }
}
