namespace Kanelson.Actors;

public interface IHasSnapshotInterval
{
    private const int SnapShotInterval = 5;
    public long LastSequenceNr { get; }

    public void SaveSnapshot(object snapshot);

    public void SaveSnapshotIfPassedInterval(object stateSnapshot)
    {
        if (LastSequenceNr % SnapShotInterval == 0 && LastSequenceNr != 0)
        {
            SaveSnapshot(stateSnapshot);
        }
    }
}