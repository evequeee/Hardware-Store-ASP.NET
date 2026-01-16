using HardwareStore.Domain.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace HardwareStore.Infrastructure.Data.Serializers;

public class EmailSerializer : SerializerBase<Email>
{
    public override Email Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var value = context.Reader.ReadString();
        return new Email(value);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Email value)
    {
        if (value == null)
        {
            context.Writer.WriteNull();
            return;
        }

        context.Writer.WriteString(value.Value);
    }
}
