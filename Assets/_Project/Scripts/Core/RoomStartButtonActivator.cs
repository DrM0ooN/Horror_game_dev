using UnityEngine;
using HorrorGame.Player;

namespace HorrorGame.Core
{
    /// <summary>
    /// Walk-into trigger for RoomStartButton, standing in for a proper interact-input system.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class RoomStartButtonActivator : MonoBehaviour
    {
        [SerializeField] private RoomStartButton startButton;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<RoomPlayerMarker>();
            if (player == null || startButton == null)
            {
                return;
            }

            startButton.TryPress(player);
        }
    }
}
