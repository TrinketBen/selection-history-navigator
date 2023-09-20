using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace SelectionHistory {
    [Serializable]
    struct SelectionRecord {
        public Object Object;
        [FormerlySerializedAs("IsPrefabStageOpen")] public bool   WasPrefabStageOpen;
    }

    class SelectionHistoryNavigator : ScriptableObject {
        [SerializeField] Object _activeSelection;
        [SerializeField] bool _ignoreNextSelectionChangedEvent;
        [SerializeField] List<SelectionRecord> _nextSelections = new();
        [SerializeField] List<SelectionRecord> _previousSelections = new();

        static SelectionHistoryNavigator _instance;

        static SelectionHistoryNavigator Instance {
            get {
                if(_instance != null)
                    return _instance;
                _instance = FindObjectOfType<SelectionHistoryNavigator>();
                if(_instance == null)
                    _instance = CreateInstance<SelectionHistoryNavigator>();
                return _instance;
            }
        }

        [InitializeOnLoadMethod]
        static void Initialize() {
            if(Instance == null)
                return;
            
            Selection.selectionChanged -= Instance.SelectionChangedHandler;
            Selection.selectionChanged += Instance.SelectionChangedHandler;
        }

        void SelectionChangedHandler() {
            Debug.Log($"{nameof(SelectionChangedHandler)}: _ignoreNextSelectionChangedEvent: {_ignoreNextSelectionChangedEvent}, _activeSelection: {_activeSelection}, Selection.activeObject: {Selection.activeObject}");
            if(_ignoreNextSelectionChangedEvent) {
                _ignoreNextSelectionChangedEvent = false;
                return;
            }

            if(_activeSelection != null)
                _previousSelections.Add(GetSelectionRecord());

            _activeSelection = Selection.activeObject;
            _nextSelections.Clear();
        }

        SelectionRecord GetSelectionRecord() {
            return new SelectionRecord {
                Object             = _activeSelection,
                WasPrefabStageOpen = PrefabStageUtility.GetCurrentPrefabStage() != null 
            };
        }

        const string kLabelBackMenu    = "Edit/Selection/Back %-";
        const string kLabelForwardMenu = "Edit/Selection/Forward %+";

        void OpenPrefabStageIfAppropriate(SelectionRecord inRecord) {
            Selection.activeObject    = inRecord.Object;
            Instance._activeSelection = Selection.activeObject;
            if(Selection.activeObject == null)
                return;
            
            if(inRecord.WasPrefabStageOpen)
                AssetDatabase.OpenAsset(Selection.activeObject);
            else
                StageUtility.GoBackToPreviousStage();
        }

        [MenuItem(kLabelBackMenu)]
        static void Back() {
            if(Instance._activeSelection != null)
                Instance._nextSelections.Add(Instance.GetSelectionRecord());

            var record = Instance._previousSelections[^1];
            Instance._previousSelections.RemoveAt(Instance._previousSelections.Count - 1);
            Instance.OpenPrefabStageIfAppropriate(record);
            Instance._ignoreNextSelectionChangedEvent = true;
        }

        [MenuItem(kLabelForwardMenu)]
        static void Forward() {
            if(Instance == null)
                return;
            if(Instance._activeSelection != null)
                Instance._previousSelections.Add(Instance.GetSelectionRecord());
            if(Instance._nextSelections.Count == 0)
                return;

            var record = Instance._nextSelections[^1];
            Instance._nextSelections.RemoveAt(Instance._nextSelections.Count - 1);
            Instance.OpenPrefabStageIfAppropriate(record);
            Instance._ignoreNextSelectionChangedEvent = true;
        }

        [MenuItem(kLabelBackMenu, true)]
        static bool ValidateBack() {
            return Instance._previousSelections.Count > 0;
        }

        [MenuItem(kLabelForwardMenu, true)]
        static bool ValidateForward() {
            return Instance._nextSelections.Count > 0;
        }

        [MenuItem("Edit/Selection/View History Navigator")]
        static void ViewHistoryNavigator() {
            Instance._ignoreNextSelectionChangedEvent = true;
            Selection.activeObject = Instance;
        }

        void OnDestroy() {
            Selection.selectionChanged -= SelectionChangedHandler;
        }
    }
}
