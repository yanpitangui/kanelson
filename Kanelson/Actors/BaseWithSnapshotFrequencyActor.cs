using Akka.Persistence;

namespace Kanelson.Actors;

public abstract class BaseWithSnapshotFrequencyActor :  ReceivePersistentActor
{
    public int SnapShotInterval { get; protected init; } = 5;

    protected void SaveSnapshotIfPassedInterval(object stateSnapshot)
    {
        if (LastSequenceNr % SnapShotInterval == 0 && LastSequenceNr != 0)
        {
            SaveSnapshot(stateSnapshot);
        }
    }
}