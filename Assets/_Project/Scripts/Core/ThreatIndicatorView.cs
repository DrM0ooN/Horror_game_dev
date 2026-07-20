using UnityEngine;

namespace HorrorGame.Core
{
    /// <summary>
    /// Simple visual hook for displaying current threat near the hub front door.
    /// </summary>
    public sealed class ThreatIndicatorView : MonoBehaviour
    {
        [SerializeField] private GameObject[] threatSteps;

        public void SetThreatLevel(int level)
        {
            if (threatSteps == null)
            {
                return;
            }

            for (var i = 0; i < threatSteps.Length; i++)
            {
                if (threatSteps[i] != null)
                {
                    threatSteps[i].SetActive(i < level);
                }
            }
        }
    }
}
