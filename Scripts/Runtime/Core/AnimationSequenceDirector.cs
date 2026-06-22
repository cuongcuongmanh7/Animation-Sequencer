#if DOTWEEN_ENABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    /// <summary>
    /// A single named sequence entry: a key used to trigger it at runtime and the
    /// <see cref="AnimationSequencerController"/> that plays it.
    /// </summary>
    [Serializable]
    public class NamedSequence
    {
        public string key;
        public AnimationSequencerController controller;
    }

    /// <summary>
    /// Triggers several <see cref="AnimationSequencerController"/>s by key from one component.
    /// Typical use: trigger "B"/"C"/… from runtime events on top of a base sequence.
    ///
    /// Playing on enable is intentionally left to each controller's own autoplay mode
    /// (set the desired sequence's controller to autoplay = OnEnable). This director only
    /// handles runtime triggering and, when <see cref="exclusive"/> is on, stops every other
    /// registered sequence before starting the requested one.
    /// </summary>
    public class AnimationSequenceDirector : MonoBehaviour
    {
        #region Fields

        [Tooltip("Named sequences this director can trigger. Each references an AnimationSequencerController.")]
        [SerializeField] private List<NamedSequence> sequences = new List<NamedSequence>();

        [Tooltip("When true, playing a sequence stops every other registered sequence first.")]
        [SerializeField] private bool exclusive = true;

        /// <summary>Key of the sequence started most recently via this director (null if none/stopped).</summary>
        public string CurrentKey { get; private set; }

        #endregion

        #region Lifecycle

        protected virtual void OnDisable()
        {
            // Sequences driven by this director (autoplay = Nothing controllers) won't stop
            // themselves when the object is disabled, so stop them here as a safety net.
            StopAll();
        }

        #endregion

        #region Public API

        /// <summary>Plays the sequence registered under <paramref name="key"/>.</summary>
        public void Play(string key)
        {
            AnimationSequencerController controller = FindController(key);
            if (controller == null)
            {
                Debug.LogWarning($"[AnimationSequenceDirector] No sequence found for key '{key}' on '{name}'.", this);
                return;
            }

            if (exclusive)
                StopOthers(key);

            CurrentKey = key;
            controller.Play();
        }

        /// <summary>Stops the sequence registered under <paramref name="key"/>.</summary>
        public void Stop(string key)
        {
            AnimationSequencerController controller = FindController(key);
            if (controller != null)
                controller.Kill();

            if (CurrentKey == key)
                CurrentKey = null;
        }

        /// <summary>Stops every registered sequence.</summary>
        public void StopAll()
        {
            for (int i = 0; i < sequences.Count; i++)
            {
                AnimationSequencerController controller = sequences[i]?.controller;
                if (controller != null)
                    controller.Kill();
            }

            CurrentKey = null;
        }

        /// <summary>True if the sequence registered under <paramref name="key"/> is currently playing.</summary>
        public bool IsPlaying(string key)
        {
            AnimationSequencerController controller = FindController(key);
            return controller != null && controller.IsPlaying;
        }

        #endregion

        #region Helpers

        private void StopOthers(string exceptKey)
        {
            for (int i = 0; i < sequences.Count; i++)
            {
                NamedSequence sequence = sequences[i];
                if (sequence == null || sequence.controller == null)
                    continue;

                if (sequence.key != exceptKey)
                    sequence.controller.Kill();
            }
        }

        private AnimationSequencerController FindController(string key)
        {
            for (int i = 0; i < sequences.Count; i++)
            {
                NamedSequence sequence = sequences[i];
                if (sequence != null && sequence.key == key)
                    return sequence.controller;
            }

            return null;
        }

        #endregion
    }
}
#endif
