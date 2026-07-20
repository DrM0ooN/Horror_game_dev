using UnityEngine;

namespace HorrorGame.Player
{
    /// <summary>
    /// Lightweight player marker used by room systems for membership, respawn, and temporary item grants.
    /// </summary>
    public sealed class RoomPlayerMarker : MonoBehaviour
    {
        [SerializeField] private int playerId;
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private FlashlightItem flashlightItem;

        public int PlayerId => playerId;

        public void GrantFlashlight()
        {
            if (flashlightItem != null)
            {
                flashlightItem.SetEnabled(true);
                flashlightItem.ResetBattery();
            }
        }

        public void RemoveFlashlight()
        {
            if (flashlightItem != null)
            {
                flashlightItem.SetEnabled(false);
            }
        }

        public void RespawnAt(Transform spawnPoint)
        {
            if (spawnPoint == null)
            {
                return;
            }

            var target = bodyRoot != null ? bodyRoot : transform;

            // CharacterController caches its own internal position for collision sweeps, separate
            // from Transform.position. Setting the transform directly while it's enabled does not
            // reset that cache, so the very next Move() call (every frame, in
            // PlayerFirstPersonController) silently snaps the character back to wherever the
            // CharacterController itself last resolved a position -- undoing the teleport one frame
            // later with no visible transition. Disabling it around the set is the standard fix.
            var controller = target.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            target.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

            if (controller != null)
            {
                controller.enabled = true;
            }
        }
    }
}
