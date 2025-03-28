using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace NaxtorGames.AssemblyDefinitionCreator
{
    public static class AssemblyDefinitionCreator
    {
        private const string SOURCE_FOLDER_NAME = "Source";
        private const string RUNTIME_FOLDER_NAME = "Runtime";
        private const string EDITOR_FOLDER_NAME = "Editor";
        private const string PLATFORM_EDITOR = "Editor";

        public static readonly string ProjectPath = Application.dataPath;

        private static readonly string s_invalidPathCharsRegEx = Regex.Escape(new string(Path.GetInvalidPathChars()));
        private static readonly string s_invalidFileCharsRegEx = Regex.Escape(new string(Path.GetInvalidFileNameChars()));

        [Serializable]
        public sealed class AssemblyDefinitionAsset
        {
            [Serializable]
            public sealed class VersionDefine
            {
                public string name;
                public string expression;
                public string define;
            }

            public string name = string.Empty;
            public string rootNamespace = string.Empty;
            public string[] references = Array.Empty<string>();
            public string[] includePlatforms = Array.Empty<string>();
            public string[] excludePlatforms = Array.Empty<string>();
            public bool allowUnsafeCode = false;
            public bool overrideReferences = false;
            public string[] precompiledReferences = Array.Empty<string>();
            public bool autoReferenced = true;
            public string[] defineConstraints = Array.Empty<string>();
            public VersionDefine[] versionDefines = Array.Empty<VersionDefine>();
            public bool noEngineReferences = false;
        }

        public static void CreateAssemblyDefinitions(string path, string authorName, string packageName, bool createRuntime, bool createEditor, bool authorInPath = false)
        {
            if (!createRuntime && !createEditor)
            {
                Debug.LogError("Both create Runtime and Editor are false!");
                return;
            }
            if (path == null || string.IsNullOrWhiteSpace(authorName) || string.IsNullOrWhiteSpace(packageName))
            {
                Debug.LogError("Path cannot be null and Author and Package cannot be empty or null!");
                return;
            }

            _ = ClearPath(ref path);
            _ = ClearNames(ref authorName);
            _ = ClearNames(ref packageName);

            string mainFolderPath;
            if (authorInPath)
            {
                mainFolderPath = Path.Combine(ProjectPath, path, CapitalizeFirstLetters(authorName), CapitalizeFirstLetters(packageName));
            }
            else
            {
                mainFolderPath = Path.Combine(ProjectPath, path, CapitalizeFirstLetters(packageName));
            }
            _ = Directory.CreateDirectory(mainFolderPath);

            string sourceFolderPath = Path.Combine(mainFolderPath, SOURCE_FOLDER_NAME);
            _ = Directory.CreateDirectory(sourceFolderPath);

            if (createRuntime)
            {
                string runtimeFolderPath = Path.Combine(sourceFolderPath, RUNTIME_FOLDER_NAME);
                _ = Directory.CreateDirectory(runtimeFolderPath);
                CreateAssemblyDefinition(runtimeFolderPath, CreateAssemblyName(authorName, packageName, false, false), false);
            }
            if (createEditor)
            {
                string editorFolderPath = Path.Combine(sourceFolderPath, EDITOR_FOLDER_NAME);
                _ = Directory.CreateDirectory(editorFolderPath);

                string editorAssemblyName = CreateAssemblyName(authorName, packageName, true, false);

                if (createRuntime)
                {
                    AssetDatabase.Refresh();

                    string runtimeAssemblyPath;
                    if (authorInPath)
                    {
                        runtimeAssemblyPath = Path.Combine(path, CapitalizeFirstLetters(authorName), CapitalizeFirstLetters(packageName), SOURCE_FOLDER_NAME, RUNTIME_FOLDER_NAME);
                    }
                    else
                    {
                        runtimeAssemblyPath = Path.Combine(path, CapitalizeFirstLetters(packageName), SOURCE_FOLDER_NAME, RUNTIME_FOLDER_NAME);
                    }
                    string totalRuntimeAssemblyPath = Path.Combine("Assets", runtimeAssemblyPath, CreateAssemblyName(authorName, packageName, false, true));
                    string runtimeAssemblyGUID = AssetDatabase.AssetPathToGUID(totalRuntimeAssemblyPath);

                    if (!string.IsNullOrWhiteSpace(runtimeAssemblyGUID))
                    {
                        AssemblyDefinitionAsset asmdef = new AssemblyDefinitionAsset
                        {
                            name = editorAssemblyName,
                            includePlatforms = new string[] { PLATFORM_EDITOR },
                            references = new string[] { $"GUID:{runtimeAssemblyGUID}" }
                        };

                        File.WriteAllText(Path.Combine(editorFolderPath, $"{editorAssemblyName}.asmdef"), JsonUtility.ToJson(asmdef, true));
                    }
                }
                else
                {
                    CreateAssemblyDefinition(editorFolderPath, editorAssemblyName, true);
                }
            }

            AssetDatabase.Refresh();
        }

        public static string CreateAssemblyName(string author, string package, bool isEditor, bool withAssetExtension)
        {
            return $"com.{RemoveSpacer(author)}.{RemoveSpacer(package)}.{(isEditor ? "editor" : "runtime")}{(withAssetExtension ? ".asmdef" : "")}";
        }

        public static string CapitalizeFirstLetters(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i++)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }

            return string.Join(" ", words);
        }

        public static string RemoveSpacer(string text)
        {
            return Regex.Replace(text, "[ _-]", "").ToLower();
        }

        public static bool ClearPath(ref string path)
        {
            string oldPath = path;
            path = Regex.Replace(path, $"[{s_invalidPathCharsRegEx}]", "");
            return !oldPath.Equals(path);
        }

        public static bool ClearNames(ref string name)
        {
            string oldName = name;
            name = Regex.Replace(name, $"[{s_invalidFileCharsRegEx}]", "");
            return !oldName.Equals(name);
        }

        private static void CreateAssemblyDefinition(string folderPath, string assemblyName, bool isEditor)
        {
            AssemblyDefinitionAsset asmdef = new AssemblyDefinitionAsset
            {
                name = assemblyName,
                includePlatforms = isEditor ? new string[] { EDITOR_FOLDER_NAME } : Array.Empty<string>(),
            };

            File.WriteAllText(Path.Combine(folderPath, $"{assemblyName}.asmdef"), JsonUtility.ToJson(asmdef, true));
        }
    }
}