using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 关卡基础数据包
/// </summary>
[Serializable]
public class LevelPack
{
    public int Index;//关卡索引
    public string sceneName;//场景名字
    public int UnlockConditionLevel = 0; // 解锁条件：通关前N关
    public string Difficulty = "Normal"; // 难度
}

/// <summary>
/// 关卡管理器
/// </summary>
public class LevelManager : SingleMonoAutoBehavior<LevelManager>
{
    public LevelInfoPack CurrentLevelInfoPack;//当前关卡场景内的实时数据包
    public LevelPack CurrentLevelPack; // 当前关卡的静态配置
    public float LoadProgress; // 场景加载进度（0~1）

    // 核心回调
    public Action OnLevelLoadComplete; // 关卡加载完成
    public Action OnLevelWin; // 关卡胜利
    public Action OnLevelLose; // 关卡失败
    public Action<float> OnLevelLoadProgress; // 加载进度更新

    // 存档Key
    private const string KEY_GLOBAL_LEVEL_PROGRESS = "Global_Level_Progress"; // 全局已通关关卡
    private const string KEY_LEVEL_STATE = "Level_State_"; // 单关卡状态 Key + 关卡索引
    private const string KEY_LEVEL_CHECKPOINT = "Level_Checkpoint_"; // 单关卡复活点 Key + 关卡索引

    #region 核心：关卡场景加载/卸载
    /// <summary>
    /// 进入指定关卡场景
    /// </summary>
    /// <param name="levelPack">要进入的关卡配置</param>
    public void EnterLevelScene(LevelPack levelPack)
    {
        // 1. 校验关卡配置
        if (levelPack == null || string.IsNullOrEmpty(levelPack.sceneName))
        {
            Debug.LogError("关卡配置为空或场景名无效！");
            return;
        }

        // 2. 记录当前关卡配置
        CurrentLevelPack = levelPack;

        // 3. 异步加载场景（避免主线程卡顿，P1核心）
        StartCoroutine(LoadLevelSceneCoroutine(levelPack.sceneName));
    }

    /// <summary>
    /// 异步加载场景的协程（核心逻辑）
    /// </summary>
    private IEnumerator LoadLevelSceneCoroutine(string sceneName)
    {
        // 1. 先卸载当前非保留场景（如果有）
        if (SceneManager.sceneCount > 1)
        {
            for (int i = 1; i < SceneManager.sceneCount; i++)
            {
                yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i));
            }
        }

        // 2. 异步加载目标场景
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        asyncOp.allowSceneActivation = false; // 先不激活场景，等加载完成

        // 3. 实时返回加载进度
        while (asyncOp.progress < 0.9f) // Unity的progress到0.9代表加载完成
        {
            LoadProgress = asyncOp.progress;
            OnLevelLoadProgress?.Invoke(LoadProgress);
            yield return null;
        }

        // 4. 加载完成，激活场景
        LoadProgress = 1f;
        OnLevelLoadProgress?.Invoke(LoadProgress);
        asyncOp.allowSceneActivation = true;

        // 5. 等待场景激活完成
        while (!asyncOp.isDone)
        {
            yield return null;
        }

        // 6. 切换到加载的场景
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        Debug.Log($"关卡场景 {sceneName} 加载完成！");

        // 7. 触发加载完成回调
        OnLevelLoadComplete?.Invoke();
    }

    /// <summary>
    /// 退出关卡场景（卸载+存档，P1核心）
    /// </summary>
    public void ExitLevelScene()
    {
        if (CurrentLevelInfoPack == null)
        {
            Debug.LogWarning("当前无运行中的关卡！");
            return;
        }

        // 1. 保存当前关卡进度（P1核心：自动存档）
        SaveLevelProgress(CurrentLevelInfoPack.LevelIndex);

        // 2. 保存全局进度
        SaveGlobalProgress();

        // 3. 卸载当前关卡场景
        StartCoroutine(UnloadLevelSceneCoroutine(CurrentLevelInfoPack.gameObject.scene.name));

        // 4. 重置管理器状态
        CurrentLevelInfoPack = null;
        CurrentLevelPack = null;
    }

    /// <summary>
    /// 卸载场景的协程
    /// </summary>
    private IEnumerator UnloadLevelSceneCoroutine(string sceneName)
    {
        AsyncOperation asyncOp = SceneManager.UnloadSceneAsync(sceneName);
        while (!asyncOp.isDone)
        {
            yield return null;
        }
        // 清理未使用的资源（避免内存泄漏，P1核心）
        Resources.UnloadUnusedAssets();
        Debug.Log($"关卡场景 {sceneName} 卸载完成！");
    }
    #endregion

    #region 进度存档/读档（P1核心）
    /// <summary>
    /// 保存单关卡进度（自动存档核心）
    /// </summary>
    /// <param name="levelIndex">关卡索引</param>
    public void SaveLevelProgress(int levelIndex)
    {
        if (CurrentLevelInfoPack == null) return;

        // 1. 保存关卡状态
        PlayerPrefs.SetInt(KEY_LEVEL_STATE + levelIndex, (int)CurrentLevelInfoPack.CurrentLevelState);
        // 2. 保存复活点（示例：用Vector3的字符串存储）
        if (CurrentLevelInfoPack.PlayerBornPos != null)
        {
            string posStr = $"{CurrentLevelInfoPack.PlayerBornPos.position.x}|{CurrentLevelInfoPack.PlayerBornPos.position.y}|{CurrentLevelInfoPack.PlayerBornPos.position.z}";
            PlayerPrefs.SetString(KEY_LEVEL_CHECKPOINT + levelIndex, posStr);
        }
        // 3. 提交存档（必须调用，否则数据不生效）
        PlayerPrefs.Save();
        Debug.Log($"关卡 {levelIndex} 进度已保存！");
    }

    /// <summary>
    /// 加载单关卡进度
    /// </summary>
    /// <param name="levelIndex">关卡索引</param>
    public void LoadLevelProgress(int levelIndex)
    {
        // 1. 读取关卡状态
        if (PlayerPrefs.HasKey(KEY_LEVEL_STATE + levelIndex))
        {
            CurrentLevelInfoPack.CurrentLevelState = (LevelState)PlayerPrefs.GetInt(KEY_LEVEL_STATE + levelIndex);
        }
        else
        {
            CurrentLevelInfoPack.CurrentLevelState = LevelState.Playing; // 默认游玩中
        }

        // 2. 读取复活点
        if (PlayerPrefs.HasKey(KEY_LEVEL_CHECKPOINT + levelIndex))
        {
            string posStr = PlayerPrefs.GetString(KEY_LEVEL_CHECKPOINT + levelIndex);
            string[] posArr = posStr.Split('|');
            if (posArr.Length == 3)
            {
                Vector3 bornPos = new Vector3(
                    float.Parse(posArr[0]),
                    float.Parse(posArr[1]),
                    float.Parse(posArr[2])
                );
                CurrentLevelInfoPack.PlayerBornPos.position = bornPos;
            }
        }
        Debug.Log($"关卡 {levelIndex} 进度已加载！");
    }

    /// <summary>
    /// 保存全局进度
    /// </summary>
    public void SaveGlobalProgress()
    {
        string passedLevels = PlayerPrefs.GetString(KEY_GLOBAL_LEVEL_PROGRESS, "");
        if (!passedLevels.Contains(CurrentLevelInfoPack.LevelIndex.ToString()))
        {
            passedLevels += $"{CurrentLevelInfoPack.LevelIndex},";
            PlayerPrefs.SetString(KEY_GLOBAL_LEVEL_PROGRESS, passedLevels);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// 检查关卡是否已解锁
    /// </summary>
    public bool IsLevelUnlocked(int levelIndex)
    {
        if (levelIndex == 1) return true; // 第一关默认解锁
        string passedLevels = PlayerPrefs.GetString(KEY_GLOBAL_LEVEL_PROGRESS, "");
        return passedLevels.Contains((levelIndex - 1).ToString()); // 通关前一关则解锁
    }
    #endregion

    #region 辅助：关卡状态校验
    /// <summary>
    /// 检查当前关卡是否可操作（避免非法操作）
    /// </summary>
    public bool IsLevelOperable()
    {
        return CurrentLevelInfoPack != null
               && CurrentLevelInfoPack.CurrentLevelState != LevelState.Settlement
               && CurrentLevelInfoPack.CurrentLevelState != LevelState.Win
               && CurrentLevelInfoPack.CurrentLevelState != LevelState.Lose;
    }
    #endregion
}
