using System;
using AspNetCore.Kafka.Abstractions;

namespace AspNetCore.Kafka.Data
{
    public class MessagePayload<TContract> : IMessage<TContract>
    {
        private bool _suppressCommit;
        private readonly Lazy<bool> _commit;

        public MessagePayload(Func<bool> commit) => _commit = new Lazy<bool>(commit);

        public TContract Value { get; init; }
        
        public int Partition { get; init; }
        
        public long Offset { get; init; }
        
        public string Key { get; init; }
        
        public string Topic { get; init; }
        
        public void SuppressCommit() => _suppressCommit = true;

        public bool Commit(bool force = false) => !force && _suppressCommit || _commit.Value;
    }
}