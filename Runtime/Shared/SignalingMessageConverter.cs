using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace StinkySteak.N2D
{
    public class SignalingMessageConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SignalingMessage);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            var type = obj["Type"]?.ToObject<SignalingMessageType>();

            return type switch
            {
                SignalingMessageType.RequestAllocation => obj.ToObject<SignalingMessageRequestAllocation>(serializer),
                SignalingMessageType.JoinCodeAllocated => obj.ToObject<SignalingMessageJoinCodeAllocated>(serializer),
                SignalingMessageType.Answer => obj.ToObject<SignalingMessageAnswer>(serializer),
                SignalingMessageType.Offer => obj.ToObject<SignalingMessageOffer>(serializer),
                SignalingMessageType.Ping => obj.ToObject<SignalingMessagePing>(serializer),
                _ => throw new JsonSerializationException("Unknown SignalingMessageType")
            };
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JObject obj = JObject.FromObject(value, serializer);
            obj.WriteTo(writer);
        }
    }
}
