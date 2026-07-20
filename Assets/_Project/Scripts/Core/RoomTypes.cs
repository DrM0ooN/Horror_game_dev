namespace HorrorGame.Core
{
    public enum RoomLifecycleState
    {
        IdleExplorable = 0,
        ReadyToStart = 1,
        LockedInProgress = 2,
        SolvedClosed = 3,
        FailedLocked = 4,
    }

    public enum RoomDoorState
    {
        Open = 0,
        Locked = 1,
        ClosedCompleted = 2,
        ClosedFailed = 3,
    }
}
