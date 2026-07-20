using System;
using UnityEngine;

namespace HorrorGame.MiniGames
{
    /// <summary>
    /// Collectible key fragment for the flashlight mini-game.
    /// </summary>
    public sealed class FlashlightKeyFragment : MonoBehaviour
    {
        [SerializeField] private GameObject collectedVisual;

        public bool IsCollected { get; private set; }
        public event Action<FlashlightKeyFragment> Collected;

        public void ResetFragment()
        {
            IsCollected = false;
            if (collectedVisual != null)
            {
                collectedVisual.SetActive(true);
            }
        }

        /// <summary>
        /// Should be called by flashlight-hit detection when this fragment is illuminated.
        /// </summary>
        public void Collect()
        {
            if (IsCollected)
            {
                return;
            }

            IsCollected = true;
            if (collectedVisual != null)
            {
                collectedVisual.SetActive(false);
            }

            Collected?.Invoke(this);
        }
    }
}
