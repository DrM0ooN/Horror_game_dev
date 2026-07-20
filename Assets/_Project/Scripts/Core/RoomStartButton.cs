using HorrorGame.Player;
using UnityEngine;

namespace HorrorGame.Core
{
    /// <summary>
    /// Interaction endpoint for committing room start once enough players are inside.
    /// </summary>
    public sealed class RoomStartButton : MonoBehaviour
    {
        [SerializeField] private RoomController roomController;

        /// <summary>
        /// Hook this method to your interaction system when the player presses the start button.
        /// </summary>
        public bool TryPress(RoomPlayerMarker presser)
        {
            if (roomController == null)
            {
                return false;
            }

            return roomController.TryStartChallenge(presser);
        }
    }
}
