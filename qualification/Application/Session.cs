using Qualification.Domain;

namespace Qualification.Application;

public sealed class Session(State initial, Action<string>? log=null)
{
    public State Current { get; private set; } = initial;
    public string Submit(AdvanceCommand command)
    {
        State next=Fixture.Apply(Current,command);
        Current=next;
        string hash=Fixture.Hash(next);
        log?.Invoke($"revision={next.Execution.Revision} hash={hash}");
        return hash;
    }
}

public sealed record ReplayResult(string InitialHash, string[] Hashes, string FinalHash);

public static class Replay
{
    public static ReplayResult Run(bool logging=false,bool reversed=false,ulong seed=123456789)
    {
        var session=new Session(Fixture.Initial(seed,reversed),logging ? Console.Error.WriteLine : null);
        string initial=Fixture.Hash(session.Current);
        string[] hashes=Fixture.Journal().Select(session.Submit).ToArray();
        return new(initial,hashes,Fixture.Hash(session.Current));
    }
}
