using AspNetCore.Kafka.Abstractions;
using AspNetCore.Kafka.Avro;
using Avro.Generic;
using Confluent.Kafka;

namespace AspNetCore.Kafka.Client.Consumer
{
    public class MessageParser<TKey, TValue>
    {
        private readonly IMessageSerializer _serializer;

        public MessageParser(IMessageSerializer serializer)
        {
            _serializer = serializer;
        }

        public TContract Parse<TContract>(ConsumeResult<TKey, TValue> message) where TContract : class
        {
            if (message.Message.Value == null)
                return null;

            if (typeof(TValue) == typeof(TContract))
                return message.Message.Value as TContract;

            if (message.Message.Value is GenericRecord x)
                return x.ToObject<TContract>();

            var json = message.Message.Value.ToString(); 

            return json is not null ? _serializer.Deserialize<TContract>(json) : null;
        }
    }
}