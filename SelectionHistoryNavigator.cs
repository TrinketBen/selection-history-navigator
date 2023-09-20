using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SelectionHistory {
    /// <summary>
    /// Adds Back and Forward items to the Edit > Selection menu to navigate between Hierarchy and Project pane selections.
    /// </summary>
    [InitializeOnLoad]
    static class SelectionHistoryNavigator {
        static Object activeSelection;
        static bool ignoreNextSelectionChangedEvent;
        static readonly Stack<Object> nextSelections = new Stack<Object>();
        static readonly Stack<Object> previousSelections = new Stack<Object>();

        static SelectionHistoryNavigator() {
            Selection.selectionChanged += SelectionChangedHandler;
        }

        static void SelectionChangedHandler() {
            if(ignoreNextSelectionChangedEvent) {
                ignoreNextSelectionChangedEvent = false;
                return;
            }

            if(activeSelection != null) {
                previousSelections.Push(activeSelection);
            }

            activeSelection = Selection.activeObject;
            nextSelections.Clear();
        }

        const string backMenuLabel = "Edit/Selection/Back %[";
        const string forwardMenuLabel = "Edit/Selection/Forward %]";

        static void OpenPrefabStageIfAppropriate() {
            if(Selection.activeObject != null && PrefabUtility.IsPartOfPrefabAsset(Selection.activeObject)) {
                AssetDatabase.OpenAsset(Selection.activeObject);
            }
        }

        [MenuItem(backMenuLabel)]
        static void Back() {
            if(activeSelection != null) {
                nextSelections.Push(activeSelection);
            }

            Selection.activeObject = previousSelections.Pop();
            activeSelection = Selection.activeObject;
            OpenPrefabStageIfAppropriate();
            ignoreNextSelectionChangedEvent = true;
        }

        [MenuItem(forwardMenuLabel)]
        static void Forward() {
            if(activeSelection != null) {
                previousSelections.Push(activeSelection);
            }

            Selection.activeObject = nextSelections.Pop();
            activeSelection = Selection.activeObject;
            OpenPrefabStageIfAppropriate();
            ignoreNextSelectionChangedEvent = true;
        }

        [MenuItem(backMenuLabel, true)]
        static bool ValidateBack() {
            return previousSelections.Count > 0;
        }

        [MenuItem(forwardMenuLabel, true)]
        static bool ValidateForward() {
            return nextSelections.Count > 0;
        }
    }
}
