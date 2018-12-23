using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace MLAgents
{
    /// <summary>
    /// PropertyDrawer for AgentSpawner. Used to display the AgentSpawner in the Inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(AgentSpawner))]
    public class AgentSpawnerDrawer : PropertyDrawer
    {
        private AgentSpawner _agentSpawner;
        private int _choiceIndex;
        // The height of a line in the Unity Inspectors
        private const float LineHeight = 17f;
        // The vertical space left below the AgentSpawner UI.
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
            var numLines = _agentSpawner.Count + 2 + (_agentSpawner.Count > 0 ? 1 : 0);
            float height = (numLines) * LineHeight;
            height += 6 * LineHeight; // additional normal height properties
            height += 2 * LineHeight * 0; // additional tripple height properties
            height += ExtraSpaceBelow;
            return height;
        }

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LazyInitialize(property, label);
            position.height = LineHeight;
            EditorGUI.LabelField(position, new GUIContent(label.text, 
                "The Agent Spawner enables spawning 1-many agents." +
                "The Agent Id and number of agents can be specified from the python commant line."));
            position.y += LineHeight;

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.indentLevel++;
            DrawAddRemoveButtons(position);
            position.y += LineHeight;
            
            // This is the labels for each columns
            var halfWidth = position.width / 2;

            var agentIdRect = new Rect(
                position.x, position.y, halfWidth, position.height);
            var agentPrefabRect = new Rect(
                position.x+halfWidth, position.y, halfWidth, position.height);
            if (_agentSpawner.Count > 0)
            {
                EditorGUI.LabelField(agentIdRect, "AgentIds");
                agentIdRect.y += LineHeight;
                EditorGUI.LabelField(agentPrefabRect, "Prefabs");
                agentPrefabRect.y += LineHeight;
            }
            position.y = DrawSpawnableAgents(agentIdRect, agentPrefabRect);
            // position.y += LineHeight;
            foreach (var item in property)
            {
                SerializedProperty subProp = item as SerializedProperty;
                if (subProp != null) {
                    switch (subProp.name)
                    {
                        case nameof(_agentSpawner.agentIdDefault):
                            if (_agentSpawner.Count > 0)
                            {
                                // EditorGUI.PropertyField(position, subProp);
                                var choices = _agentSpawner.spawnableAgents
                                    .Where(x=>!string.IsNullOrWhiteSpace(x.agentId))
                                    .Select(x=>x.agentId).ToList();
                                // choices = new []{string.Empty}.Concat(choices).ToList();
                                if (choices.Contains(_agentSpawner.agentIdDefault))
                                    _choiceIndex = choices.IndexOf(_agentSpawner.agentIdDefault);
                                else
                                    _choiceIndex = 0;
                                _choiceIndex = EditorGUI.Popup(position, subProp.displayName, _choiceIndex, choices.ToArray());
                                _agentSpawner.agentIdDefault = choices[_choiceIndex];
                                position.y += LineHeight;
                            }
                            else
                            {
                                _agentSpawner.agentIdDefault = string.Empty;
                            }
                            break;
                        case nameof(_agentSpawner.trainingNumAgentsDefault):
                        case nameof(_agentSpawner.inferenceNumAgentsDefault):
                        case nameof(_agentSpawner.trainingMode):
                        case nameof(_agentSpawner.worldBoundsMaxOffset):
                        case nameof(_agentSpawner.worldBoundsMinOffset):
                        // case nameof(_agentSpawner.agentBoundsMinOffset):
                        // case nameof(_agentSpawner.agentBoundsMaxOffset):
                            EditorGUI.PropertyField(position, subProp);
                            position.y += LineHeight;
                            break;
                        // case nameof(_agentSpawner.spawnedAgentBounds):
                        // case nameof(_agentSpawner.worldBounds):
                        //     EditorGUI.PropertyField(position, subProp);
                        //     position.y += LineHeight * 3;
                        //     break;
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
            if (_agentSpawner.Count > 0)
            {
                addButtonRect.width /= 2;
                addButtonRect.width -= 24;
                var buttonContent = new GUIContent(
                    "Add New", "Add a new Agent to the Agent Spawner");
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
                    "Remove Last", "Remove the last Agent from the Agent Spawner");
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
                    "Add Brain to Broadcast Hub", "Add a new Brain to the Broadcast Hub");
                if (GUI.Button(addButtonRect, buttonContent, EditorStyles.miniButton))
                {
                    MarkSceneAsDirty();
                    AddItem();
                }
            }
        }

        /// <summary>
        /// Draws a Spawnable Agent.
        /// </summary>
        /// <param name="agentIdRect">The Rect to draw the AgentId.</param>
        /// <param name="agentPrefabRect">The Rect to draw the AgentPrefab.</param>
        private float DrawSpawnableAgents(Rect agentIdRect, Rect agentPrefabRect)
        {
            foreach (var spawnableAgent in _agentSpawner.spawnableAgents)
            {
                // This is the rectangle for the agentId
                EditorGUI.BeginChangeCheck();
                var newAgentId = EditorGUI.TextField(
                    agentIdRect, spawnableAgent.agentId);
                agentIdRect.y += LineHeight;
                if (EditorGUI.EndChangeCheck())
                {
                    MarkSceneAsDirty();
                    spawnableAgent.agentId = newAgentId;
                    break;
                }
                // This is the rectangle for the agentPrefab
                EditorGUI.BeginChangeCheck();
                var agentPrefab = EditorGUI.ObjectField(
                    agentPrefabRect, spawnableAgent.agentPrefab, typeof(Agent), true) as Agent;
                agentPrefabRect.y += LineHeight;
                if (EditorGUI.EndChangeCheck())
                {
                    MarkSceneAsDirty();
                    spawnableAgent.agentPrefab = agentPrefab;
                    break;
                }
            }
            return agentIdRect.y;
        }

        /// <summary>
        /// Lazy initializes the Drawer with the property to be drawn.
        /// </summary>
        /// <param name="property">The SerializedProperty of the AgentSpawner
        /// to make the custom GUI for.</param>
        /// <param name="label">The label of this property.</param>
        private void LazyInitialize(SerializedProperty property, GUIContent label)
        {
            if (_agentSpawner != null)
            {
                return;
            }
            var target = property.serializedObject.targetObject;
            _agentSpawner = fieldInfo.GetValue(target) as AgentSpawner;
            if (_agentSpawner == null)
            {
                _agentSpawner = new AgentSpawner();
                fieldInfo.SetValue(target, _agentSpawner);
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
        /// Removes the last Agent from the AgentSpawner
        /// </summary>
        private void RemoveLastItem()
        {
            if (_agentSpawner.Count > 0)
            {
                _agentSpawner.spawnableAgents.RemoveAt(_agentSpawner.spawnableAgents.Count - 1);
            }
        }

        /// <summary>
        /// Adds a new Agent to the AgentSpawner. The value of this brain will not be initialized.
        /// </summary>
        private void AddItem()
        {
            var item = new AgentSpawner.SpawnableAgent{
                agentId = string.Empty
            };
            _agentSpawner.spawnableAgents.Add(item);
        }
    }
}
