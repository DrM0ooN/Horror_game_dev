using UnityEngine;

namespace HorrorGame.Core
{
    /// <summary>
    /// Displays overall run progress as orbs that light up one at a time as rooms are solved.
    /// Mirrors ThreatIndicatorView's step pattern, but drives per-orb lit state instead of
    /// GameObject activation, since these orbs stay visible (dormant) rather than hidden.
    /// </summary>
    public sealed class MiniGameProgressIndicatorView : MonoBehaviour
    {
        [SerializeField] private MiniGameProgressOrb[] progressOrbs;

        public void SetSolvedCount(int count)
        {
            if (progressOrbs == null)
            {
                return;
            }

            for (var i = 0; i < progressOrbs.Length; i++)
            {
                if (progressOrbs[i] != null)
                {
                    progressOrbs[i].SetLit(i < count);
                }
            }
        }
    }
}
