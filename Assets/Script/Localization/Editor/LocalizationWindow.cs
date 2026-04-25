using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using Newtonsoft.Json;

namespace Localization.Editor
{
    public class LocalizationWindow : EditorWindow
    {
        private Vector2 scrollPos;

        // ========== 请在这里填入你的豆包API信息 ==========
        private const string DOUBAO_API_KEY = "fcf24e8b-ac9a-49b3-8931-b27928e52e1f";
        private const string ENDPOINT_ID = "doubao-lite-32k-character-250228";
        private const string API_URL = "https://ark.cn-beijing.volces.com/api/v3/chat/completions";
        // =================================================

        [MenuItem("Tools/Localization Hub")]
        public static void Open() => GetWindow<LocalizationWindow>("Localization Hub");

        private void OnGUI()
        {
            GUILayout.Label("Workflow (Global Scan)", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("1. Scan & Generate JSON (All)"))
            {
                ExportGlobalJson();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("2. Load JSON (Apply to All)"))
            {
                ImportGlobalJson();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("3. AI Auto-Translate (Fill English)"))
            {
                AutoTranslateJSON();
            }

            GUILayout.Space(20);
            GUILayout.Label("Debug Info", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.HelpBox("This tool scans ALL Scenes in Build Settings and ALL Prefabs in Assets.", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        // 数据容器
        class TextData
        {
            public int id;
            public List<LocalizedString> strings;
            public string sourcePath;
        }

        // ================= 导出逻辑 =================

        private void ExportGlobalJson()
        {
            var dataDict = new Dictionary<int, TextData>();

            ScanScenes(dataDict);
            ScanPrefabs(dataDict);

            string json = JsonConvert.SerializeObject(dataDict.Values.ToList(), Formatting.Indented);
            string path = EditorUtility.SaveFilePanel("Save Localization", Application.dataPath, "LocalizationData.json", "json");

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, json);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Success", $"JSON generated! Found {dataDict.Count} unique texts.", "OK");
            }
        }

        private void ScanScenes(Dictionary<int, TextData> dict)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            string currentScenePath = currentScene.path;

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;

                Scene sceneOpened = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
                var allTexts = Object.FindObjectsOfType<LocalizedText>(includeInactive: true);

                foreach (var text in allTexts)
                {
                    AddOrUpdateDict(dict, text, scene.path);
                }
            }

            if (!string.IsNullOrEmpty(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
            }
        }

        private void ScanPrefabs(Dictionary<int, TextData> dict)
        {
            string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab");

            foreach (string guid in allPrefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    var components = prefab.GetComponentsInChildren<LocalizedText>(includeInactive: true);
                    foreach (var text in components)
                    {
                        AddOrUpdateDict(dict, text, path);
                    }
                }
            }
        }

        private void AddOrUpdateDict(Dictionary<int, TextData> dict, LocalizedText text, string source)
        {
            if (text.UniqueId == 0)
            {
                /* Debug.LogWarning($"Skipping text with ID 0 in: {source}"); */
                return;
            }

            if (!dict.ContainsKey(text.UniqueId))
            {
                dict[text.UniqueId] = new TextData
                {
                    id = text.UniqueId,
                    strings = text.LocalizedStrings,
                    sourcePath = source
                };
            }
            else
            {
                /* Debug.LogWarning($"Duplicate ID {text.UniqueId} found in {source}. It already exists in {dict[text.UniqueId].sourcePath}"); */
            }
        }

        // ================= 导入逻辑 =================

        private void ImportGlobalJson()
        {
            string path = EditorUtility.OpenFilePanel("Load Localization", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(path))
            {
                string json = File.ReadAllText(path);
                var dataList = JsonConvert.DeserializeObject<List<TextData>>(json);

                var dataDict = dataList.ToDictionary(d => d.id);
                int updateCount = 0;

                var sceneTexts = Object.FindObjectsOfType<LocalizedText>(includeInactive: true);
                ApplyData(sceneTexts, dataDict, ref updateCount);

                string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab");
                foreach (string guid in allPrefabGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                    if (prefab != null)
                    {
                        var prefabTexts = prefab.GetComponentsInChildren<LocalizedText>(includeInactive: true);
                        bool prefabDirty = false;

                        foreach (var text in prefabTexts)
                        {
                            if (dataDict.TryGetValue(text.UniqueId, out var data))
                            {
                                text.LoadData(data.strings);
                                EditorUtility.SetDirty(text);
                                updateCount++;
                                prefabDirty = true;
                            }
                        }

                        if (prefabDirty)
                        {
                            EditorUtility.SetDirty(prefab);
                        }
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Success", $"Updated {updateCount} texts across scenes and prefabs!", "OK");
            }
        }

        private void ApplyData(LocalizedText[] texts, Dictionary<int, TextData> dict, ref int count)
        {
            foreach (var text in texts)
            {
                if (dict.TryGetValue(text.UniqueId, out var data))
                {
                    text.LoadData(data.strings);
                    EditorUtility.SetDirty(text);
                    count++;
                }
            }
        }

        // ================= AI 翻译逻辑 =================

        private void AutoTranslateJSON()
        {
            string path = EditorUtility.OpenFilePanel("选择要翻译的JSON文件", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return;

            string json = File.ReadAllText(path);
            var dataList = JsonConvert.DeserializeObject<List<TextData>>(json);

            if (dataList == null || dataList.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "JSON文件解析失败，请检查格式", "OK");
                return;
            }

            EditorCoroutineUtility.StartCoroutine(TranslateCoroutine(dataList, path), this);
        }

        private IEnumerator TranslateCoroutine(List<TextData> dataList, string savePath)
        {
            int totalToTranslate = 0;
            int translatedCount = 0;

            foreach (var item in dataList)
            {
                var chineseEntry = item.strings.Find(s => s.language == Language.Chinese);
                var englishEntry = item.strings.Find(s => s.language == Language.English);
                if (chineseEntry != null && englishEntry != null && string.IsNullOrEmpty(englishEntry.content))
                {
                    totalToTranslate++;
                }
            }

            if (totalToTranslate == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有找到需要翻译的空英文条目", "OK");
                yield break;
            }

            EditorUtility.DisplayProgressBar("AI翻译中", $"正在准备翻译 {totalToTranslate} 条文本...", 0);

            foreach (var item in dataList)
            {
                var chineseEntry = item.strings.Find(s => s.language == Language.Chinese);
                var englishEntry = item.strings.Find(s => s.language == Language.English);

                if (chineseEntry != null && englishEntry != null && string.IsNullOrEmpty(englishEntry.content))
                {
                    string sourceText = chineseEntry.content.Trim();
                    EditorUtility.DisplayProgressBar("AI翻译中", $"正在翻译：{sourceText} ({translatedCount + 1}/{totalToTranslate})", (float)translatedCount / totalToTranslate);
                    /* Debug.Log($"正在翻译 ({translatedCount + 1}/{totalToTranslate})：{sourceText}"); */

                    var requestBody = new TranslationRequest
                    {
                        model = ENDPOINT_ID,
                        messages = new System.Collections.Generic.List<Message>
                        {
                            new Message
                            {
                                role = "user",
                                content = $"你是专业的游戏本地化翻译，把下面的中文游戏UI文本翻译成通顺自然的英文，只返回翻译结果，不要任何解释、标点和多余内容：{sourceText}"
                            }
                        },
                        temperature = 0.3f
                    };

                    string requestJson = JsonConvert.SerializeObject(requestBody);
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);

                    using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
                    {
                        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                        request.downloadHandler = new DownloadHandlerBuffer();
                        request.SetRequestHeader("Content-Type", "application/json");
                        request.SetRequestHeader("Authorization", $"Bearer {DOUBAO_API_KEY}");

                        yield return request.SendWebRequest();

                        if (request.result == UnityWebRequest.Result.Success)
                        {
                            var response = JsonConvert.DeserializeObject<TranslationResponse>(request.downloadHandler.text);
                            if (response.choices != null && response.choices.Length > 0)
                            {
                                string translateResult = response.choices[0].message.content.Trim();
                                englishEntry.content = translateResult;
                                translatedCount++;
                                /* Debug.Log($"翻译完成：{sourceText} -> {translateResult}"); */
                            }
                        }
                        else
                        {
                            /* Debug.LogError($"翻译失败！文本：{sourceText}，错误：{request.error}，响应：{request.downloadHandler.text}"); */
                            EditorUtility.ClearProgressBar();
                            EditorUtility.DisplayDialog("翻译失败", $"请求出错：{request.error}\n请查看Console日志详情", "OK");
                            yield break;
                        }
                    }

                    yield return new EditorWaitForSeconds(0.3f);
                }
            }

            string newJson = JsonConvert.SerializeObject(dataList, Formatting.Indented);
            File.WriteAllText(savePath, newJson);
            AssetDatabase.Refresh();

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("翻译完成", $"成功翻译 {translatedCount}/{totalToTranslate} 条文本！\n已自动保存到JSON文件", "OK");
        }

        // API 序列化辅助类
        [System.Serializable]
        public class TranslationRequest
        {
            public string model;
            public System.Collections.Generic.List<Message> messages;
            public float temperature = 0.3f;
        }

        [System.Serializable]
        public class Message
        {
            public string role;
            public string content;
        }

        [System.Serializable]
        public class TranslationResponse
        {
            public Choice[] choices;
        }

        [System.Serializable]
        public class Choice
        {
            public Message message;
        }
    }
}