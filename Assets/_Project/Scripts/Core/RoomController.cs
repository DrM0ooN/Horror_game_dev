using System.Collections.Generic;
using HorrorGame.Player;
using UnityEngine;

namespace HorrorGame.Core
{
    /// <summary>
    /// Owns room lifecycle, start gating, door state transitions, and solve/fail flow.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class RoomController : MonoBehaviour
    {
        [Header("Room Rules")]
        [SerializeField, Min(1)] private int requiredPlayers = 1;

        [Header("References")]
        [SerializeField] private RoomDoorController doorController;
        [SerializeField] private MonoBehaviour miniGameBehaviour;
        [SerializeField] private HubThreatSystem hubThreatSystem;
        [SerializeField] private Transform[] hubRespawnPoints;

        private readonly HashSet<RoomPlayerMarker> playersInside = new HashSet<RoomPlayerMarker>();
        private readonly List<RoomPlayerMarker> activeRunPlayers = new List<RoomPlayerMarker>();

        private IMiniGame miniGame;

        public RoomLifecycleState LifecycleState { get; private set; } = RoomLifecycleState.IdleExplorable;

        /// <summary>
        /// True when enough players are in the room and challenge is not started yet.
        /// </summary>
        public bool CanStart =>
            (LifecycleState == RoomLifecycleState.IdleExplorable || LifecycleState == RoomLifecycleState.ReadyToStart)
            && playersInside.Count >= requiredPlayers;

        private void Awake()
        {
            miniGame = miniGameBehaviour as IMiniGame;
            if (miniGame == null)
            {
                Debug.LogError("RoomController requires a MiniGame behaviour implementing IMiniGame.", this);
                enabled = false;
                return;
            }

            miniGame.Solved += OnMiniGameSolved;
            miniGame.Failed += OnMiniGameFailed;

            var trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnDestroy()
        {
            if (miniGame == null)
            {
                return;
            }

            miniGame.Solved -= OnMiniGameSolved;
            miniGame.Failed -= OnMiniGameFailed;
        }

        private void Start()
        {
            LifecycleState = RoomLifecycleState.IdleExplorable;
            if (doorController != null)
            {
                doorController.SetDoorState(RoomDoorState.Open);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<RoomPlayerMarker>();
            if (player == null)
            {
                return;
            }

            if (LifecycleState == RoomLifecycleState.LockedInProgress || LifecycleState == RoomLifecycleState.SolvedClosed || LifecycleState == RoomLifecycleState.FailedLocked)
            {
                return;
            }

            playersInside.Add(player);
            UpdateReadyState();
        }

        private void OnTriggerExit(Collider other)
        {
            var player = other.GetComponentInParent<RoomPlayerMarker>();
            if (player == null)
            {
                return;
            }

            if (playersInside.Remove(player))
            {
                UpdateReadyState();
            }
        }

        /// <summary>
        /// Called by the room start button. Any valid player inside can commit the start.
        /// </summary>
        public bool TryStartChallenge(RoomPlayerMarker requestedBy)
        {
            if (requestedBy == null || !playersInside.Contains(requestedBy) || !CanStart)
            {
                return false;
            }

            LifecycleState = RoomLifecycleState.LockedInProgress;
            if (doorController != null)
            {
                doorController.SetDoorState(RoomDoorState.Locked);
            }

            activeRunPlayers.Clear();
            activeRunPlayers.AddRange(playersInside);
            for (var i = 0; i < activeRunPlayers.Count; i++)
            {
                activeRunPlayers[i].GrantFlashlight();
            }

            miniGame.OnPlayersEntered(BuildPlayerRefs(activeRunPlayers));
            return true;
        }

        private void UpdateReadyState()
        {
            if (LifecycleState == RoomLifecycleState.LockedInProgress || LifecycleState == RoomLifecycleState.SolvedClosed || LifecycleState == RoomLifecycleState.FailedLocked)
            {
                return;
            }

            LifecycleState = playersInside.Count >= requiredPlayers
                ? RoomLifecycleState.ReadyToStart
                : RoomLifecycleState.IdleExplorable;
        }

private void OnMiniGameSolved()
        {
            LifecycleState = RoomLifecycleState.SolvedClosed;
            if (doorController != null)
            {
                doorController.SetDoorState(RoomDoorState.ClosedCompleted);
            }

            NotificationHud.Toast("Room solved!");

            RespawnPlayersToHub();
            RemoveFlashlights();
            playersInside.Clear();
            activeRunPlayers.Clear();
        }

        private void OnMiniGameFailed()
        {
            LifecycleState = RoomLifecycleState.FailedLocked;
            if (doorController != null)
            {
                doorController.SetDoorState(RoomDoorState.ClosedFailed);
            }

            if (hubThreatSystem != null)
            {
                hubThreatSystem.IncreaseThreat();
            }

            RespawnPlayersToHub();
            RemoveFlashlights();
            playersInside.Clear();
            activeRunPlayers.Clear();
        }

        private void RespawnPlayersToHub()
        {
            if (hubRespawnPoints == null || hubRespawnPoints.Length == 0)
            {
                return;
            }

            for (var i = 0; i < activeRunPlayers.Count; i++)
            {
                var spawn = hubRespawnPoints[i % hubRespawnPoints.Length];
                activeRunPlayers[i].RespawnAt(spawn);
            }
        }

        private void RemoveFlashlights()
        {
            for (var i = 0; i < activeRunPlayers.Count; i++)
            {
                activeRunPlayers[i].RemoveFlashlight();
            }
        }

        private static List<int> BuildPlayerRefs(List<RoomPlayerMarker> players)
        {
            var refs = new List<int>(players.Count);
            for (var i = 0; i < players.Count; i++)
            {
                refs.Add(players[i].PlayerId);
            }

            return refs;
        }
    }
}
