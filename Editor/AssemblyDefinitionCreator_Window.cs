using UnityEngine;
using UnityEditor;

using static NaxtorGames.AssemblyDefinitionCreator.AssemblyDefinitionCreator;

namespace NaxtorGames.AssemblyDefinitionCreator.EditorScripts
{
    public sealed class AssemblyDefinitionCreator_Window : EditorWindow
    {
        private const string TOOL_NAME = "Assembly Definition Creator";
        private const string ASSETS_ROOT = "Assets/";
        private const string DEFAULT_AUTHOR = "Author";
        private const string DEFAULT_PACKAGE = "MyPackage";

        private const string TOOLTIP_ROOT_FOLDER = "The root folder to create the package from.\n'Asset' is not required because that's the main folder anyways.";
        private const string TOOLTIP_AS_SUBFOLDER = "Should the author be a separate folder?";
        private const string TOOLTIP_RUNTIME_CODE = "Dose the package contain runtime code?";
        private const string TOOLTIP_EDITOR_CODE = "Dose the package contain editor only code?\nWhen 'Runtime' is checked, the assembly definition will automatically be referenced.";

        private const string WARNING_ASSET_IN_PATH = "Folder path begins with 'Assets'. This will create a nested Assets folder";
        private Vector2 _scrollPosition = Vector2.zero;
        private string _rootFolderPath = "";
        private string _authorName = DEFAULT_AUTHOR;
        private bool _authorInPath = true;
        private string _packageName = DEFAULT_PACKAGE;
        private bool _createEditor = true;
        private bool _createRuntime = true;
        private string _totalPackageName = "";
        private string _assetFolderPath = "";

        [MenuItem("Tools/NaxtorGames/" + TOOL_NAME)]
        public static void ShowWindow()
        {
            AssemblyDefinitionCreator_Window window = GetWindow<AssemblyDefinitionCreator_Window>(TOOL_NAME);
            if (window._authorName == DEFAULT_AUTHOR)
            {
                window._authorName = string.IsNullOrWhiteSpace(Application.companyName) ? DEFAULT_AUTHOR : Application.companyName;
            }
            window.minSize = new Vector2(256 + 128, 256 + 16);
            window.maxSize = new Vector2(window.minSize.x * 2.0f, window.minSize.y + 128.0f);
            Rect windowRect = window.position;
            windowRect.size = window.minSize;
            windowRect.position = new Vector2(
                (Screen.currentResolution.width * 0.5f) - (windowRect.size.x * 0.5f),
                (Screen.currentResolution.height * 0.5f) - (windowRect.size.y * 0.5f));
            window.position = windowRect;
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _rootFolderPath = EditorGUILayout.TextField(new GUIContent("Root Folder", TOOLTIP_ROOT_FOLDER), _rootFolderPath);
            if (_rootFolderPath.StartsWith(ASSETS_ROOT))
            {
                EditorGUILayout.HelpBox(WARNING_ASSET_IN_PATH, MessageType.Warning);
                if (GUILayout.Button("Remove 'Assets/' From Path"))
                {
                    _rootFolderPath = _rootFolderPath.Substring(ASSETS_ROOT.Length);
                    UpdatePathPreview();
                }
            }
            _ = EditorGUILayout.BeginHorizontal();
            _authorName = EditorGUILayout.TextField(new GUIContent("Author"), _authorName);
            _authorInPath = EditorGUILayout.ToggleLeft(new GUIContent("As Subfolder", TOOLTIP_AS_SUBFOLDER), _authorInPath, GUILayout.Width(100.0f));
            EditorGUILayout.EndHorizontal();
            _packageName = EditorGUILayout.TextField("Package", _packageName);

            bool firstCheck = EditorGUI.EndChangeCheck();
            GUI.enabled = false;
            if (string.IsNullOrWhiteSpace(_assetFolderPath) || firstCheck)
            {
                UpdatePathPreview();
            }

            _ = EditorGUILayout.TextField(new GUIContent("Path", _assetFolderPath), _assetFolderPath);
            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Create Assembly For", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            _createRuntime = EditorGUILayout.Toggle(new GUIContent("Runtime", TOOLTIP_RUNTIME_CODE), _createRuntime);
            _createEditor = EditorGUILayout.Toggle(new GUIContent("Editor", TOOLTIP_EDITOR_CODE), _createEditor);
            EditorGUI.indentLevel--;

            if (string.IsNullOrEmpty(_totalPackageName) || firstCheck || EditorGUI.EndChangeCheck())
            {
                string correctAuthorName = RemoveSpacer(_authorName);
                string correctPackageName = RemoveSpacer(_packageName);
                _totalPackageName = $"com.{correctAuthorName}.{correctPackageName}";
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Package Name", EditorStyles.boldLabel);

            GUI.enabled = false;
            EditorGUI.indentLevel++;
            if (_createRuntime)
            {
                _ = EditorGUILayout.TextField(_totalPackageName + ".runtime");
            }
            if (_createEditor)
            {
                _ = EditorGUILayout.TextField(_totalPackageName + ".editor");
            }
            EditorGUI.indentLevel--;
            GUI.enabled = true;

            EditorGUILayout.Space();

            GUI.enabled = _createRuntime || _createEditor;

            if (GUILayout.Button("Create Assembly Definitions"))
            {
                CreateAssemblyDefinitions(_rootFolderPath,
                                                                    _authorName,
                                                                    _packageName,
                                                                    _createRuntime,
                                                                    _createEditor,
                                                                    _authorInPath);
            }
            GUI.enabled = true;

            EditorGUILayout.EndScrollView();
        }

        private void UpdatePathPreview()
        {
            if (ClearPath(ref _rootFolderPath))
            {
                Debug.LogWarning("Invalid characters were removed from 'Root Path'");
            }
            if (ClearNames(ref _authorName))
            {
                Debug.LogWarning("Invalid characters were removed from 'Author'");
            }
            if (ClearNames(ref _packageName))
            {
                Debug.LogWarning("Invalid characters were removed from 'Package'");
            }

            string rootFolder = string.IsNullOrWhiteSpace(_rootFolderPath) ? "" : $"{_rootFolderPath}/";
            if (_authorInPath && !string.IsNullOrWhiteSpace(_authorName))
            {
                _assetFolderPath = $"Assets/{rootFolder}{CapitalizeFirstLetters(_authorName)}/{CapitalizeFirstLetters(_packageName)}/";
            }
            else
            {
                _assetFolderPath = $"Assets/{rootFolder}{CapitalizeFirstLetters(_packageName)}/";
            }
        }
    }
}
