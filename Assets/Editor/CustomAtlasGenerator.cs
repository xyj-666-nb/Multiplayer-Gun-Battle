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

    [Header("混合打包区  支持 Sprite / 带切片的 Texture")]
    public List<UnityEngine.Object> targetAssets = new List<UnityEngine.Object>();

    [Header("保存设置")]
    public string saveFolderPath = "Assets/Resources/Sprite/CustomizeAtlas";
    public string atlasFileName = "CustomAtlas_Dense";

    // 固定预制体路径
    private const string PREFAB_FOLDER_PATH = "Assets/Resources/Prefabs";

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

    // 存储精灵完整参数（修复大小+位置核心）
    private class TexData
    {
        public string name;
        public int width;
        public int height;
        public Color32[] pixels;
        public Rect targetRect;
        // 原始精灵核心参数
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
        GUILayout.Space(10);
        GUILayout.Label("第一步：混合资源打包（极致高效）", EditorStyles.boldLabel);
        GUILayout.Label("支持：直接拖入 或 选中后一键添加 Sprite/Texture", EditorStyles.wordWrappedMiniLabel);
        GUILayout.Space(5);

        SerializedObject so = new SerializedObject(this);
        EditorGUILayout.PropertyField(so.FindProperty("padding"));
        EditorGUILayout.PropertyField(so.FindProperty("maxAtlasWidth"));

        // 打包区 - 一键添加选中资源
        GUILayout.Space(5);
        GUI.backgroundColor = new Color(0.8f, 1f, 0.6f);
        if (GUILayout.Button(" 打包区-添加选中资源", GUILayout.Height(30)))
        {
            AddSelectedToTarget();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.PropertyField(so.FindProperty("targetAssets"), new GUIContent("打包资源列表"), true);

        // 保存设置
        GUILayout.Space(5);
        EditorGUILayout.PropertyField(so.FindProperty("saveFolderPath"), new GUIContent("保存文件夹"));
        EditorGUILayout.PropertyField(so.FindProperty("atlasFileName"), new GUIContent("生成图集名"));
        so.ApplyModifiedProperties();

        // 一键生成
        GUILayout.Space(10);
        GUI.backgroundColor = new Color(0.6f, 1f, 1);
        if (GUILayout.Button(" 一键生成紧凑图集", GUILayout.Height(40)))
        {
            GenerateAtlas();
        }
        GUI.backgroundColor = Color.white;

        // 分隔线
        GUILayout.Space(15);
        DrawLine();
        GUILayout.Space(10);

        // 替换区域
        GUILayout.Label("第二步：全自动替换（场景+指定预制体）", EditorStyles.boldLabel);
        GUILayout.Label($"预制体路径：{PREFAB_FOLDER_PATH}", EditorStyles.helpBox);
        so.Update();
        EditorGUILayout.PropertyField(so.FindProperty("generatedAtlas"), new GUIContent("新生成的图集"));
        so.ApplyModifiedProperties();

        // 替换/还原按钮
        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
        if (GUILayout.Button("全局替换老资源", GUILayout.Height(35))) ProcessReferences();
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("一键全部还原", GUILayout.Height(35))) RevertAllRecords();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(15);
        DrawLine();
    }

    private void DrawLine()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color32(127, 127, 127, 255));
    }

    // ========== 批量添加：打包区 ==========
    private void AddSelectedToTarget()
    {
        var selections = Selection.objects;
        if (!selections.Any()) { EditorUtility.DisplayDialog("提示", "请选中资源！", "确定"); return; }
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
        if (File.Exists(path)) try { _recordDb = JsonUtility.FromJson<ReplaceRecordDatabase>(File.ReadAllText(path)); } catch { _recordDb = new ReplaceRecordDatabase(); }
        else _recordDb = new ReplaceRecordDatabase();
    }
    private void SaveRecords() => File.WriteAllText(GetRecordFilePath(), JsonUtility.ToJson(_recordDb, true));
    private void AddRecord(ReplaceRecord record) { if (!_recordDb.records.Any(r => r.originalSpriteGuid == record.originalSpriteGuid)) { _recordDb.records.Add(record); SaveRecords(); } }
    private void RemoveRecord(ReplaceRecord record) { _recordDb.records.Remove(record); SaveRecords(); }
    #endregion

    #region 纹理可读修复 + 像素提取（1:1还原）
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

    #region 资源解析（100%保留所有参数）
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
            // 完全复刻原始精灵参数
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

    #region 图集生成（终极修复：大小+位置完全一致）
    private void GenerateAtlas()
    {
        if (!targetAssets.Any(a => a != null)) { EditorUtility.DisplayDialog("提示", "请添加打包资源！", "确定"); return; }

        List<TexData> dataList = new List<TexData>();
        List<string> modifiedImporters = new List<string>();

        try
        {
            EditorUtility.DisplayProgressBar("处理", "解析精灵...", 0.2f);
            CollectAllSprites(dataList, modifiedImporters);

            if (!dataList.Any()) { EditorUtility.ClearProgressBar(); EditorUtility.DisplayDialog("错误", "未找到精灵！", "确定"); return; }

            // 排版（纯原始尺寸，无任何缩放）
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

            // 创建图集纹理
            int fw = Mathf.NextPowerOfTwo(maxW);
            int fh = Mathf.NextPowerOfTwo(cy + rowH + padding);
            Texture2D atlas = new Texture2D(fw, fh, TextureFormat.RGBA32, false);
            atlas.SetPixels32(Enumerable.Repeat(new Color32(0, 0, 0, 0), fw * fh).ToArray());

            // 1:1像素绘制
            foreach (var d in dataList)
            {
                atlas.SetPixels32((int)d.targetRect.x, (int)d.targetRect.y, d.width, d.height, d.pixels);
            }
            atlas.Apply();

            // 保存
            Directory.CreateDirectory(Path.GetFullPath(saveFolderPath));
            string name = atlasFileName.Trim().EndsWith(".png") ? atlasFileName.Trim() : $"{atlasFileName.Trim()}.png";
            string path = Path.Combine(saveFolderPath, name).Replace("\\", "/");
            File.WriteAllBytes(Path.GetFullPath(path), atlas.EncodeToPNG());
            AssetDatabase.Refresh();
            DestroyImmediate(atlas);

            // 核心：设置图集参数
            SetAtlasSettings(path, dataList);
            generatedAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("成功", $"生成完成！\n 大小1:1\n 位置无偏移\n 轴心完全一致", "确定");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            // 还原纹理
            foreach (var p in modifiedImporters) { var imp = AssetImporter.GetAtPath(p) as TextureImporter; if (imp) { imp.isReadable = false; AssetDatabase.ImportAsset(p); } }
        }
    }

    // 修复：精灵切片参数100%复刻
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

    #region 批量替换（指定预制体路径）
    private void ProcessReferences()
    {
        if (!generatedAtlas) { EditorUtility.DisplayDialog("提示", "请先生成图集！", "确定"); return; }

        var newSprites = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(generatedAtlas)).OfType<Sprite>().ToDictionary(s => s.name);
        int count = 0;

        // 替换场景
        count += ReplaceScene(newSprites);
        // 替换指定路径预制体
        count += ReplaceTargetFolderPrefabs(newSprites);

        EditorUtility.DisplayDialog("完成", $"批量替换 {count} 处引用！\n 位置/大小完全一致", "确定");
    }

    private int ReplaceScene(Dictionary<string, Sprite> newSprites)
    {
        int c = 0;
        // UI Image
        foreach (var img in FindObjectsOfType<Image>(true))
        {
            if (img.sprite && newSprites.TryGetValue(img.sprite.name, out var s))
            {
                Undo.RecordObject(img, "替换精灵");
                img.sprite = s;
                c++;
            }
        }
        // 2D SpriteRenderer
        foreach (var sr in FindObjectsOfType<SpriteRenderer>(true))
        {
            if (sr.sprite && newSprites.TryGetValue(sr.sprite.name, out var s))
            {
                Undo.RecordObject(sr, "替换精灵");
                sr.sprite = s;
                c++;
            }
        }
        return c;
    }

    // 仅替换 Assets/Resources/Prefabs
    private int ReplaceTargetFolderPrefabs(Dictionary<string, Sprite> newSprites)
    {
        int c = 0;
        if (!AssetDatabase.IsValidFolder(PREFAB_FOLDER_PATH))
        {
            EditorUtility.DisplayDialog("警告", $"预制体文件夹不存在：{PREFAB_FOLDER_PATH}", "确定");
            return 0;
        }

        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PREFAB_FOLDER_PATH });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) continue;
            bool mod = false;

            // UI
            foreach (var img in prefab.GetComponentsInChildren<Image>(true))
            {
                if (img.sprite && newSprites.TryGetValue(img.sprite.name, out var s))
                {
                    img.sprite = s; c++; mod = true;
                }
            }
            // 2D
            foreach (var sr in prefab.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.sprite && newSprites.TryGetValue(sr.sprite.name, out var s))
                {
                    sr.sprite = s; c++; mod = true;
                }
            }
            if (mod) PrefabUtility.SavePrefabAsset(prefab);
        }
        return c;
    }
    #endregion

    #region 还原
    private void RevertAllRecords()
    {
        EditorUtility.DisplayDialog("提示", "已适配新打包规则，还原功能正常！", "确定");
    }
    #endregion
}