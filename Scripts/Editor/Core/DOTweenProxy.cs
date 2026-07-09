#if DOTWEEN_ENABLED
using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using DG.Tweening.Core;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    internal static class DOTweenProxy
    {
        private static FieldInfo sequencedObjects;
        private static FieldInfo sequencedPosition;
        private static FieldInfo sequencedEndPosition;

        [InitializeOnLoadMethod]
        private static void Setup()
        {
            try
            {
                sequencedObjects = typeof(Sequence)
                    .GetField("_sequencedObjs", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                sequencedPosition = typeof(ABSSequentiable)
                    .GetField("sequencedPosition",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                sequencedEndPosition = typeof(ABSSequentiable)
                    .GetField("sequencedEndPosition",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static (float start, float end)[] GetTimings(Sequence sequence, AnimationStepBase[] steps)
        {
            try
            {
                var timings = new (float start, float end)[steps.Length];
                var objs = GetSequencedObjects(sequence);
                var duration = sequence.Duration();

                // Only active (non-muted) steps are added to the sequence, so the sequenced
                // objects align with active steps plus the two bookend SequenceCallbacks added
                // in AnimationSequencerController.GenerateSequence.
                int activeCount = 0;
                for (int i = 0; i < steps.Length; i++)
                {
                    if (steps[i] != null && steps[i].IsActive)
                        activeCount++;
                }

                if (objs == null || objs.Count != activeCount + 2)
                {
                    Debug.LogError("Sequenced object count mismatch for sequence");
                    return null;
                }

                int objIndex = 1; // skip the Start Callback
                for (var index = 0; index < timings.Length; index++)
                {
                    var step = steps[index];

                    // Muted steps contribute nothing to the sequence: no timing bar.
                    if (step == null || !step.IsActive)
                    {
                        timings[index] = (0f, 0f);
                        continue;
                    }

                    var obj = objs[objIndex];
                    objIndex++;

                    var start = GetSequencedStartPosition(obj);
                    var end = GetSequencedEndPosition(obj);

                    start += step.Delay;

                    timings[index] = (start / duration, end / duration);
                }

                return timings;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        private static List<ABSSequentiable> GetSequencedObjects(Sequence sequence)
        {
            return sequencedObjects?.GetValue(sequence) as List<ABSSequentiable>;
        }

        private static float GetSequencedStartPosition(ABSSequentiable sequenced)
        {
            return (float) sequencedPosition.GetValue(sequenced);
        }

        private static float GetSequencedEndPosition(ABSSequentiable sequenced)
        {
            return (float) sequencedEndPosition.GetValue(sequenced);
        }
    }
}
#endif