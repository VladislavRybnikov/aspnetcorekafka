# AspNetCore.Kafka samples

## Registration

```c#
// Get Kafka bootstrap servers from ConnectionString:Kafka options
services.AddKafka(Configuration);
```

## Message handlers

```c#
// optional attribute
[Message(Topic = "event.currency.rate-{env}", Format = TopicFormat.Avro)]
public class RateNotification
{
    public string Currency { get; set; }
    public decimal Rate { get; set; }
}

// Attribute to mark as a Kafka message handler.
// Otherwise - class name must have a 'MessageHandler' suffix.
[Message]
public class RateNotificationMessageHandler
{
    // class with proper DI support.

    // Required attribute for actual subscription
    [Message(Topic = "event.currency.rate-{env}", Format = TopicFormat.Avro, Offset = TopicOffset.Begin))]
    // or to get topic name from type definition attribute
    [Message]
    public Task Handler(IMessage<RateNotification> message)
    {
        Console.WriteLine($"{message.Currency} rate is {message.Rate}");
        return Task.CompletedTask;
    }
}
```

## Message blocks

* [MessageBatch] - batch messages by size and time.
* [MessageBuffer] - buffer messages by size.

User defined message blocks supported via MessageConverterAttribute

```c#
public class MyBatchOptions
{
    // Max size of the batch
    public int Size { get; set; }
    
    // Max period in milliseconds to populate batch before consuming
    public int Time { get; set; }
}

public class RateNotificationMessageHandler
{
    [Message]
    // batching
    [MessageConverter(typeof(BatchMessageConverter), typeof(MyBatchOptions))]
    // or
    [MessageBatch(Size = 190, Time = 5000)]
    // or
    [MessageBuffer(Size = 10)]
    // or
    [MessageBatch(typeof(MyBatchOptions))]
    public Task Handler(IEnumerable<IMessage<RateNotification>> messages)
    {
        Console.WriteLine($"Received batch with size {messages.Count}");
        return Task.CompletedTask;
    }
}
```

## Interceptors

```c#

public class MyInterceptor : IMessageInterceptor
{
    public Task ConsumeAsync(IMessage<object> message, Exception exception);
    {
        Console.WriteLine($"{message.Topic} processed. Exception: {exception}");
        return Task.CompletedTask;
    }
    
    public Task ProduceAsync(string topic, object key, object message, Exception exception)
    {
        Console.WriteLine($"{message.Topic} produced. Exception: {exception}");
        return Task.CompletedTask;
    }
}

services
    .AddKafka(Configuration)
    .AddInterceptor(new MyInterceptor())
    // or
    .AddInterceptor(x => new MyInterceptor())
    // or
    .AddInterceptor(typeof(MyInterceptor))
    // or
    .AddInterceptor<MyInterceptor>();
```

## Metrics

Implemented as a MetricsInterceptor.

```c#
services
    .AddKafka(Configuration)
    .AddMetrics();
```

## Configuration

```json
{
  "Kafka": {
    "Group": "consumer-group-name",
    "Producer": {
      "linger.ms": 5,
      "socket.timeout.ms": 15000,
      "message.send.max.retries": 10,
      "message.timeout.ms": 200000
    },
    "Consumer": {
      "socket.timeout.ms": 15000,
      "enable.auto.commit": false
    }
  },
  "ConnectionStrings": {
    "Kafka": "192.168.0.1:9092,192.168.0.2:9092",
    "SchemaRegistry": "http://192.168.0.1:8084"
  }
}
```
