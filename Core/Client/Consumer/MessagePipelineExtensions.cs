using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AspNetCore.Kafka.Abstractions;
using AspNetCore.Kafka.Data;
using Microsoft.Extensions.Logging;

namespace AspNetCore.Kafka.Client.Consumer
{
    public static class MessagePipelineExtensions
    {
        public static IMessagePipeline<TSource, TDestination> Action<TSource, TDestination>(
            this IMessagePipeline<TSource, TDestination> pipeline,
            params Func<TDestination, Task>[] handlers)
        {
            return pipeline.Action(null, handlers);
        }
        
        public static IMessagePipeline<TSource, TDestination> Action<TSource, TDestination>(
            this IMessagePipeline<TSource, TDestination> pipeline,
            ILogger log,
            params Func<TDestination, Task>[] handlers)
        {
            return pipeline.Block(new TransformBlock<TDestination, TDestination>(async x =>
                {
                    foreach (var handler in handlers)
                    {
                        try
                        {
                            await handler(x);
                        }
                        catch (Exception e)
                        {
                            log?.LogError(e, "Message handler failure");
                        }
                    }

                    return x;
                },
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 1,
                    EnsureOrdered = true
                }));
        }

        public static IMessagePipeline<TSource, TDestination> Buffer<TSource, TDestination>(
            this IMessagePipeline<TSource, TDestination> pipeline, 
            int size)
        {
            if (size <= 1)
                throw new ArgumentException("Buffer size must be greater that 1");

            return pipeline.Block(new TransformBlock<TDestination, TDestination>(x => x, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = size,
                EnsureOrdered = true
            }));
        }
        
        public static IMessagePipeline<TSource, IMessageOffset> Commit<TSource>(
            this IMessagePipeline<TSource, IMessageOffset> pipeline)
        {
            return pipeline.Block(new TransformBlock<IMessageOffset, IMessageOffset>(x =>
                {
                    x.Commit(true);
                    return x;
                },
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 1,
                    EnsureOrdered = true
                }));
        }

        /*
        public static IMessagePipeline<TSource, IEnumerable<TDestination>> Batch<TSource, TDestination>(
            this IMessagePipeline<TSource, TDestination> pipeline,
            int size, 
            int time = 0)
        {
            if (size <= 1)
                throw new ArgumentException("Buffer size must be greater that 1");
            
            var batch = new BatchBlock<TDestination>(size, new GroupingDataflowBlockOptions
            {
                EnsureOrdered = true,
                BoundedCapacity = size
            });
            
            var timer = new Timer(_ => batch.TriggerBatch(), null, time, Timeout.Infinite);

            var transform = new TransformBlock<TDestination[], TDestination[]>(x =>
                {
                    timer.Change(time, Timeout.Infinite);
                    return x;
                },
                new ExecutionDataflowBlockOptions
                {
                    EnsureOrdered = true,
                    BoundedCapacity = 1
                });
            
            batch.LinkTo(transform);

            return pipeline.Block(DataflowBlock.Encapsulate(batch, transform));
        }*/

        public static IMessagePipeline<IMessage<T>, IMessageEnumerable<T>> Batch<T>(
            this IMessagePipeline<IMessage<T>, IMessage<T>> pipeline,
            int size,
            int time)
        {
            return pipeline.Batch(size, TimeSpan.FromMilliseconds(time));
        }
        
        public static IMessagePipeline<IMessage<T>, IMessageEnumerable<T>> Batch<T>(
            this IMessagePipeline<IMessage<T>, IMessage<T>> pipeline,
            int size, 
            TimeSpan time)
        {
            if (size <= 1)
                throw new ArgumentException("Buffer size must be greater that 1");
            
            var batch = new BatchBlock<IMessage<T>>(size, new GroupingDataflowBlockOptions
            {
                EnsureOrdered = true,
                BoundedCapacity = size
            });

            var timer = time.TotalMilliseconds > 0
                ? new Timer(_ => batch.TriggerBatch(), null, (int) time.TotalMilliseconds, Timeout.Infinite)
                : null;

            var transform = new TransformBlock<IMessage<T>[], IMessageEnumerable<T>>(x =>
                {
                    timer?.Change((int) time.TotalMilliseconds, Timeout.Infinite);
                    return new KafkaMessageEnumerable<T>(x);
                },
                new ExecutionDataflowBlockOptions
                {
                    EnsureOrdered = true,
                    BoundedCapacity = 1
                });
            
            batch.LinkTo(transform);

            return pipeline.Block(DataflowBlock.Encapsulate(batch, transform));
        }
    }
}