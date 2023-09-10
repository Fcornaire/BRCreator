namespace BRCreator.RacePlugin.Race
{
    public enum RaceState
    {
        None,
        Loaded,
        WaitingForRace,
        WaitingForPlayers,
        LoadingStage,
        WaitingForPlayersToBeReady,
        Starting,
        Racing,
        WaitingForFullRanking,
        ShowRanking,
        Finished,
        ForcedFinish,
        Aborted
    }
}
