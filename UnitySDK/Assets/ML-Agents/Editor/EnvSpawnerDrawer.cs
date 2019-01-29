using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace MLAgents
{
    /// <summary>
    /// PropertyDrawer for EnvSpawner. Used to display the EnvSpawner in the Inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(EnvSpawner))]
    public class EnvSpawnerDrawer : PropertyDrawer
    {
        private EnvSpawner _envSpawner;
        private int _choiceIndex;
        // The height of a line in the Unity Inspectors
        private const float LineHeight = 17f;
        // The vertical space left below the EnvSpawner UI.
        private const float ExtraSpaceBelow = 10f;
        // The horizontal size of the Control checkbox
        private const int ControlSize = 80;

        /// <summary>
        /// Computes the height of the Drawer depending on the property it is showing
        /// </summary>
        /// <param name="property">The property that is being drawn.</param>
        /// <param name="label">The label of the property being drawn.</param>
        /// <returns>The vertical space needed to draw the property.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            LazyInitialize(property, label);
            var numLines = _envSpawner.Count + 2 + (_envSpawner.Count > 0 ? 1 : 0);
            float height = (numLines) * LineHeight;
            height += 4 * LineHeight; // additional normal height properties
            height += ExtraSpaceBelow;
            return height;
        }

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LazyInitialize(property, label);
            position.height = LineHeight;
            EditorGUI.LabelField(position, new GUIContent(label.text, 
                "The Envenoment Spawner enables spawning 1-many envenoments." +
                "The Envenoment Id and number of envenoments can be specified from the python commant line."));
            position.y += LineHeight;

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.indentLevel++;
            DrawAddRemoveButtons(position);
            position.y += LineHeight;
            
            // This is the labels for each columns
            var halfWidth = position.width / 2;

            var envIdRect = new Rect(
                position.x, position.y, halfWidth, position.height);
            var envPrefabRect = new Rect(
                position.x+halfWidth, position.y, halfWidth, position.height);
            if (_envSpawner.Count > 0)
            {
                EditorGUI.LabelField(envIdRect, "EnvIds");
                envIdRect.y += LineHeight;
                EditorGUI.LabelField(envPrefabRect, "Prefabs");
                envPrefabRect.y += LineHeight;
            }
            position.y = DrawSpawnableEnvDefinition(envIdRect, envPrefabRect);
            // position.y += LineHeight;
            foreach (var item in property)
            {
                SerializedProperty subProp = item as SerializedProperty;
                if (subProp != null) {
                    switch (subProp.name)
                    {
                        case nameof(_envSpawner.envIdDefault):
                            if (_envSpawner.Count > 0)
                            {
                                // EditorGUI.PropertyField(position, subProp);
                                var choices = _envSpawner.spawnableEnvDefinitions
                                    .Where(x=>!string.IsNullOrWhiteSpace(x.envId))
                                    .Select(x=>x.envId).ToList();
                                // choices = new []{string.Empty}.Concat(choices).ToList();
                                if (choices.Contains(_envSpawner.envIdDefault))
                                    _choiceIndex = choices.IndexOf(_envSpawner.envIdDefault);
                                else
                                    _choiceIndex = 0;
                                _choiceIndex = EditorGUI.Popup(position, subProp.displayName, _choiceIndex, choices.ToArray());
                                if (choices.Count >0)
                                    _envSpawner.envIdDefault = choices[_choiceIndex];
                                position.y += LineHeight;
                            }
                            else
                            {
                                _envSpawner.envIdDefault = string.Empty;
                            }
                            break;
                        case nameof(_envSpawner.trainingNumEnvsDefault):
                        case nameof(_envSpawner.inferenceNumEnvsDefault):
                        case nameof(_envSpawner.trainingMode):
                            EditorGUI.PropertyField(position, subProp);
                            position.y += LineHeight;
                            break;
                        default:
                            break;
                    }
                }
            }
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
        
        /// <summary>
        /// Draws the Add and Remove buttons.
        /// </summary>
        /// <param name="position">The position at which to draw.</param>
        private void DrawAddRemoveButtons(Rect position)
        {
            // This is the rectangle for the Add button
            var addButtonRect = position;
            addButtonRect.x += 20;
            if (_envSpawner.Count > 0)
            {
                addButtonRect.width /= 2;
                addButtonRect.width -= 24;
                var buttonContent = new GUIContent(
                    "Add New", "Add a new Environment to the EnvSpawner");
                if (GUI.Button(addButtonRect, buttonContent, EditorStyles.miniButton))
                {
                    MarkSceneAsDirty();
                    AddItem();
                }
                // This is the rectangle for the Remove button
                var removeButtonRect = position;
                removeButtonRect.x = position.width / 2 + 15;
                removeButtonRect.width = addButtonRect.width - 18;
                buttonContent = new GUIContent(
                    "Remove Last", "Remove the last Environment from the EnvSpawner");
                if (GUI.Button(removeButtonRect, buttonContent, EditorStyles.miniButton))
                {
                    MarkSceneAsDirty();
                    RemoveLastItem();
                }
            }
            else
            {
                addButtonRect.width -= 50;
                var buttonContent = new GUIContent(
                    "Add Environment to EnvSpawner", "Add a new EnvId and Environment Prefab to the EnvSpawner");
                if (GUI.Button(addButtonRect, buttonContent, EditorStyles.miniButton))
                {
                    MarkSceneAsDirty();
                    AddItem();
                }
            }
        }

        /// <summary>
        /// Draws a Spawnable Environment.
        /// </summary>
        /// <param name="envIdRect">The Rect to draw the Environment Id.</param>
        /// <param name="envPrefabRect">The Rect to draw the Environment Prefab.</param>
        private float DrawSpawnableEnvDefinition(Rect envIdRect, Rect envPrefabRect)
        {
            foreach (var spawnableEnv in _envSpawner.spawnableEnvDefinitions)
            {
                // This is the rectangle for the envId
                EditorGUI.BeginChangeCheck();
                var newEnvironmentId = EditorGUI.TextField(
                    envIdRect, spawnableEnv.envId);
                envIdRect.y += LineHeight;
                if (EditorGUI.EndChangeCheck())
                {
                    MarkSceneAsDirty();
                    spawnableEnv.envId = newEnvironmentId;
                    break;
                }
                // This is the rectangle for the envPrefab
                EditorGUI.BeginChangeCheck();
                var envPrefab = EditorGUI.ObjectField(
                    envPrefabRect, spawnableEnv.envPrefab, typeof(GameObject), true) as GameObject;
                envPrefabRect.y += LineHeight;
                if (EditorGUI.EndChangeCheck())
                {
                    MarkSceneAsDirty();
                    spawnableEnv.envPrefab = envPrefab;
                    break;
                }
            }
            return envIdRect.y;
        }

        /// <summary>
        /// Lazy initializes the Drawer with the property to be drawn.
        /// </summary>
        /// <param name="property">The SerializedProperty of the EnvironmentSpawner
        /// to make the custom GUI for.</param>
        /// <param name="label">The label of this property.</param>
        private void LazyInitialize(SerializedProperty property, GUIContent label)
        {
            if (_envSpawner != null)
            {
                return;
            }
            var target = property.serializedObject.targetObject;
            _envSpawner = fieldInfo.GetValue(target) as EnvSpawner;
            if (_envSpawner == null)
            {
                _envSpawner = new EnvSpawner();
                fieldInfo.SetValue(target, _envSpawner);
            }
        }
        
        /// <summary>
        /// Signals that the property has been modified and requires the scene to be saved for
        /// the changes to persist. Only works when the Editor is not playing.
        /// </summary>
        private static void MarkSceneAsDirty()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        /// <summary>
        /// Removes the last Environment from the EnvSpawner
        /// </summary>
        private void RemoveLastItem()
        {
            if (_envSpawner.Count > 0)
            {
                _envSpawner.spawnableEnvDefinitions.RemoveAt(_envSpawner.spawnableEnvDefinitions.Count - 1);
            }
        }

        /// <summary>
        /// Adds a new Environment to the EnvSpawner. The value of this brain will not be initialized.
        /// </summary>
        private void AddItem()
        {
            var item = new EnvSpawner.SpawnableEnvDefinition{
                envId = string.Empty
            };
            _envSpawner.spawnableEnvDefinitions.Add(item);
        }
    }
}
