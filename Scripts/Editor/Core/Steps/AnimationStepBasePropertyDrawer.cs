#if DOTWEEN_ENABLED
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

namespace BrunoMikoski.AnimationSequencer
{
    [CustomPropertyDrawer(typeof(AnimationStepBase), true)]
    public class AnimationStepBasePropertyDrawer : PropertyDrawer
    {
        protected void DrawBaseGUI(Rect position, SerializedProperty property, GUIContent label, params string[] excludedPropertiesNames)
        {
            float buttonsY = position.y + 2;
            float buttonHeight = EditorGUIUtility.singleLineHeight - 1;

            // Duplicate (icon) and Delete (X) buttons, pinned to the right edge of the row.
            // Both changes go through the SerializedObject, so they are undoable with Ctrl+Z.
            const float buttonSize = 22f;
            const float buttonGap = 2f;
            Rect deleteRect = new Rect(position.xMax - buttonSize, buttonsY, buttonSize, buttonHeight);
            Rect duplicateRect = new Rect(deleteRect.x - buttonGap - buttonSize, buttonsY, buttonSize, buttonHeight);

            GUIContent duplicateContent = new GUIContent(EditorGUIUtility.IconContent("TreeEditor.Duplicate")) { tooltip = "Duplicate step" };
            if (GUI.Button(duplicateRect, duplicateContent))
            {
                DuplicateProperty(property);
                // The backing array changed during this OnGUI pass; abort it so the
                // ReorderableList does not keep iterating stale/!valid elements this frame.
                GUIUtility.ExitGUI();
            }

            Color previousBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.92f, 0.42f, 0.42f);
            bool deleteClicked = GUI.Button(deleteRect, new GUIContent("X", "Delete this step"));
            GUI.backgroundColor = previousBackgroundColor;
            if (deleteClicked)
            {
                DeleteProperty(property);
                // The element was removed; abort the current OnGUI pass so the
                // ReorderableList stops drawing the now-disposed SerializedProperty.
                GUIUtility.ExitGUI();
            }

            float originY = position.y;

            position.height = EditorGUIUtility.singleLineHeight;

            // Mute toggle in the right cluster, just left of the Duplicate button. The far-left
            // area competes with the reorderable-list drag handle and the foldout's click region
            // (clicks there toggle the foldout instead), so the toggle lives among the right-side
            // buttons where GUI controls capture clicks reliably.
            bool stepActive = true;
            Rect foldoutRect = position;
            SerializedProperty activeProperty = property.FindPropertyRelative("active");
            if (activeProperty != null)
            {
                stepActive = activeProperty.boolValue;

                // Use GUI.Button rather than EditorGUI.Toggle: inside the ReorderableList element
                // the toggle didn't reliably receive clicks (they fell through to the foldout),
                // whereas buttons (like Duplicate/Delete) do. Red background signals muted.
                Rect muteRect = new Rect(duplicateRect.x - 38, buttonsY, 28, buttonHeight);
                Color previousMuteBackground = GUI.backgroundColor;
                if (!stepActive)
                    GUI.backgroundColor = new Color(0.92f, 0.42f, 0.42f);

                GUIContent muteContent = new GUIContent(stepActive ? "On" : "Off",
                    stepActive ? "Enabled — click to mute (skip this step)" : "Muted — click to enable");
                if (GUI.Button(muteRect, muteContent, EditorStyles.miniButton))
                {
                    activeProperty.boolValue = !stepActive;
                    property.serializedObject.ApplyModifiedProperties();
                    stepActive = activeProperty.boolValue;
                }

                GUI.backgroundColor = previousMuteBackground;

                foldoutRect.xMax = muteRect.x - 4; // keep the label clear of the mute + buttons
            }
            else
            {
                foldoutRect.xMax = duplicateRect.x - 4;
            }

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true, EditorStyles.foldout);

            if (property.isExpanded)
            {
                EditorGUI.BeginChangeCheck();

                EditorGUI.indentLevel++;
                position = EditorGUI.IndentedRect(position);
                EditorGUI.indentLevel--;
                
                position.height = EditorGUIUtility.singleLineHeight;
                position.y +=  EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // Grey out the body of a muted step (the mute toggle itself stays enabled above).
                bool wasGUIEnabled = GUI.enabled;
                GUI.enabled = stepActive;

                foreach (SerializedProperty serializedProperty in property.GetChildren())
                {
                    // 'active' is surfaced as the header mute toggle, not in the body.
                    if (serializedProperty.name.Equals("active", StringComparison.Ordinal))
                        continue;

                    bool shouldDraw = true;
                    for (int i = 0; i < excludedPropertiesNames.Length; i++)
                    {
                        string excludedPropertyName = excludedPropertiesNames[i];
                        if (serializedProperty.name.Equals(excludedPropertyName, StringComparison.Ordinal))
                        {
                            shouldDraw = false;
                            break;
                        }
                    }

                    if (!shouldDraw)
                        continue;

                    EditorGUI.PropertyField(position, serializedProperty);
                    position.y += EditorGUI.GetPropertyHeight(serializedProperty) + EditorGUIUtility.standardVerticalSpacing;

                }

                GUI.enabled = wasGUIEnabled;

                if (EditorGUI.EndChangeCheck())
                    property.serializedObject.ApplyModifiedProperties();
            }
            
            property.SetPropertyDrawerHeight(position.y - originY + EditorGUIUtility.singleLineHeight);
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawBaseGUI(position, property, label);
        }
    
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.GetPropertyDrawerHeight();
        }

        private void DuplicateProperty(SerializedProperty property)
        {
            SerializedProperty parentArray = GetParentArrayProperty(property);
            if (parentArray != null && parentArray.isArray)
            {
                int index = GetIndexInArray(property);

                object sourceObject = property.managedReferenceValue;
                object clonedObject = CloneManagedReference(sourceObject);

                if (clonedObject != null)
                {
                    parentArray.InsertArrayElementAtIndex(index);

                    var newElement = parentArray.GetArrayElementAtIndex(index + 1);
                    newElement.managedReferenceValue = clonedObject;

                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void DeleteProperty(SerializedProperty property)
        {
            SerializedProperty parentArray = GetParentArrayProperty(property);
            if (parentArray != null && parentArray.isArray)
            {
                int index = GetIndexInArray(property);
                if (index >= 0 && index < parentArray.arraySize)
                {
                    parentArray.DeleteArrayElementAtIndex(index);
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private SerializedProperty GetParentArrayProperty(SerializedProperty property)
        {
            string path = property.propertyPath;
            int lastDot = path.LastIndexOf('.');
            if (lastDot < 0)
                return null;

            string arrayPath = path.Substring(0, lastDot);
            return property.serializedObject.FindProperty(arrayPath);
        }

        private int GetIndexInArray(SerializedProperty property)
        {
            string path = property.propertyPath;
            int start = path.IndexOf("[") + 1;
            int end = path.IndexOf("]");
            string indexStr = path.Substring(start, end - start);
            return int.Parse(indexStr);
        }

        private static FieldInfo[] GetAllFieldsIncludingBaseTypes(Type type, BindingFlags flags)
        {
            List<FieldInfo> fields = new List<FieldInfo>();
            while (type != null)
            {
                fields.AddRange(type.GetFields(flags | BindingFlags.DeclaredOnly));
                type = type.BaseType;
            }
            return fields.ToArray();
        }

        public static object CloneManagedReference(object obj, int depth = 2)
        {
            if (obj == null) return null;

            if (depth == 0) return obj;

            Type type = obj.GetType();
            object clone = System.Activator.CreateInstance(type);

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo[] fields = GetAllFieldsIncludingBaseTypes(type , flags);// type.GetFields(flags);

            foreach (FieldInfo field in fields)
            {
                // Skip private/protected fields without [SerializeField] or [SerializeReference]
                if (!field.IsPublic)
                {
                    var isSeialized = field.GetCustomAttribute<SerializeField>() != null || field.GetCustomAttribute<SerializeReference>() != null;
                    if (!isSeialized)
                        continue;
                }

                var value = field.GetValue(obj);
                if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var list = (IList)value;
                    var clonedList = (IList)Activator.CreateInstance(field.FieldType);
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            var clonedItem = CloneManagedReference(item, depth - 1);
                            clonedList.Add(clonedItem);
                        }
                    }
                    field.SetValue(clone, clonedList);
                }
                else if (field.FieldType.IsArray)
                {
                    var elementType = field.FieldType.GetElementType();
                    var array = (Array)value;
                    var clonedArray = Array.CreateInstance(elementType, array.Length);
                    for (int i = 0; i < array.Length; i++)
                    {
                        var clonedItem = CloneManagedReference(array.GetValue(i), depth - 1);
                        clonedArray.SetValue(clonedItem, i);
                    }
                    field.SetValue(clone, clonedArray);
                }
                else if (typeof(UnityEventBase).IsAssignableFrom(field.FieldType))
                {
                    var originalEvent = (UnityEventBase)value;
                    var clonedEvent = Activator.CreateInstance(field.FieldType) as UnityEventBase;

                    if (originalEvent is UnityEvent originalUnityEvent && clonedEvent is UnityEvent clonedUnityEvent)
                    {
                        int count = originalUnityEvent.GetPersistentEventCount();
                        for (int i = 0; i < count; i++)
                        {
                            var target = originalUnityEvent.GetPersistentTarget(i);
                            var methodName = originalUnityEvent.GetPersistentMethodName(i);

                            if (target != null && !string.IsNullOrEmpty(methodName))
                            {
                                var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                if (method != null)
                                {
                                    var action = Delegate.CreateDelegate(typeof(UnityAction), target, method, false) as UnityAction;
                                    if (action != null)
                                    {
                                        UnityEventTools.AddPersistentListener(clonedUnityEvent, action);
                                    }
                                }
                            }
                        }

                        field.SetValue(clone, clonedUnityEvent);
                    }
                    else
                    {
                        field.SetValue(clone, clonedEvent);
                    }
                }
                else if (IsManagedReferenceField(field))
                {
                    var duplicate = CloneManagedReference(value, depth - 1);
                    field.SetValue(clone, duplicate);
                }
                else
                {
                    field.SetValue(clone, value);
                }
                
            }

            return clone;
        }

        private static bool IsManagedReferenceField(FieldInfo field)
        {
            Type fieldType = field.FieldType;

            // Never deep-clone Unity objects (GameObject/Transform/Component/etc.): they are
            // scene/asset references and must be copied by reference. The previous exact-type
            // check missed subclasses like GameObject, so the clone recursed into them and broke
            // the step's target (e.g. a duplicate ended up sharing/losing the source target).
            if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                return false;

            // If it's object, abstract class, interface, or not sealed
            if (fieldType == typeof(object))
                return true;

            if (fieldType.IsAbstract || fieldType.IsInterface)
                return true;

            if (!fieldType.IsSealed && !fieldType.IsValueType)
                return true; // non-sealed class (polymorphic)

            return false;
        }
    }
}
#endif