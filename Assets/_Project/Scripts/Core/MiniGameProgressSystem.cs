using UnityEngine;

namespace HorrorGame.Core
{
    /// <summary>
    /// Shared run-progress state: how many challenge rooms have been solved so far. Mirrors
    /// HubThreatSystem's shape but tracks the opposite direction (solves, not threat).
    /// </summary>
    public sealed class MiniGameProgressSystem : MonoBehaviour
    {
        [SerializeField, Min(1)] private int totalRooms = 4;
        [SerializeField] private MiniGameProgressIndicatorView indicatorView;

        public int SolvedCount { get; private set; }

        /// <summary>
        /// Marks one more room solved and updates the view. Safe to call once per room clear.
        /// </summary>
        public void RoomSolved()
        {
            SolvedCount = Mathf.Clamp(SolvedCount + 1, 0, totalRooms);
            if (indicatorView != null)
            {
                indicatorView.SetSolvedCount(SolvedCount);
            }
        }
    }
}
