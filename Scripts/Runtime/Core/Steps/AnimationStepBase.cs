#if DOTWEEN_ENABLED
using System;
using DG.Tweening;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    [Serializable]
    public abstract class AnimationStepBase
    {
        [SerializeField]
        private bool active = true;
        /// <summary>
        /// When false, the step is muted: it is skipped while building the sequence
        /// (no delay, tween or callback) and is not reset to its initial state.
        /// Defaults to true so steps serialized before this field existed stay enabled.
        /// </summary>
        public bool IsActive { get => active; set => active = value; }

        [SerializeField]
        private float delay;
        public float Delay => delay;

        [SerializeField]
        private FlowType flowType;
        public FlowType FlowType => flowType;

        public abstract string DisplayName { get; }
        
        public abstract void AddTweenToSequence(Sequence animationSequence);

        public abstract void ResetToInitialState();

        public virtual string GetDisplayNameForEditor(int index)
        {
            return $"{index}. {this}";
        }

        public bool IsSkippingToEnd { get; set; }
    }
}
#endif