using System;
using System.Collections.Generic;
using HorrorGame.Core;
using UnityEngine;

#if FUSION_WEAVER
using Fusion;
using PlayerRefType = Fusion.PlayerRef;
#else
using PlayerRefType = System.Int32;
#endif

namespace HorrorGame.MiniGames
{
    /// <summary>
    /// Vertical-slice mini-game: collect fragments while avoiding monster flashlight exposure.
    /// </summary>
    public sealed class FlashlightMiniGame : MonoBehaviour, IMiniGame
    {
        [Header("Objectives")]
        [SerializeField, Min(1)] private int targetFragmentCount = 5;
        [SerializeField] private FlashlightKeyFragment[] fragments;
        [Tooltip("15 static candidate spots; 5 are randomly chosen each run/retry and the fragments are moved there.")]
        [SerializeField] private Transform[] fragmentSpawnPoints;

        [Header("Timing")]
        [SerializeField, Min(5f)] private float baseTimeLimitSeconds = 120f;
        [SerializeField, Min(0f)] private float timePenaltyPerThreat = 4f;
        [SerializeField, Min(5f)] private float finalPhaseSeconds = 20f;

        [Header("Monster Exposure")]
        [SerializeField] private FlashlightMonsterSwitcher monsterSwitcher;
        [SerializeField, Min(0.01f)] private float baseExposureFailThresholdSeconds = 0.45f;
        [SerializeField, Min(0f)] private float exposureThresholdPenaltyPerThreat = 0.02f;
        [SerializeField, Min(0.05f)] private float minimumExposureThresholdSeconds = 0.1f;

        [Header("Threat")]
        [SerializeField] private HubThreatSystem hubThreatSystem;

        private bool isRunning;
        private bool dangerPhaseActive;
        private float remainingTime;
        private float exposureTimer;
        private float activeExposureFailThreshold;
        private int collectedCount;
        private int runThreatAtStart;

        public event Action Solved;
        public event Action Failed;

        public float RemainingTime => remainingTime;
        public bool IsDangerPhaseActive => dangerPhaseActive;

        private void Awake()
        {
            if (fragments == null)
            {
                fragments = Array.Empty<FlashlightKeyFragment>();
            }

            for (var i = 0; i < fragments.Length; i++)
            {
                if (fragments[i] != null)
                {
                    fragments[i].Collected += OnFragmentCollected;
                }
            }

            // So the room already looks populated (fragments spread across 5 of the 15 spawn
            // points) the first time anyone walks in, not just after the challenge is started.
            RandomizeFragmentPositions();
        }

        private void OnDestroy()
        {
            for (var i = 0; i < fragments.Length; i++)
            {
                if (fragments[i] != null)
                {
                    fragments[i].Collected -= OnFragmentCollected;
                }
            }
        }

        private void Update()
        {
            if (!isRunning)
            {
                return;
            }

            remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
            if (!dangerPhaseActive && remainingTime <= finalPhaseSeconds)
            {
                dangerPhaseActive = true;
                ApplyLatePhaseEscalation();
            }

            if (monsterSwitcher != null)
            {
                if (monsterSwitcher.IsLitByFlashlight)
                {
                    exposureTimer += Time.deltaTime;
                    if (exposureTimer >= activeExposureFailThreshold)
                    {
                        OnFailed();
                        return;
                    }
                }
                else
                {
                    exposureTimer = 0f;
                }
            }

            if (remainingTime <= 0f)
            {
                OnFailed();
            }
        }

        /// <summary>
        /// Starts mini-game session after room lock and commitment.
        /// </summary>
        public void OnPlayersEntered(List<PlayerRefType> players)
        {
            _ = players;

            isRunning = true;
            dangerPhaseActive = false;
            exposureTimer = 0f;
            collectedCount = 0;

            runThreatAtStart = hubThreatSystem != null ? hubThreatSystem.ThreatLevel : 0;
            remainingTime = Mathf.Max(10f, baseTimeLimitSeconds - (runThreatAtStart * timePenaltyPerThreat));
            activeExposureFailThreshold = Mathf.Max(
                minimumExposureThresholdSeconds,
                baseExposureFailThresholdSeconds - (runThreatAtStart * exposureThresholdPenaltyPerThreat));

            if (monsterSwitcher != null)
            {
                monsterSwitcher.ResetToStartingAnchor();
                monsterSwitcher.ApplyThreatLevel(runThreatAtStart);
                // Only cycle anchors while a session is actually running -- previously it switched
                // continuously from scene load regardless of whether anyone had entered the room.
                monsterSwitcher.SetActive(true);
            }

            RandomizeFragmentPositions();
            for (var i = 0; i < fragments.Length; i++)
            {
                if (fragments[i] != null)
                {
                    fragments[i].ResetFragment();
                }
            }
        }

        /// <summary>
        /// Moves each fragment to a randomly chosen, distinct spawn point out of the 15 static
        /// candidates. Called on Awake (so the room looks populated before the first start) and
        /// on every OnPlayersEntered (so each run/retry gets a fresh layout).
        /// </summary>
        private void RandomizeFragmentPositions()
        {
            if (fragmentSpawnPoints == null || fragmentSpawnPoints.Length == 0 || fragments == null || fragments.Length == 0)
            {
                return;
            }

            var indices = new List<int>(fragmentSpawnPoints.Length);
            for (var i = 0; i < fragmentSpawnPoints.Length; i++)
            {
                indices.Add(i);
            }

            var pickCount = Mathf.Min(fragments.Length, indices.Count);
            for (var i = 0; i < pickCount; i++)
            {
                var swapIndex = UnityEngine.Random.Range(i, indices.Count);
                (indices[i], indices[swapIndex]) = (indices[swapIndex], indices[i]);

                var point = fragmentSpawnPoints[indices[i]];
                if (fragments[i] != null && point != null)
                {
                    fragments[i].transform.position = point.position;
                }
            }
        }

        /// <summary>
        /// Force-resolves the mini-game.
        /// </summary>
        public void OnSolved()
        {
            if (!isRunning)
            {
                return;
            }

            isRunning = false;
            if (monsterSwitcher != null)
            {
                monsterSwitcher.SetActive(false);
            }

            Solved?.Invoke();
        }

        /// <summary>
        /// Force-fails the mini-game.
        /// </summary>
        public void OnFailed()
        {
            if (!isRunning)
            {
                return;
            }

            isRunning = false;
            if (monsterSwitcher != null)
            {
                monsterSwitcher.SetActive(false);
            }

            Failed?.Invoke();
        }

        private void OnFragmentCollected(FlashlightKeyFragment _)
        {
            if (!isRunning)
            {
                return;
            }

            collectedCount++;
            if (collectedCount >= targetFragmentCount)
            {
                OnSolved();
            }
        }

        private void ApplyLatePhaseEscalation()
        {
            if (monsterSwitcher != null)
            {
                monsterSwitcher.ApplyThreatLevel(runThreatAtStart + 1);
            }
        }
    }
}
