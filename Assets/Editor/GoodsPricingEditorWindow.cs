using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class GoodsPricingEditorWindow : EditorWindow
{
    private const string DefaultGoodsFolder = "Assets/Resources/GameInfo/GoodInfo";

    private static readonly string[] QualityFieldNames =
    {
        "quality",
        "Quality",
        "goodsQuality",
        "GoodsQuality",
        "SkinQuality"
    };

    private readonly List<GoodsData> goodsList = new List<GoodsData>();
    private DefaultAsset folderAsset;
    private string goodsFolder = DefaultGoodsFolder;
    private string searchText = string.Empty;
    private Vector2 scrollPosition;
    private int currentIndex;
    private bool onlyWithIcon = true;
    private bool autoSave = true;
    private int bulkPriceDelta = 10;
    private bool bulkUseCurrentList = true;
    private bool bulkFilterByQuality;
    private GoodsQuality bulkQuality = GoodsQuality.Normal;
    private bool bulkFilterByType;
    private SkinType bulkSkinType = SkinType.PlayerCharacter;
    private int bulletPackFixedPrice = 100;

    [MenuItem("Tools/Goods/商品快速定价")]
    public static void Open()
    {
        GoodsPricingEditorWindow window = GetWindow<GoodsPricingEditorWindow>("商品快速定价");
        window.minSize = new Vector2(380f, 520f);
        window.Show();
    }

    private void OnEnable()
    {
        folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(goodsFolder);
        RefreshGoods();
    }

    private void OnProjectChange()
    {
        RefreshGoods(false);
        Repaint();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawBulkPriceTools();

        if (goodsList.Count == 0)
        {
            EditorGUILayout.HelpBox("没有找到符合条件的 GoodsData。可以关闭“只显示有图标商品”，或检查商品文件夹。", MessageType.Info);
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, goodsList.Count - 1);
        GoodsData goods = goodsList[currentIndex];

        DrawNavigation();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawCurrentGoods(goods);
        EditorGUILayout.EndScrollView();

        HandleKeyboardNavigation();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.Space(6f);

        EditorGUI.BeginChangeCheck();
        folderAsset = (DefaultAsset)EditorGUILayout.ObjectField("商品文件夹", folderAsset, typeof(DefaultAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            string selectedPath = AssetDatabase.GetAssetPath(folderAsset);
            if (!string.IsNullOrEmpty(selectedPath) && Directory.Exists(selectedPath))
            {
                goodsFolder = selectedPath;
                RefreshGoods();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            searchText = EditorGUILayout.TextField("搜索", searchText);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshGoods();
            }

            if (GUILayout.Button("刷新", GUILayout.Width(64f)))
            {
                RefreshGoods();
            }
        }

        EditorGUI.BeginChangeCheck();
        onlyWithIcon = EditorGUILayout.ToggleLeft("只显示有图标商品", onlyWithIcon);
        autoSave = EditorGUILayout.ToggleLeft("修改后自动保存", autoSave);
        if (EditorGUI.EndChangeCheck())
        {
            RefreshGoods();
        }

        EditorGUILayout.Space(4f);
    }

    private void DrawBulkPriceTools()
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("批量调价", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            bulkPriceDelta = Mathf.Max(0, EditorGUILayout.IntField("调整金额", bulkPriceDelta));
            bulkUseCurrentList = EditorGUILayout.ToggleLeft("只调整当前列表", bulkUseCurrentList);

            bulkFilterByQuality = EditorGUILayout.ToggleLeft("按品质筛选", bulkFilterByQuality);
            using (new EditorGUI.DisabledScope(!bulkFilterByQuality))
            {
                bulkQuality = (GoodsQuality)EditorGUILayout.EnumPopup("目标品质", bulkQuality);
            }

            bulkFilterByType = EditorGUILayout.ToggleLeft("按商品种类筛选", bulkFilterByType);
            using (new EditorGUI.DisabledScope(!bulkFilterByType))
            {
                bulkSkinType = (SkinType)EditorGUILayout.EnumPopup("目标种类", bulkSkinType);
            }

            int targetCount = CountBulkTargets();
            EditorGUILayout.LabelField("将影响商品", targetCount + " 个");

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = targetCount > 0 && bulkPriceDelta > 0;
                if (GUILayout.Button("加价 +" + bulkPriceDelta))
                {
                    ApplyBulkPriceDelta(bulkPriceDelta);
                }

                if (GUILayout.Button("降价 -" + bulkPriceDelta))
                {
                    ApplyBulkPriceDelta(-bulkPriceDelta);
                }
                GUI.enabled = true;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("子弹捆绑包统一定价", EditorStyles.boldLabel);
            bulletPackFixedPrice = Mathf.Max(0, EditorGUILayout.IntField("统一价格", bulletPackFixedPrice));

            int bulletPackCount = CountBulletPackGoods();
            EditorGUILayout.LabelField("将配置子弹捆绑包", bulletPackCount + " 个");

            GUI.enabled = bulletPackCount > 0;
            if (GUILayout.Button("一键设置子弹捆绑包价格"))
            {
                ApplyBulletPackFixedPrice();
            }
            GUI.enabled = true;
        }

        EditorGUILayout.Space(4f);
    }

    private void DrawNavigation()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUI.enabled = goodsList.Count > 1;
            if (GUILayout.Button("上一个", EditorStyles.toolbarButton, GUILayout.Width(72f)))
            {
                Move(-1);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label((currentIndex + 1) + " / " + goodsList.Count, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("下一个", EditorStyles.toolbarButton, GUILayout.Width(72f)))
            {
                Move(1);
            }
            GUI.enabled = true;
        }
    }

    private void DrawCurrentGoods(GoodsData goods)
    {
        EditorGUILayout.Space(8f);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawIcon(goods);

            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("商品名", GetDisplayName(goods));
                EditorGUILayout.LabelField("资源名", goods.name);
                EditorGUILayout.LabelField("类型", goods.skinType.ToString());

                if (GUILayout.Button("在 Project 中定位", GUILayout.Width(130f)))
                {
                    Selection.activeObject = goods;
                    EditorGUIUtility.PingObject(goods);
                }
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("资源路径", AssetDatabase.GetAssetPath(goods), EditorStyles.miniLabel);
        EditorGUILayout.Space(8f);

        EditorGUI.BeginChangeCheck();
        int price = Mathf.Max(0, EditorGUILayout.IntField("价格", goods.goodsPrice));
        GoodsQuality quality = (GoodsQuality)EditorGUILayout.EnumPopup("品质", goods.quality);
        if (EditorGUI.EndChangeCheck())
        {
            ApplyGoodsChanges(goods, price, quality);
        }
    }

    private void DrawIcon(GoodsData goods)
    {
        Sprite icon = GetDisplaySprite(goods);
        Rect iconRect = GUILayoutUtility.GetRect(112f, 112f, GUILayout.Width(112f), GUILayout.Height(112f));
        GUI.Box(iconRect, GUIContent.none);

        if (icon == null)
        {
            EditorGUI.LabelField(iconRect, "无图标", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        Texture2D preview = AssetPreview.GetAssetPreview(icon);
        if (preview == null)
        {
            preview = icon.texture;
        }

        if (preview != null)
        {
            Rect paddedRect = new Rect(iconRect.x + 6f, iconRect.y + 6f, iconRect.width - 12f, iconRect.height - 12f);
            GUI.DrawTexture(paddedRect, preview, ScaleMode.ScaleToFit, true);
        }
    }

    private void ApplyGoodsChanges(GoodsData goods, int price, GoodsQuality quality)
    {
        bool qualityChanged = goods.quality != quality;
        bool priceChanged = goods.goodsPrice != price;

        if (!priceChanged && !qualityChanged)
        {
            return;
        }

        Undo.RecordObject(goods, "Edit Goods Pricing");
        goods.goodsPrice = price;
        goods.quality = quality;
        EditorUtility.SetDirty(goods);

        if (qualityChanged)
        {
            SyncLinkedQuality(goods, quality);
        }

        if (autoSave)
        {
            AssetDatabase.SaveAssets();
        }
    }

    private int CountBulkTargets()
    {
        return GetBulkTargets().Count;
    }

    private void ApplyBulkPriceDelta(int delta)
    {
        List<GoodsData> targets = GetBulkTargets();
        if (targets.Count == 0)
        {
            return;
        }

        UnityEngine.Object[] undoTargets = new UnityEngine.Object[targets.Count];
        for (int i = 0; i < targets.Count; i++)
        {
            undoTargets[i] = targets[i];
        }

        Undo.RecordObjects(undoTargets, "Bulk Edit Goods Prices");

        foreach (GoodsData goods in targets)
        {
            int newPrice = Mathf.Max(0, goods.goodsPrice + delta);
            if (newPrice == goods.goodsPrice)
            {
                continue;
            }

            goods.goodsPrice = newPrice;
            EditorUtility.SetDirty(goods);
        }

        if (autoSave)
        {
            AssetDatabase.SaveAssets();
        }

        RefreshGoods();
    }

    private int CountBulletPackGoods()
    {
        int count = 0;
        List<GoodsData> allGoods = LoadAllGoodsInFolder();
        foreach (GoodsData goods in allGoods)
        {
            if (IsBulletPackGoods(goods))
            {
                count++;
            }
        }

        return count;
    }

    private void ApplyBulletPackFixedPrice()
    {
        List<GoodsData> targets = new List<GoodsData>();
        List<GoodsData> allGoods = LoadAllGoodsInFolder();
        foreach (GoodsData goods in allGoods)
        {
            if (IsBulletPackGoods(goods))
            {
                targets.Add(goods);
            }
        }

        if (targets.Count == 0)
        {
            return;
        }

        UnityEngine.Object[] undoTargets = new UnityEngine.Object[targets.Count];
        for (int i = 0; i < targets.Count; i++)
        {
            undoTargets[i] = targets[i];
        }

        Undo.RecordObjects(undoTargets, "Set Bullet Pack Goods Prices");

        foreach (GoodsData goods in targets)
        {
            if (goods.goodsPrice == bulletPackFixedPrice)
            {
                continue;
            }

            goods.goodsPrice = bulletPackFixedPrice;
            EditorUtility.SetDirty(goods);
        }

        if (autoSave)
        {
            AssetDatabase.SaveAssets();
        }

        RefreshGoods();
    }

    private bool IsBulletPackGoods(GoodsData goods)
    {
        return goods != null && (goods.skinType == SkinType.SpecialBullet || goods.bulletPack != null);
    }

    private List<GoodsData> GetBulkTargets()
    {
        List<GoodsData> targets = new List<GoodsData>();
        List<GoodsData> source = bulkUseCurrentList ? goodsList : LoadAllGoodsInFolder();

        foreach (GoodsData goods in source)
        {
            if (goods == null || !MatchesBulkFilter(goods))
            {
                continue;
            }

            targets.Add(goods);
        }

        return targets;
    }

    private bool MatchesBulkFilter(GoodsData goods)
    {
        if (bulkFilterByQuality && goods.quality != bulkQuality)
        {
            return false;
        }

        if (bulkFilterByType && goods.skinType != bulkSkinType)
        {
            return false;
        }

        return true;
    }

    private List<GoodsData> LoadAllGoodsInFolder()
    {
        List<GoodsData> allGoods = new List<GoodsData>();
        if (string.IsNullOrEmpty(goodsFolder) || !Directory.Exists(goodsFolder))
        {
            return allGoods;
        }

        string[] guids = AssetDatabase.FindAssets("t:GoodsData", new[] { goodsFolder });
        Array.Sort(guids, StringComparer.Ordinal);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GoodsData goods = AssetDatabase.LoadAssetAtPath<GoodsData>(path);
            if (goods != null)
            {
                allGoods.Add(goods);
            }
        }

        return allGoods;
    }

    private void SyncLinkedQuality(GoodsData goods, GoodsQuality quality)
    {
        TrySyncQuality(goods.playerSkinPack, quality);
        TrySyncQuality(goods.gunSkinPack, quality);
        TrySyncQuality(goods.gunHitData, quality);
        TrySyncQuality(goods.bulletPack, quality);

        if (goods.expressionPacks == null)
        {
            return;
        }

        foreach (ExpressionPack expressionPack in goods.expressionPacks)
        {
            TrySyncQuality(expressionPack, quality);
        }
    }

    private void TrySyncQuality(UnityEngine.Object target, GoodsQuality quality)
    {
        if (target == null)
        {
            return;
        }

        Type targetType = target.GetType();
        foreach (string fieldName in QualityFieldNames)
        {
            FieldInfo field = targetType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null || field.FieldType != typeof(GoodsQuality))
            {
                continue;
            }

            GoodsQuality currentValue = (GoodsQuality)field.GetValue(target);
            if (currentValue == quality)
            {
                return;
            }

            Undo.RecordObject(target, "Sync Goods Quality");
            field.SetValue(target, quality);
            EditorUtility.SetDirty(target);
            return;
        }
    }

    private void RefreshGoods(bool keepCurrent = true)
    {
        GoodsData currentGoods = keepCurrent && goodsList.Count > 0 && currentIndex < goodsList.Count ? goodsList[currentIndex] : null;
        goodsList.Clear();

        if (string.IsNullOrEmpty(goodsFolder) || !Directory.Exists(goodsFolder))
        {
            goodsFolder = DefaultGoodsFolder;
            folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(goodsFolder);
        }

        string[] guids = AssetDatabase.FindAssets("t:GoodsData", new[] { goodsFolder });
        Array.Sort(guids, StringComparer.Ordinal);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GoodsData goods = AssetDatabase.LoadAssetAtPath<GoodsData>(path);
            if (goods == null)
            {
                continue;
            }

            if (onlyWithIcon && GetDisplaySprite(goods) == null)
            {
                continue;
            }

            if (!MatchesSearch(goods))
            {
                continue;
            }

            goodsList.Add(goods);
        }

        if (currentGoods != null)
        {
            int keptIndex = goodsList.IndexOf(currentGoods);
            if (keptIndex >= 0)
            {
                currentIndex = keptIndex;
                return;
            }
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, goodsList.Count - 1));
    }

    private bool MatchesSearch(GoodsData goods)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        string normalizedSearch = searchText.Trim();
        return Contains(goods.goodsName, normalizedSearch)
            || Contains(goods.name, normalizedSearch)
            || Contains(goods.skinType.ToString(), normalizedSearch)
            || Contains(goods.quality.ToString(), normalizedSearch);
    }

    private bool Contains(string value, string search)
    {
        return !string.IsNullOrEmpty(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void Move(int direction)
    {
        if (goodsList.Count == 0)
        {
            return;
        }

        currentIndex = (currentIndex + direction + goodsList.Count) % goodsList.Count;
        GUI.FocusControl(null);
    }

    private void HandleKeyboardNavigation()
    {
        Event currentEvent = Event.current;
        if (currentEvent.type != EventType.KeyDown)
        {
            return;
        }

        if (currentEvent.keyCode == KeyCode.LeftArrow)
        {
            Move(-1);
            currentEvent.Use();
        }
        else if (currentEvent.keyCode == KeyCode.RightArrow)
        {
            Move(1);
            currentEvent.Use();
        }
    }

    private Sprite GetDisplaySprite(GoodsData goods)
    {
        if (goods.goodsIcon != null)
        {
            return goods.goodsIcon;
        }

        if (goods.playerSkinPack != null && goods.playerSkinPack.IdleSprite != null)
        {
            return goods.playerSkinPack.IdleSprite;
        }

        if (goods.gunSkinPack != null && goods.gunSkinPack.skinIcon != null)
        {
            return goods.gunSkinPack.skinIcon;
        }

        if (goods.gunHitData != null && goods.gunHitData.HitIcon != null)
        {
            return goods.gunHitData.HitIcon;
        }

        if (goods.bulletPack != null && goods.bulletPack.Sprite != null)
        {
            return goods.bulletPack.Sprite;
        }

        if (goods.expressionPacks != null)
        {
            foreach (ExpressionPack expressionPack in goods.expressionPacks)
            {
                if (expressionPack != null && expressionPack.ExpressionSprite != null)
                {
                    return expressionPack.ExpressionSprite;
                }
            }
        }

        return null;
    }

    private string GetDisplayName(GoodsData goods)
    {
        return string.IsNullOrEmpty(goods.goodsName) ? goods.name : goods.goodsName;
    }
}
