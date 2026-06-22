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
    /// Drives several <see cref="AnimationSequencerController"/>s from one component.
    /// Typical use: play sequence "A" on enable, then trigger "B"/"C"/… from runtime events.
    ///
    /// Each sequence is an ordinary AnimationSequencerController (with the full inspector/preview),
    /// usually placed on child GameObjects. The director disables their autoplay and is the single
    /// runtime entry point. With <see cref="exclusive"/> on, playing a new sequence stops the
    /// currently playing one.
    /// </summary>
    public class AnimationSequenceDirector : MonoBehaviour
    {
        #region Fields

        [Tooltip("Named sequences this director can play. Each references an AnimationSequencerController.")]
        [SerializeField] private List<NamedSequence> sequences = new List<NamedSequence>();

        [Tooltip("Key played automatically in OnEnable. Leave empty to play nothing on enable.")]
        [SerializeField] private string autoPlayKey;

        [Tooltip("When true, playing a sequence stops the one currently playing.")]
        [SerializeField] private bool exclusive = true;

        /// <summary>Key of the sequence started most recently (null if none/stopped).</summary>
        public string CurrentKey { get; private set; }

        #endregion

        #region Lifecycle

        protected virtual void Awake()
        {
            // Only the director should drive the child controllers. Awake runs before any
            // component's OnEnable in the same activation, so this reliably disables their autoplay.
            for (int i = 0; i < sequences.Count; i++)
            {
                AnimationSequencerController controller = sequences[i]?.controller;
                if (controller != null)
                    controller.SetAutoplayMode(AnimationSequencerController.AutoplayType.Nothing);
            }
        }

        protected virtual void OnEnable()
        {
            // Cancel any stray play (e.g. a child still set to autoplay=Awake) before starting.
            StopAll();

            if (!string.IsNullOrEmpty(autoPlayKey))
                Play(autoPlayKey);
        }

        protected virtual void OnDisable()
        {
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

            if (exclusive && !string.IsNullOrEmpty(CurrentKey) && CurrentKey != key)
            {
                AnimationSequencerController currentController = FindController(CurrentKey);
                if (currentController != null && currentController != controller)
                    currentController.Kill();
            }

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
