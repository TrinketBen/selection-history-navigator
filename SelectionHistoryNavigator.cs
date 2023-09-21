using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace SelectionHistory {
    [Serializable]
    struct SelectionRecord {
        public UObject Object;
        public string  Path;
        public UObject AssetSource;
        public bool    WasPrefabStageOpen;
    }

    class SelectionHistoryNavigator : ScriptableObject {
        [SerializeField] UObject _activeSelection;
        [SerializeField] bool _ignoreNext;
        [SerializeField] bool _wasPrefabStageOpen;
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
        static void Initialize()
        {
            var _ = Instance;
        }

        void OnEnable()
        {
            Selection.selectionChanged -= Instance.SelectionChanged;
            Selection.selectionChanged += Instance.SelectionChanged;
        }

        void SelectionChanged() {
            Debug.Log($"{nameof(SelectionChanged)}: IgnoreNext: {_ignoreNext}, Previous: {_activeSelection}, Current: {Selection.activeObject}", Selection.activeContext);
            if(_ignoreNext) {
                _ignoreNext = false;
                return;
            }

            if(_activeSelection != null)
                _previousSelections.Add(GetSelectionRecord());

            _wasPrefabStageOpen = PrefabStageUtility.GetCurrentPrefabStage() != null;
            _activeSelection    = Selection.activeObject;
            _nextSelections.Clear();
        }

        SelectionRecord GetSelectionRecord()
        {
            var prefabAssetPath = PrefabStageUtility.GetCurrentPrefabStage()?.assetPath;
            return new SelectionRecord {
                Object             = _activeSelection,
                Path               = GetHierarchyPath((_activeSelection as GameObject)?.transform),
                AssetSource        = !string.IsNullOrEmpty(prefabAssetPath) ?
                    AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath) :
                    null,
                WasPrefabStageOpen = _wasPrefabStageOpen 
            };
        }

        const string kLabelBackMenu    = "Edit/Selection/Back &-";
        const string kLabelForwardMenu = "Edit/Selection/Forward &=";

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
            Instance._ignoreNext = true;
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
            Instance._ignoreNext = true;
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
            Instance._ignoreNext = true;
            Selection.activeObject = Instance;
        }

        void OnDisable() {
            Selection.selectionChanged -= SelectionChanged;
        }
        
        string GetHierarchyPath(Transform inTransform) {
            if (!inTransform)
                return string.Empty;
            
            var path = inTransform.name;
            while(inTransform && inTransform.transform.parent) {
                inTransform = inTransform.transform.parent;
                path = inTransform.name + "/" + path;
            }
            return path;
        }
    }
}
