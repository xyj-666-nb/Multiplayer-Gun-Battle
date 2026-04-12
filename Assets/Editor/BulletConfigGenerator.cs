using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


public class BulletGeneratorWithFix : EditorWindow
{
    // 枪械-中文名映射（和你项目完全匹配）
    private static readonly string[] GunNames = { "步枪", "冲锋枪", "射手步枪", "机枪", "栓动步枪" };
    private static readonly GunType[] GunTypes = { GunType.Rifle, GunType.Charge, GunType.DMR, GunType.LightMachineGun, GunType.Snipe };

    // 颜色系列配置（和你现有文件命名/颜色完全匹配）
    private class ColorConfig
    {
        public string Name;
        public bool IsBase;
        public Color BulletColor;
        public Color CaseColor;
        public Color LightStart;
        public Color LightEnd;
        public int Price;
        public GoodsQuality Quality;
    }

    private static ColorConfig[] Colors = new ColorConfig[]
    {
        new ColorConfig() { Name = "基础", IsBase = true, BulletColor = new Color(0.83f, 0.68f, 0.22f), CaseColor = new Color(0.83f, 0.68f, 0.22f), LightStart = Color.white, LightEnd = new Color(1f, 0.5f, 0f), Price = 0, Quality = GoodsQuality.Normal },
        new ColorConfig() { Name = "粉色", IsBase = false, BulletColor = new Color(1f, 0.2f, 0.8f), CaseColor = new Color(1f, 0.3f, 0.85f), LightStart = new Color(1f, 0.4f, 1f), LightEnd = new Color(1f, 0.8f, 1f), Price = 30, Quality = GoodsQuality.Normal },
        new ColorConfig() { Name = "紫色", IsBase = false, BulletColor = new Color(0.6f, 0.2f, 1f), CaseColor = new Color(0.7f, 0.3f, 1f), LightStart = new Color(0.8f, 0.4f, 1f), LightEnd = new Color(0.9f, 0.7f, 1f), Price = 30, Quality = GoodsQuality.Normal },
        new ColorConfig() { Name = "绿色", IsBase = false, BulletColor = new Color(0.2f, 1f, 0.2f), CaseColor = new Color(0.3f, 1f, 0.3f), LightStart = new Color(0.4f, 1f, 0.4f), LightEnd = new Color(0.8f, 1f, 0.8f), Price = 30, Quality = GoodsQuality.Normal },
        new ColorConfig() { Name = "蓝色", IsBase = false, BulletColor = new Color(0.2f, 0.4f, 1f), CaseColor = new Color(0.3f, 0.5f, 1f), LightStart = new Color(0.4f, 0.6f, 1f), LightEnd = new Color(0.7f, 0.8f, 1f), Price = 30, Quality = GoodsQuality.Normal },
        new ColorConfig() { Name = "青色", IsBase = false, BulletColor = new Color(0.2f, 1f, 1f), CaseColor = new Color(0.3f, 1f, 1f), LightStart = new Color(0.4f, 1f, 1f), LightEnd = new Color(0.8f, 1f, 1f), Price = 30, Quality = GoodsQuality.Normal },
    };

    // 项目路径（和你完全匹配）
    private const string ROOT_FOLDER = "Assets/Resources/GameInfo";
    private string BulletFolder => ROOT_FOLDER + "/BulletInfo";
    private string FlashFolder => ROOT_FOLDER + "/MuzzleFlashInfo";
    private string BindFolder => ROOT_FOLDER + "/BulletBindPack";
    private string GoodsFolder => ROOT_FOLDER + "/GoodInfo";

    [MenuItem("游戏工具/子弹生成器(带修复功能)")]
    public static void ShowWindow()
    {
        GetWindow<BulletGeneratorWithFix>("子弹生成器(带修复)");
    }

    private void OnGUI()
    {
        GUILayout.Label(" 已匹配项目路径：Assets/Resources/GameInfo", EditorStyles.boldLabel);
        GUILayout.Label("功能：生成缺失文件 + 修复已存在文件的关联/参数/颜色", EditorStyles.miniLabel);
        GUILayout.Space(20);

        if (GUILayout.Button("1. 生成缺失文件 + 修复已存在文件", GUILayout.Height(40)))
        {
            GenerateAndFixAll();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("2. 仅修复已存在文件（不生成新的）", GUILayout.Height(40)))
        {
            FixExistingOnly();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("3. 仅检查文件（不修改）", GUILayout.Height(30)))
        {
            CheckOnly();
        }
    }

    #region 核心功能：生成+修复
    private void GenerateAndFixAll()
    {
        Debug.Log("========== 开始【生成缺失+修复已存在】 ==========");
        InitFolders();

        // 获取现有文件最大ID，用于新文件生成
        var maxIds = GetMaxIDs();
        int currentBulletID = maxIds.bulletID + 1;
        int currentFlashID = maxIds.flashID + 1;
        int currentBindID = maxIds.bindID + 1;

        int fixCount = 0;
        int newCount = 0;

        // 遍历所有枪械+颜色
        foreach (var gunType in GunTypes)
        {
            string gunName = GunNames[(int)gunType];
            foreach (var color in Colors)
            {
                // 生成命名
                var names = GetFileNames(color, gunName);
                Debug.Log($"--- 处理：{color.Name} {gunName} ---");

                // ============== 1. 处理子弹视觉配置 ==============
                BulletVisualConfig bulletConfig = null;
                if (File.Exists(names.bulletPath))
                {
                    // 已存在：加载并修复
                    bulletConfig = AssetDatabase.LoadAssetAtPath<BulletVisualConfig>(names.bulletPath);
                    if (FixBulletConfig(bulletConfig, color, gunType)) fixCount++;
                }
                else
                {
                    // 不存在：生成新的
                    bulletConfig = CreateNewBulletConfig(names.bulletPath, color, gunType, ref currentBulletID);
                    newCount++;
                }

                // ============== 2. 处理火光配置 ==============
                MuzzleFlashConfig flashConfig = null;
                if (File.Exists(names.flashPath))
                {
                    // 已存在：加载并修复
                    flashConfig = AssetDatabase.LoadAssetAtPath<MuzzleFlashConfig>(names.flashPath);
                    if (FixFlashConfig(flashConfig, color, gunType)) fixCount++;
                }
                else
                {
                    // 不存在：生成新的
                    flashConfig = CreateNewFlashConfig(names.flashPath, color, gunType, ref currentFlashID);
                    newCount++;
                }

                // ============== 3. 处理子弹捆绑包 ==============
                SpecialBulletBindPack bindPack = null;
                if (File.Exists(names.bindPath))
                {
                    // 已存在：加载并修复
                    bindPack = AssetDatabase.LoadAssetAtPath<SpecialBulletBindPack>(names.bindPath);
                    if (FixBindPack(bindPack, color, gunType, bulletConfig, flashConfig)) fixCount++;
                }
                else
                {
                    // 不存在：生成新的
                    bindPack = CreateNewBindPack(names.bindPath, color, gunType, bulletConfig, flashConfig, ref currentBindID);
                    newCount++;
                }

                // ============== 4. 处理商品配置 ==============
                if (File.Exists(names.goodsPath))
                {
                    // 已存在：加载并修复
                    var goodsData = AssetDatabase.LoadAssetAtPath<GoodsData>(names.goodsPath);
                    if (FixGoodsData(goodsData, color, gunType, bindPack)) fixCount++;
                }
                else
                {
                    // 不存在：生成新的
                    CreateNewGoodsData(names.goodsPath, color, gunType, bindPack);
                    newCount++;
                }
            }
        }

        // 最终保存刷新
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        JumpToRootFolder();

        Debug.Log($"========== 处理完成！新增文件：{newCount} 个，修复文件：{fixCount} 个 ==========");
        EditorUtility.DisplayDialog("处理完成", $" 处理完成！\n新增文件：{newCount} 个\n修复文件：{fixCount} 个\n详细日志请查看Console窗口", "好的");
    }
    #endregion

    #region 仅修复已存在
    private void FixExistingOnly()
    {
        Debug.Log("========== 开始【仅修复已存在文件】 ==========");
        InitFolders();

        int fixCount = 0;

        foreach (var gunType in GunTypes)
        {
            string gunName = GunNames[(int)gunType];
            foreach (var color in Colors)
            {
                var names = GetFileNames(color, gunName);
                Debug.Log($"--- 检查：{color.Name} {gunName} ---");

                // 加载所有已存在的文件
                var bulletConfig = AssetDatabase.LoadAssetAtPath<BulletVisualConfig>(names.bulletPath);
                var flashConfig = AssetDatabase.LoadAssetAtPath<MuzzleFlashConfig>(names.flashPath);
                var bindPack = AssetDatabase.LoadAssetAtPath<SpecialBulletBindPack>(names.bindPath);
                var goodsData = AssetDatabase.LoadAssetAtPath<GoodsData>(names.goodsPath);

                // 逐个修复
                if (bulletConfig != null && FixBulletConfig(bulletConfig, color, gunType)) fixCount++;
                if (flashConfig != null && FixFlashConfig(flashConfig, color, gunType)) fixCount++;
                if (bindPack != null && FixBindPack(bindPack, color, gunType, bulletConfig, flashConfig)) fixCount++;
                if (goodsData != null && FixGoodsData(goodsData, color, gunType, bindPack)) fixCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        JumpToRootFolder();

        Debug.Log($"========== 修复完成！共修复 {fixCount} 个文件 ==========");
        EditorUtility.DisplayDialog("修复完成", $" 修复完成！\n共修复 {fixCount} 个文件\n详细日志请查看Console窗口", "好的");
    }
    #endregion

    #region 仅检查不修改
    private void CheckOnly()
    {
        Debug.Log("========== 开始【仅检查文件】 ==========");
        InitFolders();

        int missingCount = 0;
        int errorCount = 0;

        foreach (var gunType in GunTypes)
        {
            string gunName = GunNames[(int)gunType];
            foreach (var color in Colors)
            {
                var names = GetFileNames(color, gunName);
                bool hasMissing = false;

                // 检查文件是否存在
                if (!File.Exists(names.bulletPath)) { Debug.LogWarning($" 缺失：{names.bulletPath}"); hasMissing = true; missingCount++; }
                if (!File.Exists(names.flashPath)) { Debug.LogWarning($" 缺失：{names.flashPath}"); hasMissing = true; missingCount++; }
                if (!File.Exists(names.bindPath)) { Debug.LogWarning($" 缺失：{names.bindPath}"); hasMissing = true; missingCount++; }
                if (!File.Exists(names.goodsPath)) { Debug.LogWarning($" 缺失：{names.goodsPath}"); hasMissing = true; missingCount++; }

                if (hasMissing) continue;

                // 检查关联是否正确
                var bulletConfig = AssetDatabase.LoadAssetAtPath<BulletVisualConfig>(names.bulletPath);
                var flashConfig = AssetDatabase.LoadAssetAtPath<MuzzleFlashConfig>(names.flashPath);
                var bindPack = AssetDatabase.LoadAssetAtPath<SpecialBulletBindPack>(names.bindPath);
                var goodsData = AssetDatabase.LoadAssetAtPath<GoodsData>(names.goodsPath);

                if (bindPack.bulletVisualConfig != bulletConfig) { Debug.LogWarning($" 关联错误：{names.bindName} 子弹配置关联错误"); errorCount++; }
                if (bindPack.muzzleFlashConfig != flashConfig) { Debug.LogWarning($" 关联错误：{names.bindName} 火光配置关联错误"); errorCount++; }
                if (goodsData.bulletPack != bindPack) { Debug.LogWarning($" 关联错误：{names.goodsName} 捆绑包关联错误"); errorCount++; }
                if (bulletConfig.gunType != gunType) { Debug.LogWarning($" 参数错误：{names.bulletName} GunType错误"); errorCount++; }
                if (flashConfig.gunType != gunType) { Debug.LogWarning($" 参数错误：{names.flashName} GunType错误"); errorCount++; }
                if (bindPack.gunType != gunType) { Debug.LogWarning($" 参数错误：{names.bindName} GunType错误"); errorCount++; }
            }
        }

        Debug.Log($"========== 检查完成！缺失文件：{missingCount} 个，错误文件：{errorCount} 个 ==========");
        EditorUtility.DisplayDialog("检查完成", $"检查完成！\n缺失文件：{missingCount} 个\n错误文件：{errorCount} 个\n详细日志请查看Console窗口", "好的");
    }
    #endregion

    #region 修复逻辑（核心）
    /// <summary>
    /// 修复子弹视觉配置
    /// </summary>
    private bool FixBulletConfig(BulletVisualConfig config, ColorConfig color, GunType gunType)
    {
        bool isChanged = false;

        // 修正GunType
        if (config.gunType != gunType)
        {
            config.gunType = gunType;
            Debug.Log($" 修复：{config.name} GunType修正为 {gunType}");
            isChanged = true;
        }

        // 修正子弹颜色
        if (config.bulletColor != color.BulletColor)
        {
            config.bulletColor = color.BulletColor;
            Debug.Log($" 修复：{config.name} 子弹颜色修正");
            isChanged = true;
        }

        // 修正弹壳颜色
        if (config.cartridgeCaseColor != color.CaseColor)
        {
            config.cartridgeCaseColor = color.CaseColor;
            Debug.Log($" 修复：{config.name} 弹壳颜色修正");
            isChanged = true;
        }

        // 修正默认参数（如果是0的话）
        if (config.bulletSegmentLength <= 0) { config.bulletSegmentLength = 0.2f; isChanged = true; }
        if (config.bulletLineWidth <= 0) { config.bulletLineWidth = 0.03f; isChanged = true; }
        if (config.bulletFlySpeed <= 0) { config.bulletFlySpeed = 80f; isChanged = true; }
        if (config.bulletShowDuration <= 0) { config.bulletShowDuration = 0.5f; isChanged = true; }
        if (config.cartridgeCaseSize <= 0) { config.cartridgeCaseSize = 0.2f; isChanged = true; }

        if (isChanged) EditorUtility.SetDirty(config);
        return isChanged;
    }

    /// <summary>
    /// 修复火光配置
    /// </summary>
    private bool FixFlashConfig(MuzzleFlashConfig config, ColorConfig color, GunType gunType)
    {
        bool isChanged = false;

        // 修正GunType
        if (config.gunType != gunType)
        {
            config.gunType = gunType;
            Debug.Log($" 修复：{config.name} GunType修正为 {gunType}");
            isChanged = true;
        }

        // 修正火光颜色
        if (config.lightStartColor != color.LightStart)
        {
            config.lightStartColor = color.LightStart;
            Debug.Log($" 修复：{config.name} 火光起始颜色修正");
            isChanged = true;
        }
        if (config.lightEndColor != color.LightEnd)
        {
            config.lightEndColor = color.LightEnd;
            Debug.Log($" 修复：{config.name} 火光结束颜色修正");
            isChanged = true;
        }

        // 修正默认参数
        if (config.flashDuration <= 0) { config.flashDuration = 0.15f; isChanged = true; }
        if (config.lightMaxIntensity <= 0) { config.lightMaxIntensity = 12.92f; isChanged = true; }
        if (config.lightRadius <= 0) { config.lightRadius = 1.5f; isChanged = true; }
        if (!config.lock2DZAxis) { config.lock2DZAxis = true; isChanged = true; }

        if (isChanged) EditorUtility.SetDirty(config);
        return isChanged;
    }

    /// <summary>
    /// 修复子弹捆绑包
    /// </summary>
    private bool FixBindPack(SpecialBulletBindPack config, ColorConfig color, GunType gunType, BulletVisualConfig bulletConfig, MuzzleFlashConfig flashConfig)
    {
        bool isChanged = false;

        // 修正GunType
        if (config.gunType != gunType)
        {
            config.gunType = gunType;
            Debug.Log($" 修复：{config.name} GunType修正为 {gunType}");
            isChanged = true;
        }

        // 修正名称
        if (config.BulletBindName != config.name)
        {
            config.BulletBindName = config.name;
            Debug.Log($" 修复：{config.name} 名称修正");
            isChanged = true;
        }

        // 修正描述
        string targetDesc = color.IsBase ? $"基础{GunNames[(int)gunType]}子弹配置，默认弹道与火光效果" : $"专属配色曳光弹，弹体、弹壳、枪火全新，高亮弹道划破战场，每一枪都足够亮眼。";
        if (config.description != targetDesc)
        {
            config.description = targetDesc;
            Debug.Log($" 修复：{config.name} 描述修正");
            isChanged = true;
        }

        // 修复关联
        if (config.bulletVisualConfig != bulletConfig && bulletConfig != null)
        {
            config.bulletVisualConfig = bulletConfig;
            Debug.Log($" 修复：{config.name} 子弹配置关联修复");
            isChanged = true;
        }
        if (config.muzzleFlashConfig != flashConfig && flashConfig != null)
        {
            config.muzzleFlashConfig = flashConfig;
            Debug.Log($" 修复：{config.name} 火光配置关联修复");
            isChanged = true;
        }

        if (isChanged) EditorUtility.SetDirty(config);
        return isChanged;
    }

    /// <summary>
    /// 修复商品配置
    /// </summary>
    private bool FixGoodsData(GoodsData config, ColorConfig color, GunType gunType, SpecialBulletBindPack bindPack)
    {
        bool isChanged = false;

        // 修正类型
        if (config.skinType != SkinType.SpecialBullet)
        {
            config.skinType = SkinType.SpecialBullet;
            Debug.Log($" 修复：{config.name} 皮肤类型修正");
            isChanged = true;
        }

        // 修正名称
        string targetName = color.IsBase ? $"基础子弹({GunNames[(int)gunType]})" : $"{color.Name}曳光弹({GunNames[(int)gunType]})";
        if (config.goodsName != targetName)
        {
            config.goodsName = targetName;
            Debug.Log($" 修复：{config.name} 商品名称修正");
            isChanged = true;
        }

        // 修正描述
        string targetDesc = color.IsBase ? $"基础{GunNames[(int)gunType]}子弹配置" : $"专属配色曳光弹，{color.Name}系列";
        if (config.goodsDescription != targetDesc)
        {
            config.goodsDescription = targetDesc;
            Debug.Log($" 修复：{config.name} 商品描述修正");
            isChanged = true;
        }

        // 修正价格和品质
        if (config.goodsPrice != color.Price)
        {
            config.goodsPrice = color.Price;
            Debug.Log($" 修复：{config.name} 价格修正");
            isChanged = true;
        }
        if (config.quality != color.Quality)
        {
            config.quality = color.Quality;
            Debug.Log($" 修复：{config.name} 品质修正");
            isChanged = true;
        }

        // 修复关联
        if (config.bulletPack != bindPack && bindPack != null)
        {
            config.bulletPack = bindPack;
            Debug.Log($" 修复：{config.name} 捆绑包关联修复");
            isChanged = true;
        }

        // 自动生成GUID（如果为空）
        if (string.IsNullOrEmpty(config.goodsGuid))
        {
            config.goodsGuid = Guid.NewGuid().ToString();
            Debug.Log($" 修复：{config.name} 生成GUID");
            isChanged = true;
        }

        if (isChanged) EditorUtility.SetDirty(config);
        return isChanged;
    }
    #endregion

    #region 新建文件逻辑
    private BulletVisualConfig CreateNewBulletConfig(string path, ColorConfig color, GunType gunType, ref int id)
    {
        var config = CreateInstance<BulletVisualConfig>();
        config.BulletID = id++;
        config.gunType = gunType;
        config.bulletColor = color.BulletColor;
        config.cartridgeCaseColor = color.CaseColor;
        config.bulletSegmentLength = 0.2f;
        config.bulletLineWidth = 0.03f;
        config.bulletFlySpeed = 80f;
        config.bulletShowDuration = 0.5f;
        config.cartridgeCaseSize = 0.2f;
        config.smokeColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        config.smokeSizeMin = 0.5f;
        config.smokeSizeMax = 1.2f;
        config.smokeDuration = 0.3f;
        config.smokeDecaySpeed = 10f;

        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($" 新建：{path}");

        return AssetDatabase.LoadAssetAtPath<BulletVisualConfig>(path);
    }

    private MuzzleFlashConfig CreateNewFlashConfig(string path, ColorConfig color, GunType gunType, ref int id)
    {
        var config = CreateInstance<MuzzleFlashConfig>();
        config.MuzzleFlashID = id++;
        config.gunType = gunType;
        config.lightStartColor = color.LightStart;
        config.lightEndColor = color.LightEnd;
        config.flashDuration = 0.15f;
        config.lightMaxIntensity = 12.92f;
        config.lightRadius = 1.5f;
        config.lock2DZAxis = true;

        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($" 新建：{path}");

        return AssetDatabase.LoadAssetAtPath<MuzzleFlashConfig>(path);
    }

    private SpecialBulletBindPack CreateNewBindPack(string path, ColorConfig color, GunType gunType, BulletVisualConfig bullet, MuzzleFlashConfig flash, ref int id)
    {
        var config = CreateInstance<SpecialBulletBindPack>();
        config.BulletBindID = id++;
        config.gunType = gunType;
        config.BulletBindName = Path.GetFileNameWithoutExtension(path);
        config.description = color.IsBase ? $"基础{GunNames[(int)gunType]}子弹配置，默认弹道与火光效果" : $"专属配色曳光弹，弹体、弹壳、枪火全新，高亮弹道划破战场，每一枪都足够亮眼。";
        config.bulletVisualConfig = bullet;
        config.muzzleFlashConfig = flash;

        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($" 新建：{path}");

        return AssetDatabase.LoadAssetAtPath<SpecialBulletBindPack>(path);
    }

    private void CreateNewGoodsData(string path, ColorConfig color, GunType gunType, SpecialBulletBindPack bindPack)
    {
        var config = CreateInstance<GoodsData>();
        config.goodsGuid = Guid.NewGuid().ToString();
        config.skinType = SkinType.SpecialBullet;
        config.goodsName = color.IsBase ? $"基础子弹({GunNames[(int)gunType]})" : $"{color.Name}曳光弹({GunNames[(int)gunType]})";
        config.goodsDescription = color.IsBase ? $"基础{GunNames[(int)gunType]}子弹配置" : $"专属配色曳光弹，{color.Name}系列";
        config.goodsPrice = color.Price;
        config.quality = color.Quality;
        config.bulletPack = bindPack;

        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($" 新建：{path}");
    }
    #endregion

    #region 工具方法
    private void InitFolders()
    {
        CreateFolder(ROOT_FOLDER);
        CreateFolder(BulletFolder);
        CreateFolder(FlashFolder);
        CreateFolder(BindFolder);
        CreateFolder(GoodsFolder);
    }

    private void CreateFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
            Debug.Log($"创建文件夹：{path}");
        }
    }

    private (int bulletID, int flashID, int bindID) GetMaxIDs()
    {
        int maxBullet = 0;
        int maxFlash = 0;
        int maxBind = 0;

        var allBullets = AssetDatabase.FindAssets("t:BulletVisualConfig")
            .Select(g => AssetDatabase.LoadAssetAtPath<BulletVisualConfig>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(c => c != null);
        if (allBullets.Any()) maxBullet = allBullets.Max(c => c.BulletID);

        var allFlashes = AssetDatabase.FindAssets("t:MuzzleFlashConfig")
            .Select(g => AssetDatabase.LoadAssetAtPath<MuzzleFlashConfig>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(c => c != null);
        if (allFlashes.Any()) maxFlash = allFlashes.Max(c => c.MuzzleFlashID);

        var allBinds = AssetDatabase.FindAssets("t:SpecialBulletBindPack")
            .Select(g => AssetDatabase.LoadAssetAtPath<SpecialBulletBindPack>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(c => c != null);
        if (allBinds.Any()) maxBind = allBinds.Max(c => c.BulletBindID);

        return (maxBullet, maxFlash, maxBind);
    }

    private (string bulletName, string bulletPath, string flashName, string flashPath, string bindName, string bindPath, string goodsName, string goodsPath) GetFileNames(ColorConfig color, string gunName)
    {
        string bulletName = color.IsBase ? $"基础{gunName}子弹" : $"{color.Name}{gunName}曳光弹";
        string bulletPath = $"{BulletFolder}/{bulletName}.asset";

        string flashName = $"{color.Name}{gunName}火光";
        string flashPath = $"{FlashFolder}/{flashName}.asset";

        string bindName = color.IsBase ? $"基础{gunName}子弹捆绑包" : $"{color.Name}{gunName}曳光弹捆绑包";
        string bindPath = $"{BindFolder}/{bindName}.asset";

        string goodsName = color.IsBase ? $"基础子弹_{gunName}" : $"{color.Name}曳光弹_{gunName}";
        string goodsPath = $"{GoodsFolder}/{goodsName}.asset";

        return (bulletName, bulletPath, flashName, flashPath, bindName, bindPath, goodsName, goodsPath);
    }

    private void JumpToRootFolder()
    {
        var rootFolder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ROOT_FOLDER);
        EditorGUIUtility.PingObject(rootFolder);
        Selection.activeObject = rootFolder;
    }
    #endregion
}

