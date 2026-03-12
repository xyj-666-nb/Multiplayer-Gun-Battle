using UnityEditor;
using UnityEngine;

namespace Localization.Editor
{
    [CustomEditor(typeof(LocalizedText))]
    public class LocalizedTextEditor : UnityEditor.Editor
    {
        private LocalizedText _target;

        private void OnEnable()
        {
            _target = (LocalizedText)target;
            if (_target.UniqueId == 0) AssignId();
            ValidateList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("uniqueId"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("localizedStrings"), true);

            serializedObject.ApplyModifiedProperties();
        }

        private void AssignId()
        {
            int nextId = EditorPrefs.GetInt("Localization_NextId", 1);
            _target.SetId(nextId);
            EditorPrefs.SetInt("Localization_NextId", nextId + 1);
            EditorUtility.SetDirty(_target);
        }

        private void ValidateList()
        {
            int enumLength = System.Enum.GetValues(typeof(Language)).Length;
            if (_target.LocalizedStrings.Count != enumLength)
            {
                _target.InitListByEnum();
                EditorUtility.SetDirty(_target);
            }
        }
    }
}