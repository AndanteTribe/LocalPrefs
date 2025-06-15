#nullable enable

using MessagePack;

namespace AndanteTribe.IO.Tests
{
    [MessagePackObject(AllowPrivate = true)]
    internal sealed record CustomData
    {
        [Key(0)]
        public int Id { get; init; }
        [Key(1)]
        public string? Name { get; init; }
    }
}