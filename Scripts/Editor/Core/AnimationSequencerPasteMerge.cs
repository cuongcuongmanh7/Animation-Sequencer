#if DOTWEEN_ENABLED
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    /// <summary>
    /// Adds a "Paste Steps (Merge)" item to the AnimationSequencerController component context
    /// menu. It reads the steps from a component copied with Unity's built-in "Copy Component"
    /// and appends deep clones of them to the target's existing steps (merge, not replace).
    /// </summary>
    public static class AnimationSequencerPasteMerge
    {
        private const string MenuPath = "CONTEXT/AnimationSequencerController/Paste Steps (Merge)";
        private const string AnimationStepsField = "animationSteps";

        [MenuItem(MenuPath)]
        private static void PasteStepsMerge(MenuCommand command)
        {
            AnimationSequencerController target = command.context as AnimationSequencerController;
            if (target == null)
                return;

            // ComponentUtility can only paste onto a component, not read the copy buffer directly,
            // so paste onto a throwaway instance to read its steps, then discard it.
            GameObject tempGameObject = new GameObject("__AnimationSequencerPasteTemp")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            try
            {
                AnimationSequencerController temp = tempGameObject.AddComponent<AnimationSequencerController>();
                if (!ComponentUtility.PasteComponentValues(temp))
                {
                    Debug.LogWarning("[AnimationSequencer] Nothing to paste. Use 'Copy Component' on the source AnimationSequencerController first.");
                    return;
                }

                AnimationStepBase[] sourceSteps = temp.AnimationSteps;
                if (sourceSteps == null || sourceSteps.Length == 0)
                {
                    Debug.LogWarning("[AnimationSequencer] The copied component has no steps to merge.");
                    return;
                }

                SerializedObject serializedObject = new SerializedObject(target);
                SerializedProperty stepsProperty = serializedObject.FindProperty(AnimationStepsField);
                if (stepsProperty == null || !stepsProperty.isArray)
                    return;

                int merged = 0;
                foreach (AnimationStepBase sourceStep in sourceSteps)
                {
                    // Deep clone so the merged steps are independent of the copy buffer.
                    object clone = AnimationStepBasePropertyDrawer.CloneManagedReference(sourceStep);
                    if (clone == null)
                        continue;

                    int insertIndex = stepsProperty.arraySize;
                    stepsProperty.InsertArrayElementAtIndex(insertIndex);
                    stepsProperty.GetArrayElementAtIndex(insertIndex).managedReferenceValue = clone;
                    merged++;
                }

                serializedObject.ApplyModifiedProperties();
                Debug.Log($"[AnimationSequencer] Merged {merged} step(s) into '{target.name}'.", target);
            }
            finally
            {
                Object.DestroyImmediate(tempGameObject);
            }
        }
    }
}
#endif
