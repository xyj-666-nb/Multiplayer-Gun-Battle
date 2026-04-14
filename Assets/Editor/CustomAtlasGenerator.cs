using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.U2D.Sprites;
#endif

public class CustomAtlasGenerator : EditorWindow
{
    [Header("核心设置")]
    [Tooltip("精灵间距")]
    public int padding = 2;
    [Tooltip("图集最大宽度")]
    public int maxAtlasWidth = 2048;

    [Header("混合打包区  带切片的 Texture")]
    public List<UnityEngine.Object> targetAssets = new List<UnityEngine.Object>();

    [Header("保存设置")]
    public string saveFolderPath = "Assets/Resources/Sprite/CustomizeAtlas";
    public string atlasFileName = "CustomAtlas_Dense";

    // 仅修改这里：从单个路径 改为 两个路径（完全不改动其他代码）
    private static readonly string[] FOLDER_PATHS = {
        "Assets/Resources/Prefabs",
        "Assets/Resources/UI"
    };

    [Header("引用替换区")]
    public Texture2D generatedAtlas;

    [Serializable]
    public class ReplaceRecord
    {
        public string atlasGuid;
        public string atlasPath;
        public string spriteName;
        public string originalSpriteGuid;
        public string originalSpritePath;
    }

    [Serializable]
    public class ReplaceRecordDatabase
    {
        public List<ReplaceRecord> records = new List<ReplaceRecord>();
    }

    private ReplaceRecordDatabase _recordDb;
    private const string RecordFileName = "CustomAtlasReplaceRecords.json";

    // 存储精灵完整参数
    private class TexData
    {
        public string name;
        public int width;
        public int height;
        public Color32[] pixels;
        public Rect targetRect;
        public float pixelsPerUnit;
        public Vector2 pivot;
        public Vector4 border;
    }

    [MenuItem("工具/自研图集生成器(混合精灵打包)")]
    public static void ShowWindow()
    {
        GetWindow<CustomAtlasGenerator>("混合精灵打包工具").minSize = new Vector2(480, 750);
    }

    private void OnEnable()
    {
        LoadRecords();
    }

    private void OnGUI()
    {
        try
        {
            GUILayout.Space(10);
            GUILayout.Label("第一步：混合资源打包（极致高效）", EditorStyles.boldLabel);
            GUILayout.Label("支持：直接拖入 或 选中后一键添加 Sprite/Texture", EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(5);

            SerializedObject so = new SerializedObject(this);
            EditorGUILayout.PropertyField(so.FindProperty("padding"));
            EditorGUILayout.PropertyField(so.FindProperty("maxAtlasWidth"));

            GUILayout.Space(5);
            GUI.backgroundColor = new Color(0.8f, 1f, 0.6f);
            if (GUILayout.Button(" 打包区-添加选中资源", GUILayout.Height(30)))
            {
                AddSelectedToTarget();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.PropertyField(so.FindProperty("targetAssets"), new GUIContent("打包资源列表"), true);

            GUILayout.Space(5);
            EditorGUILayout.PropertyField(so.FindProperty("saveFolderPath"), new GUIContent("保存文件夹"));
            EditorGUILayout.PropertyField(so.FindProperty("atlasFileName"), new GUIContent("生成图集名"));
            so.ApplyModifiedProperties();

            GUILayout.Space(10);
            GUI.backgroundColor = new Color(0.6f, 1f, 1);
            if (GUILayout.Button(" 一键生成紧凑图集", GUILayout.Height(40)))
            {
                GenerateAtlas();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(15);
            DrawLine();
            GUILayout.Space(10);

            GUILayout.Label("第二步：全自动替换/还原（场景+指定预制体）", EditorStyles.boldLabel);
            GUILayout.Label($"预制体路径：多文件夹扫描", EditorStyles.helpBox);
            so.Update();
            EditorGUILayout.PropertyField(so.FindProperty("generatedAtlas"), new GUIContent("新生成的图集"));
            so.ApplyModifiedProperties();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
            if (GUILayout.Button("全局替换老资源", GUILayout.Height(35))) ProcessReferences();
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("一键全部还原", GUILayout.Height(35))) RevertAllRecords();
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
            DrawLine();
        }
        catch (Exception ex)
        {
            Debug.LogError("GUI错误: " + ex);
        }
    }

    private void DrawLine()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color32(127, 127, 127, 255));
    }

    private void AddSelectedToTarget()
    {
        var selections = Selection.objects;
        if (!selections.Any())
        {
            EditorUtility.DisplayDialog("提示", "请选中资源！", "确定");
            return;
        }
        int count = 0;
        foreach (var obj in selections)
        {
            if ((obj is Sprite || obj is Texture2D) && !targetAssets.Contains(obj))
            {
                targetAssets.Add(obj);
                count++;
            }
        }
        EditorUtility.DisplayDialog("完成", $"添加 {count} 个资源到打包列表！", "确定");
        Repaint();
    }

    #region 记录管理
    private string GetRecordFilePath() => Path.Combine(Application.dataPath, "..", "Library", RecordFileName);
    private void LoadRecords()
    {
        string path = GetRecordFilePath();
        if (File.Exists(path))
        {
            try { _recordDb = JsonUtility.FromJson<ReplaceRecordDatabase>(File.ReadAllText(path)); }
            catch { _recordDb = new ReplaceRecordDatabase(); }
        }
        else
            _recordDb = new ReplaceRecordDatabase();
    }
    private void SaveRecords() => File.WriteAllText(GetRecordFilePath(), JsonUtility.ToJson(_recordDb, true));
    private void AddRecord(ReplaceRecord record)
    {
        if (!_recordDb.records.Any(r => r.originalSpriteGuid == record.originalSpriteGuid))
        {
            _recordDb.records.Add(record);
            SaveRecords();
        }
    }
    private void ClearRecords()
    {
        _recordDb.records.Clear();
        SaveRecords();
    }
    #endregion

    #region 纹理处理
    private void MakeTextureReadable(string path, TextureImporter importer)
    {
        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
    }

    private Color32[] GetSpritePixels(Sprite sprite, out int w, out int h)
    {
        w = (int)sprite.rect.width;
        h = (int)sprite.rect.height;
        Rect r = sprite.rect;
        Texture2D tex = sprite.texture;

        if (!tex || !tex.isReadable) { throw new Exception("纹理不可读！"); }

        Color32[] fullPixels = tex.GetPixels32();
        Color32[] pixels = new Color32[w * h];
        int texWidth = tex.width;

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = fullPixels[(int)(r.y + y) * texWidth + (int)(r.x + x)];
        return pixels;
    }
    #endregion

    #region 精灵解析
    private void CollectAllSprites(List<TexData> output, List<string> modifiedImporters)
    {
        foreach (var asset in targetAssets.Where(a => a != null))
        {
            try
            {
                if (asset is Sprite sprite) ProcessSingleSprite(sprite, output, modifiedImporters);
                else if (asset is Texture2D texture) ProcessTextureSprites(texture, output, modifiedImporters);
            }
            catch { continue; }
        }
    }

    private void ProcessSingleSprite(Sprite sprite, List<TexData> output, List<string> modifiedImporters)
    {
        string texPath = AssetDatabase.GetAssetPath(sprite.texture);
        TextureImporter importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer && !importer.isReadable)
        {
            MakeTextureReadable(texPath, importer);
            modifiedImporters.Add(texPath);
        }

        var data = new TexData
        {
            name = sprite.name,
            pixels = GetSpritePixels(sprite, out int w, out int h),
            width = w,
            height = h,
            pixelsPerUnit = sprite.pixelsPerUnit,
            pivot = sprite.pivot,
            border = sprite.border
        };
        output.Add(data);
    }

    private void ProcessTextureSprites(Texture2D texture, List<TexData> output, List<string> modifiedImporters)
    {
        string texPath = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer && !importer.isReadable)
        {
            MakeTextureReadable(texPath, importer);
            modifiedImporters.Add(texPath);
        }

        var sprites = AssetDatabase.LoadAllAssetsAtPath(texPath).OfType<Sprite>().ToList();
        foreach (var s in sprites)
        {
            var data = new TexData
            {
                name = s.name,
                pixels = GetSpritePixels(s, out int w, out int h),
                width = w,
                height = h,
                pixelsPerUnit = s.pixelsPerUnit,
                pivot = s.pivot,
                border = s.border
            };
            output.Add(data);
        }
    }
    #endregion

    #region 图集生成
    private void GenerateAtlas()
    {
        if (!targetAssets.Any(a => a != null))
        {
            EditorUtility.DisplayDialog("提示", "请添加打包资源！", "确定");
            return;
        }

        List<TexData> dataList = new List<TexData>();
        List<string> modifiedImporters = new List<string>();

        try
        {
            EditorUtility.DisplayProgressBar("处理", "解析精灵...", 0.2f);
            CollectAllSprites(dataList, modifiedImporters);

            if (!dataList.Any())
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("错误", "未找到精灵！", "确定");
                return;
            }

            dataList.Sort((a, b) => b.height.CompareTo(a.height));
            int cx = padding, cy = padding, rowH = 0, maxW = padding;
            foreach (var d in dataList)
            {
                if (cx + d.width + padding > maxAtlasWidth && cx > padding)
                {
                    cx = padding;
                    cy += rowH + padding;
                    rowH = 0;
                }
                d.targetRect = new Rect(cx, cy, d.width, d.height);
                cx += d.width + padding;
                maxW = Math.Max(maxW, cx);
                rowH = Math.Max(rowH, d.height);
            }

            int fw = Mathf.NextPowerOfTwo(maxW);
            int fh = Mathf.NextPowerOfTwo(cy + rowH + padding);
            Texture2D atlas = new Texture2D(fw, fh, TextureFormat.RGBA32, false);
            atlas.SetPixels32(Enumerable.Repeat(new Color32(0, 0, 0, 0), fw * fh).ToArray());

            foreach (var d in dataList)
            {
                atlas.SetPixels32((int)d.targetRect.x, (int)d.targetRect.y, d.width, d.height, d.pixels);
            }
            atlas.Apply();

            Directory.CreateDirectory(Path.GetFullPath(saveFolderPath));
            string name = atlasFileName.Trim().EndsWith(".png") ? atlasFileName.Trim() : $"{atlasFileName.Trim()}.png";
            string path = Path.Combine(saveFolderPath, name).Replace("\\", "/");
            File.WriteAllBytes(Path.GetFullPath(path), atlas.EncodeToPNG());
            AssetDatabase.Refresh();
            DestroyImmediate(atlas);

            SetAtlasSettings(path, dataList);
            generatedAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("成功", "图集生成完成！\n大小/位置100%匹配原始资源", "确定");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            foreach (var p in modifiedImporters)
            {
                var imp = AssetImporter.GetAtPath(p) as TextureImporter;
                if (imp) { imp.isReadable = false; AssetDatabase.ImportAsset(p); }
            }
        }
    }

    private void SetAtlasSettings(string path, List<TexData> dataList)
    {
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!imp) return;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.isReadable = false;
        imp.mipmapEnabled = false;
        imp.filterMode = FilterMode.Bilinear;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.spritePixelsPerUnit = dataList[0].pixelsPerUnit;

#if UNITY_2021_2_OR_NEWER
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(imp);
        provider.InitSpriteEditorDataProvider();

        var spriteRects = dataList.Select(d => new SpriteRect
        {
            name = d.name,
            rect = d.targetRect,
            pivot = d.pivot,
            border = d.border
        }).ToArray();

        provider.SetSpriteRects(spriteRects);
        provider.Apply();
#else
        imp.spritesheet = dataList.Select(d => new SpriteMetaData
        {
            name = d.name,
            rect = d.targetRect,
            pivot = d.pivot,
            border = d.border
        }).ToArray();
#endif
        EditorUtility.SetDirty(imp);
        AssetDatabase.ImportAsset(path);
    }
    #endregion

    #region 替换逻辑（带记录）
    private void ProcessReferences()
    {
        if (!generatedAtlas)
        {
            EditorUtility.DisplayDialog("提示", "请先生成图集！", "确定");
            return;
        }

        var newSprites = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(generatedAtlas))
            .OfType<Sprite>()
            .GroupBy(s => s.name)
            .ToDictionary(g => g.Key, g => g.First());

        ClearRecords();
        int count = 0;
        count += ReplaceScene(newSprites);
        count += ReplaceTargetFolderPrefabs(newSprites);

        EditorUtility.DisplayDialog("完成", $"成功替换 {count} 个精灵！\n可点击【一键全部还原】恢复原始资源", "确定");
    }

    private int ReplaceScene(Dictionary<string, Sprite> newSprites)
    {
        int c = 0;
        foreach (var img in FindObjectsOfType<Image>(true))
        {
            if (img.sprite && newSprites.TryGetValue(img.sprite.name, out var s))
            {
                // 保存替换记录
                AddRecord(new ReplaceRecord
                {
                    spriteName = img.sprite.name,
                    originalSpritePath = AssetDatabase.GetAssetPath(img.sprite),
                    originalSpriteGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(img.sprite)),
                    atlasPath = AssetDatabase.GetAssetPath(s),
                    atlasGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(s))
                });

                Undo.RecordObject(img, "替换精灵");
                img.sprite = s;
                c++;
            }
        }
        foreach (var sr in FindObjectsOfType<SpriteRenderer>(true))
        {
            if (sr.sprite && newSprites.TryGetValue(sr.sprite.name, out var s))
            {
                AddRecord(new ReplaceRecord
                {
                    spriteName = sr.sprite.name,
                    originalSpritePath = AssetDatabase.GetAssetPath(sr.sprite),
                    originalSpriteGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sr.sprite)),
                    atlasPath = AssetDatabase.GetAssetPath(s),
                    atlasGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(s))
                });

                Undo.RecordObject(sr, "替换精灵");
                sr.sprite = s;
                c++;
            }
        }
        return c;
    }

    //  仅修改这里：遍历所有路径，其他代码完全原样
    private int ReplaceTargetFolderPrefabs(Dictionary<string, Sprite> newSprites)
    {
        int c = 0;
        foreach (var path in FOLDER_PATHS)
        {
            if (!AssetDatabase.IsValidFolder(path)) continue;
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (!prefab) continue;
                bool mod = false;

                foreach (var img in prefab.GetComponentsInChildren<Image>(true))
                {
                    if (img.sprite && newSprites.TryGetValue(img.sprite.name, out var s))
                    {
                        AddRecord(new ReplaceRecord
                        {
                            spriteName = img.sprite.name,
                            originalSpritePath = AssetDatabase.GetAssetPath(img.sprite),
                            originalSpriteGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(img.sprite)),
                            atlasPath = AssetDatabase.GetAssetPath(s),
                            atlasGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(s))
                        });
                        img.sprite = s; c++; mod = true;
                    }
                }
                foreach (var sr in prefab.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.sprite && newSprites.TryGetValue(sr.sprite.name, out var s))
                    {
                        AddRecord(new ReplaceRecord
                        {
                            spriteName = sr.sprite.name,
                            originalSpritePath = AssetDatabase.GetAssetPath(sr.sprite),
                            originalSpriteGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sr.sprite)),
                            atlasPath = AssetDatabase.GetAssetPath(s),
                            atlasGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(s))
                        });
                        sr.sprite = s; c++; mod = true;
                    }
                }
                if (mod) PrefabUtility.SavePrefabAsset(prefab);
            }
        }
        return c;
    }
    #endregion

    #region 完美还原逻辑
    private void RevertAllRecords()
    {
        if (_recordDb.records.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有可还原的记录！", "确定");
            return;
        }

        int count = 0;
        // 还原场景对象
        count += RevertSceneObjects();
        // 还原预制体
        count += RevertPrefabs();

        ClearRecords();
        EditorUtility.DisplayDialog("还原成功", $"已恢复 {count} 个原始精灵！", "确定");
    }

    // 还原场景中的Image和SpriteRenderer
    private int RevertSceneObjects()
    {
        int c = 0;
        foreach (var record in _recordDb.records)
        {
            Sprite originalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(record.originalSpritePath);
            if (originalSprite == null) continue;

            // 还原UI
            foreach (var img in FindObjectsOfType<Image>(true))
            {
                if (img.sprite != null && img.sprite.name == record.spriteName)
                {
                    Undo.RecordObject(img, "还原精灵");
                    img.sprite = originalSprite;
                    c++;
                }
            }
            // 还原2D
            foreach (var sr in FindObjectsOfType<SpriteRenderer>(true))
            {
                if (sr.sprite != null && sr.sprite.name == record.spriteName)
                {
                    Undo.RecordObject(sr, "还原精灵");
                    sr.sprite = originalSprite;
                    c++;
                }
            }
        }
        return c;
    }

    // 仅修改这里：遍历所有路径，其他代码完全原样
    private int RevertPrefabs()
    {
        int c = 0;
        foreach (var path in FOLDER_PATHS)
        {
            if (!AssetDatabase.IsValidFolder(path)) continue;
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (!prefab) continue;
                bool mod = false;

                foreach (var record in _recordDb.records)
                {
                    Sprite originalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(record.originalSpritePath);
                    if (originalSprite == null) continue;

                    // 还原UI
                    foreach (var img in prefab.GetComponentsInChildren<Image>(true))
                    {
                        if (img.sprite != null && img.sprite.name == record.spriteName)
                        {
                            img.sprite = originalSprite; c++; mod = true;
                        }
                    }
                    // 还原2D
                    foreach (var sr in prefab.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if (sr.sprite != null && sr.sprite.name == record.spriteName)
                        {
                            sr.sprite = originalSprite; c++; mod = true;
                        }
                    }
                }
                if (mod) PrefabUtility.SavePrefabAsset(prefab);
            }
        }
        return c;
    }
    #endregion
}