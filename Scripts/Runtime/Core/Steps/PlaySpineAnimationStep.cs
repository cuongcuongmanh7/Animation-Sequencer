#if DOTWEEN_ENABLED
using System;
using DG.Tweening;
using Spine.Unity;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    [Serializable]
    public sealed class PlaySpineAnimationStep : AnimationStepBase
    {
        [SerializeField]
        private SkeletonGraphic skeletonGraphic;
        public SkeletonGraphic SkeletonGraphic
        {
            get => skeletonGraphic;
            set => skeletonGraphic = value;
        }

        [SerializeField, SpineAnimation(dataField: "skeletonGraphic", fallbackToTextField: true)]
        private string animationName;
        public string AnimationName
        {
            get => animationName;
            set => animationName = value;
        }

        [SerializeField]
        private bool loop;
        public bool Loop
        {
            get => loop;
            set => loop = value;
        }

        [NonSerialized]
        private Spine.TrackEntry currentTrackEntry;

        public override string DisplayName => "Play Animation Spine";

        public override void AddTweenToSequence(Sequence animationSequence)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.SetDelay(Delay);
            sequence.AppendCallback(PlayAnimation);

            float duration = GetAnimationDuration();
#if UNITY_EDITOR
            if (!Application.isPlaying && duration > 0)
            {
                sequence.Append(DOVirtual.Float(0, duration, duration, PreviewAnimation)
                    .SetEase(Ease.Linear));
            }
            else
#endif
            if (duration > 0)
            {
                sequence.AppendInterval(duration);
            }

            if (FlowType == FlowType.Join)
                animationSequence.Join(sequence);
            else
                animationSequence.Append(sequence);
        }

        public override void ResetToInitialState()
        {
            currentTrackEntry = null;
            if (!TryInitializeSkeleton())
                return;

            Spine.AnimationState animationState = skeletonGraphic.AnimationState;
            animationState.ClearTracks();
            skeletonGraphic.Skeleton.SetToSetupPose();

            if (!string.IsNullOrEmpty(skeletonGraphic.startingAnimation))
            {
                Spine.Animation startingAnimation = skeletonGraphic.SkeletonData.FindAnimation(
                    skeletonGraphic.startingAnimation);
                if (startingAnimation != null)
                    animationState.SetAnimation(0, startingAnimation, skeletonGraphic.startingLoop);
            }

            skeletonGraphic.Update(0);
            skeletonGraphic.LateUpdate();
        }

        private void PlayAnimation()
        {
            if (!TryInitializeSkeleton() || !TryGetAnimation(out Spine.Animation animation))
                return;

            currentTrackEntry = skeletonGraphic.AnimationState.SetAnimation(0, animation, loop);
            skeletonGraphic.Update(0);
            skeletonGraphic.LateUpdate();
        }

        private void PreviewAnimation(float elapsed)
        {
            if (currentTrackEntry == null)
                PlayAnimation();
            if (currentTrackEntry == null || !TryInitializeSkeleton())
                return;

            currentTrackEntry.TrackTime = elapsed;
            skeletonGraphic.AnimationState.Apply(skeletonGraphic.Skeleton);
            skeletonGraphic.Skeleton.UpdateWorldTransform();
            skeletonGraphic.LateUpdate();
        }

        private float GetAnimationDuration()
        {
            return TryGetAnimation(out Spine.Animation animation) ? animation.Duration : 0;
        }

        private bool TryGetAnimation(out Spine.Animation animation)
        {
            animation = null;
            if (skeletonGraphic == null || skeletonGraphic.skeletonDataAsset == null ||
                string.IsNullOrEmpty(animationName))
                return false;

            Spine.SkeletonData skeletonData = skeletonGraphic.skeletonDataAsset.GetSkeletonData(true);
            if (skeletonData == null)
                return false;

            animation = skeletonData.FindAnimation(animationName);
            return animation != null;
        }

        private bool TryInitializeSkeleton()
        {
            if (skeletonGraphic == null || skeletonGraphic.skeletonDataAsset == null)
                return false;

            skeletonGraphic.Initialize(false);
            return skeletonGraphic.IsValid;
        }

        public void SetTarget(SkeletonGraphic newTarget)
        {
            skeletonGraphic = newTarget;
        }

        public override string GetDisplayNameForEditor(int index)
        {
            string targetName = skeletonGraphic == null ? "NULL" : skeletonGraphic.name;
            string selectedAnimation = string.IsNullOrEmpty(animationName) ? "NULL" : animationName;
            return $"{index}. Play {targetName} Spine animation '{selectedAnimation}'";
        }
    }
}
#endif
