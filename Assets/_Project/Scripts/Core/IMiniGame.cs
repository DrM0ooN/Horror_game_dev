using System;
using System.Collections.Generic;

#if FUSION_WEAVER
using Fusion;
using PlayerRefType = Fusion.PlayerRef;
#else
using PlayerRefType = System.Int32;
#endif

namespace HorrorGame.Core
{
    /// <summary>
    /// Contract for room mini-games so room flow can stay decoupled from puzzle implementation.
    /// </summary>
    public interface IMiniGame
    {
        /// <summary>
        /// Fired when the mini-game reports a successful completion.
        /// </summary>
        event Action Solved;

        /// <summary>
        /// Fired when the mini-game reports a failure.
        /// </summary>
        event Action Failed;

        /// <summary>
        /// Called by the room controller when the challenge officially starts.
        /// </summary>
        void OnPlayersEntered(List<PlayerRefType> players);

        /// <summary>
        /// Called by the room controller to force a solved transition.
        /// </summary>
        void OnSolved();

        /// <summary>
        /// Called by the room controller to force a failed transition.
        /// </summary>
        void OnFailed();
    }
}
