using UnityEngine;

namespace HorrorGame.Core
{
    /// <summary>
    /// Controls physical door blocking and visual toggles for a room.
    /// </summary>
    public sealed class RoomDoorController : MonoBehaviour
    {
        [SerializeField] private Collider doorBlocker;
        [SerializeField] private GameObject openVisual;
        [SerializeField] private GameObject closedVisual;

        public RoomDoorState CurrentDoorState { get; private set; } = RoomDoorState.Open;

        /// <summary>
        /// Applies a room door state to collision and visuals.
        /// </summary>
        public void SetDoorState(RoomDoorState state)
        {
            CurrentDoorState = state;

            var isOpen = state == RoomDoorState.Open;
            if (doorBlocker != null)
            {
                doorBlocker.enabled = !isOpen;
            }

            if (openVisual != null)
            {
                openVisual.SetActive(isOpen);
            }

            if (closedVisual != null)
            {
                closedVisual.SetActive(!isOpen);
            }
        }
    }
}
