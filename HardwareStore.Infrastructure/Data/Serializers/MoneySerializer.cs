using HardwareStore.Domain.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace HardwareStore.Infrastructure.Data.Serializers;

public class MoneySerializer : SerializerBase<Money>
{
    public override Money Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonReader = context.Reader;
        bsonReader.ReadStartDocument();
        
        decimal amount = 0;
        string currency = "USD";

        while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var name = bsonReader.ReadName(Utf8NameDecoder.Instance);
            
            if (name == "Amount")
                amount = decimal.Parse(bsonReader.ReadString());
            else if (name == "Currency")
                currency = bsonReader.ReadString();
            else
                bsonReader.SkipValue();
        }

        bsonReader.ReadEndDocument();
        return new Money(amount, currency);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Money value)
    {
        if (value == null)
        {
            context.Writer.WriteNull();
            return;
        }

        context.Writer.WriteStartDocument();
        context.Writer.WriteName("Amount");
        context.Writer.WriteString(value.Amount.ToString());
        context.Writer.WriteName("Currency");
        context.Writer.WriteString(value.Currency);
        context.Writer.WriteEndDocument();
    }
}
