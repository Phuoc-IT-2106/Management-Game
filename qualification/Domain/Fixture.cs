using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Qualification.Domain;

// Artificial counters, not production economy, roster or competition rules.
public sealed record CompanyState(long Counter);
public sealed record Entity(string Id, long Counter);
public sealed record WorldState(int Day, ImmutableArray<Entity> Entities);
public sealed record Receipt(string Id, string Payload);
// Technical replay bookkeeping; no third gameplay authority.
public sealed record ExecutionEnvelope(ulong Seed, int Revision, ImmutableArray<Receipt> Receipts);
public sealed record State(CompanyState Company, WorldState World, ExecutionEnvelope Execution);
public sealed record AdvanceCommand(string Id, int ExpectedRevision, string EntityId, int Delta);

public static class Fixture
{
    public static State Initial(ulong seed=123456789, bool reversed=false)
    {
        var rows = new[] {new Entity("entity-A",3),new Entity("entity-B",7),new Entity("entity-C",11)};
        if(reversed) Array.Reverse(rows);
        return new(new(0),new(1,[..rows]),new(seed,0,[]));
    }

    public static ImmutableArray<AdvanceCommand> Journal() => [..Enumerable.Range(0,32).Select(i =>
        new AdvanceCommand($"cmd-{i:D3}",i,new[] {"entity-A","entity-B","entity-C"}[i%3],i%7-3))];

    public static State Apply(State state, AdvanceCommand command)
    {
        string payload=$"{command.ExpectedRevision.ToString(CultureInfo.InvariantCulture)}|{command.EntityId}|{command.Delta.ToString(CultureInfo.InvariantCulture)}";
        var previous=state.Execution.Receipts.FirstOrDefault(r=>r.Id==command.Id);
        if(previous is not null)
            return previous.Payload==payload ? state : throw new InvalidOperationException("Conflicting command ID");
        if(command.ExpectedRevision!=state.Execution.Revision || !state.World.Entities.Any(e=>e.Id==command.EntityId))
            throw new InvalidOperationException("Stale revision or missing entity");
        int draw=KeyedRng.Range(state.Execution.Seed,"fixture",command.Id,"adjust",0,17);
        // Stage all checked changes before publishing the new immutable roots.
        long change=checked(command.Delta+draw);
        var company=new CompanyState(checked(state.Company.Counter+change));
        var entities=state.World.Entities.OrderBy(e=>e.Id,StringComparer.Ordinal).Select(e=>
            e.Id==command.EntityId ? e with { Counter=checked(e.Counter+change) } : e).ToImmutableArray();
        return new(company,new(checked(state.World.Day+1),entities),
            state.Execution with { Revision=checked(state.Execution.Revision+1), Receipts=state.Execution.Receipts.Add(new(command.Id,payload)) });
    }

    public static string Hash(State state)
    {
        using var buffer=new MemoryStream();
        using(var json=new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject(); json.WriteString("canonical","fixture-canonical-v1");
            json.WriteString("rules","artificial-v1"); json.WriteString("content","synthetic-v1");
            json.WriteString("rng","rng-v1");
            json.WriteStartObject("company"); json.WriteNumber("counter",state.Company.Counter); json.WriteEndObject();
            json.WriteStartObject("world"); json.WriteNumber("day",state.World.Day); json.WriteStartArray("entities");
            foreach(var e in state.World.Entities.OrderBy(e=>e.Id,StringComparer.Ordinal))
            { json.WriteStartObject(); json.WriteString("id",e.Id); json.WriteNumber("counter",e.Counter); json.WriteEndObject(); }
            json.WriteEndArray(); json.WriteEndObject();
            json.WriteStartObject("execution"); json.WriteNumber("seed",state.Execution.Seed); json.WriteNumber("revision",state.Execution.Revision);
            json.WriteStartArray("receipts");
            foreach(var r in state.Execution.Receipts.OrderBy(r=>r.Id,StringComparer.Ordinal))
            { json.WriteStartObject(); json.WriteString("id",r.Id); json.WriteString("payload",r.Payload); json.WriteEndObject(); }
            json.WriteEndArray(); json.WriteEndObject(); json.WriteEndObject();
        }
        return Convert.ToHexString(SHA256.HashData(buffer.ToArray())).ToLowerInvariant();
    }
}

public static class KeyedRng
{
    public static ulong Draw(ulong seed,string domain,string eventId,string purpose,int index,int retry=0)
    {
        string[] parts=["rng-v1",seed.ToString(CultureInfo.InvariantCulture),domain,eventId,purpose,
            index.ToString(CultureInfo.InvariantCulture),retry.ToString(CultureInfo.InvariantCulture)];
        using var buffer=new MemoryStream();
        Span<byte> length=stackalloc byte[4];
        foreach(string part in parts)
        {
            byte[] bytes=Encoding.UTF8.GetBytes(part.Normalize(NormalizationForm.FormC));
            BinaryPrimitives.WriteInt32BigEndian(length,bytes.Length); buffer.Write(length); buffer.Write(bytes);
        }
        return BinaryPrimitives.ReadUInt64BigEndian(SHA256.HashData(buffer.ToArray()));
    }

    public static int Range(ulong seed,string domain,string eventId,string purpose,int index,int exclusiveMax)
    {
        if(exclusiveMax<=0) throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
        ulong bound=(ulong)exclusiveMax,threshold=unchecked(0UL-bound)%bound;
        for(int retry=0;;retry=checked(retry+1))
        { ulong value=Draw(seed,domain,eventId,purpose,index,retry); if(value>=threshold) return (int)(value%bound); }
    }
}
