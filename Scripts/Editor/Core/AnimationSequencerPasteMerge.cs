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

            // ComponentUtility can't read the copy buffer directly, so paste the copied component
            // onto a throwaway GameObject to read its steps, then discard it. PasteComponentAsNew
            // adds the copied component fresh (more reliable than AddComponent + PasteComponentValues).
            GameObject tempGameObject = new GameObject("__AnimationSequencerPasteTemp");
            tempGameObject.hideFlags = HideFlags.HideInHierarchy;

            try
            {
                if (!ComponentUtility.PasteComponentAsNew(tempGameObject))
                {
                    Debug.LogWarning("[AnimationSequencer] Nothing to paste. Use 'Copy Component' on the source AnimationSequencerController first.");
                    return;
                }

                AnimationSequencerController temp = tempGameObject.GetComponent<AnimationSequencerController>();
                if (temp == null)
                {
                    Debug.LogWarning("[AnimationSequencer] The copied component is not an AnimationSequencerController.");
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
