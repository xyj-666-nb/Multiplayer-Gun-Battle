using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using LitJson;
using System.IO;

public class DataEncryptionManger : SingleMonoAutoBehavior<DataEncryptionManger>
{
    #region 信息配置以及变量声明
    [Header("配置")]
    [Tooltip("存档加密的密钥")]
    public string saveSecretKey = "MyProject_Multiplayer-Gun-Battle_2026_5people_2months_Sophomore";
    [Tooltip("内存加密的校准魔数")]
    private const int MEMORY_MAGIC = 0x1A2B3C4D;

    // 存储所有动态加密数据包
    private List<DataEncryptionPack> _allDataPackList = new List<DataEncryptionPack>();
    #endregion

    #region 动态内存加密（游戏运行时）

    /// <summary>
    /// 加密数据并返回数据包ID
    /// </summary>
    /// <typeparam name="T">支持 int/float/bool/string</typeparam>
    /// <param name="value">原始值</param>
    /// <returns>数据包唯一ID</returns>
    public int EncryptData<T>(T value)
    {
        var pack = PoolManage.Instance.GetObj<DataEncryptionPack>();//获取数据包

        byte[] rawBytes = ConvertValueToBytes(value);

        byte[] magicBytes = BitConverter.GetBytes(MEMORY_MAGIC);
        byte[] combinedBytes = CombineBytes(magicBytes, rawBytes);
        pack.EncryptedData = XorEncrypt(combinedBytes, pack.RandomKey);
        pack.CheckSum = CalculateChecksum(combinedBytes);

        _allDataPackList.Add(pack);
        return pack.DataID;
    }

    /// <summary>
    /// 更新已存在的数据包，如果ID不存在则创建新数据包
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dataId"></param>
    /// <param name="newValue"></param>
    /// <returns></returns>
    public int UpdateEncryptedData<T>(int dataId, T newValue)
    {
        for (int i = 0; i < _allDataPackList.Count; i++)
        {
            if (_allDataPackList[i].DataID == dataId)
            {
                byte[] rawBytes = ConvertValueToBytes(newValue);
                byte[] magicBytes = BitConverter.GetBytes(MEMORY_MAGIC);
                byte[] combinedBytes = CombineBytes(magicBytes, rawBytes);
                _allDataPackList[i].EncryptedData = XorEncrypt(combinedBytes, _allDataPackList[i].RandomKey);
                _allDataPackList[i].CheckSum = CalculateChecksum(combinedBytes);
                return dataId;
            }
        }
        Debug.LogWarning($"[数据加密] 尝试更新不存在的DataID {dataId}！自动创建新数据包。");
        return EncryptData(newValue); // 如果ID不存在，创建新数据包
    }

    //移除加密数据包
    public void RemoveDataPack(int dataId)
    {
        for (int i = 0; i < _allDataPackList.Count; i++)
        {
            if (_allDataPackList[i].DataID == dataId)
            {
                PoolManage.Instance.PushObj(_allDataPackList[i]);
                _allDataPackList.RemoveAt(i);
                return;
            }
        }
        Debug.LogWarning($"[数据加密] 尝试移除不存在的DataID {dataId}！");
    }

    /// <summary>
    /// 根据ID获取解密后的数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="dataId">数据包ID</param>
    /// <returns>解密后的原始值</returns>
    public T GetDecryptedData<T>(int dataId)
    {
        foreach (var pack in _allDataPackList)
        {
            if (pack.DataID == dataId)
            {
                // XOR解密
                byte[] decryptedCombined = XorEncrypt(pack.EncryptedData, pack.RandomKey);
                // 验证魔数
                int magic = BitConverter.ToInt32(decryptedCombined, 0);
                if (magic != MEMORY_MAGIC)
                {
                    Debug.LogError($"[数据加密] 数据ID {dataId} 魔数校验失败！数据可能被篡改！");
                    return default(T);
                }
                // 验证校验和
                int checkSum = CalculateChecksum(decryptedCombined);
                if (checkSum != pack.CheckSum)
                {
                    Debug.LogError($"[数据加密] 数据ID {dataId} 校验和失败！数据可能被篡改！");
                    return default(T);
                }
                // 提取原始数据（跳过魔数的4个字节）
                byte[] rawData = new byte[decryptedCombined.Length - 4];
                Array.Copy(decryptedCombined, 4, rawData, 0, rawData.Length);
                // 转换回目标类型
                return ConvertBytesToValue<T>(rawData);
            }
        }

        Debug.LogError($"[数据加密] 未找到ID为 {dataId} 的数据包！");
        return default(T);
    }

    #endregion

    #region 静态数据加密（游戏存档）

    #region 简单数据
    /// <summary>
    /// 保存加密数据到PlayerPrefs
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">存储键</param>
    /// <param name="value">要保存的值</param>
    public void SaveEncryptedPlayerPrefs<T>(string key, T value)
    {
        // 序列化数据为JSON
        string jsonData = JsonUtility.ToJson(new EncryptedSaveData<T> { data = value });
        // 生成MD5哈希
        string md5Hash = CalculateMD5Hash(jsonData + saveSecretKey);
        // 组合数据和哈希
        string finalData = $"{md5Hash}|{jsonData}";
        // 保存到PlayerPrefs
        PlayerPrefs.SetString(key, finalData);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 从PlayerPrefs读取并解密数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">存储键</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>解密后的值</returns>
    public T LoadEncryptedPlayerPrefs<T>(string key, T defaultValue = default)
    {
        if (!PlayerPrefs.HasKey(key))
        {
            return defaultValue;
        }

        string savedData = PlayerPrefs.GetString(key);
        string[] splitData = savedData.Split('|');

        if (splitData.Length != 2)
        {
            Debug.LogError($"[存档加密] 存档格式错误！Key: {key}");
            return defaultValue;
        }

        string savedHash = splitData[0];
        string jsonData = splitData[1];

        // 验证MD5哈希
        string calculatedHash = CalculateMD5Hash(jsonData + saveSecretKey);
        if (savedHash != calculatedHash)
        {
            Debug.LogError($"[存档加密] 存档哈希校验失败！存档可能被篡改！Key: {key}");
            return defaultValue;
        }


        // 反序列化数据
        try
        {
            var saveData = JsonUtility.FromJson<EncryptedSaveData<T>>(jsonData);
            return saveData.data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[存档加密] 反序列化失败！Key: {key}, Error: {e.Message}");
            return defaultValue;
        }
    }
    #endregion

    #region 加密自定义数据

    /// <summary>
    /// 加密保存【自定义类/复杂数据】到本地文件
    /// 配合 JsonManager 使用
    /// </summary>
    /// <typeparam name="T">自定义数据类型</typeparam>
    /// <param name="fileName">文件名（不需要带.json）</param>
    /// <param name="data">要保存的复杂数据</param>
    /// <param name="type">Json工具类型</param>
    public void SaveEncryptedComplexData<T>(string fileName, T data, JsonType type = JsonType.LitJson)
    {
        try
        {
            // 先把复杂数据序列化为 JSON 
            string jsonStr = "";
            switch (type)
            {
                case JsonType.JsonUtlity:
                    jsonStr = JsonUtility.ToJson(data);
                    break;
                case JsonType.LitJson:
                    jsonStr = JsonMapper.ToJson(data);
                    break;
            }

            // 生成 MD5 哈希
            string md5Hash = CalculateMD5Hash(jsonStr + saveSecretKey);

            // 组合数据 (哈希|JSON)
            string finalData = $"{md5Hash}|{jsonStr}";

            // 确定存储路径
            string path = Application.persistentDataPath + "/" + fileName + ".json";

            // 写入文件
            File.WriteAllText(path, finalData);

        }
        catch (Exception e)
        {
            Debug.LogError($"[存档加密] 保存复杂数据失败! Error: {e.Message}");
        }
    }

    /// <summary>
    /// 读取并解密【自定义类/复杂数据】
    /// 配合 JsonManager 使用
    /// </summary>
    /// <typeparam name="T">自定义数据类型</typeparam>
    /// <param name="fileName">文件名（不需要带.json）</param>
    /// <param name="type">Json工具类型</param>
    /// <returns>解密后的对象</returns>
    public T LoadEncryptedComplexData<T>(string fileName, JsonType type = JsonType.LitJson) where T : new()
    {
        // 确定读取路径 
        string path = Application.streamingAssetsPath + "/" + fileName + ".json";
        if (!File.Exists(path))
            path = Application.persistentDataPath + "/" + fileName + ".json";

        if (!File.Exists(path))
        {
            return new T();
        }

        try
        {
            // 读取文件内容
            string savedData = File.ReadAllText(path);
            string[] splitData = savedData.Split('|');

            if (splitData.Length != 2)
            {
                Debug.LogError($"[存档加密] 存档格式错误（非加密格式）! File: {fileName}");
                return new T();
            }

            string savedHash = splitData[0];
            string jsonData = splitData[1];

            // 验证 MD5 哈希
            string calculatedHash = CalculateMD5Hash(jsonData + saveSecretKey);
            if (savedHash != calculatedHash)
            {
                Debug.LogError($"[存档加密] 存档哈希校验失败！数据可能被篡改！File: {fileName}");
                return new T();
            }

            // 反序列化数据 
            T data = default(T);
            switch (type)
            {
                case JsonType.JsonUtlity:
                    data = JsonUtility.FromJson<T>(jsonData);
                    break;
                case JsonType.LitJson:
                    data = JsonMapper.ToObject<T>(jsonData);
                    break;
            }

            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[存档加密] 读取复杂数据失败! File: {fileName}, Error: {e.Message}");
            return new T();
        }
    }

    /// <summary>
    /// 删除本地加密存档文件
    /// </summary>
    /// <param name="fileName">文件名（不需要带.json）</param>
    public void DeleteEncryptedComplexData(string fileName)
    {
        try
        {
            //  定义所有可能的路径（Persistent 和 StreamingAssets）
            string persistentPath = Application.persistentDataPath + "/" + fileName + ".json";
            string streamingPath = Application.streamingAssetsPath + "/" + fileName + ".json";

            bool hasDeleted = false;

            // 删除 PersistentDataPath 下的文件
            if (File.Exists(persistentPath))
            {
                File.Delete(persistentPath);
                hasDeleted = true;
                Debug.Log($"[存档加密] 已删除本地加密存档: {persistentPath}");
            }

            // 注意：StreamingAssets 目录在移动端是只读的，无法通过代码删除文件
            // 这里只做 PC 端编辑器的兼容处理，移动端直接忽略
#if UNITY_EDITOR || UNITY_STANDALONE
            if (File.Exists(streamingPath))
            {
                File.Delete(streamingPath);
                hasDeleted = true;
                Debug.Log($"[存档加密] 已删除 StreamingAssets 下的文件: {streamingPath}");
            }
#endif

            if (!hasDeleted)
            {
                Debug.LogWarning($"[存档加密] 未找到需要删除的存档文件: {fileName}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[存档加密] 删除存档文件失败! File: {fileName}, Error: {e.Message}");
        }
    }

    #endregion

    #endregion

    #region 内部工具方法

    // 字节数组合并
    private byte[] CombineBytes(byte[] a, byte[] b)
    {
        byte[] result = new byte[a.Length + b.Length];
        Buffer.BlockCopy(a, 0, result, 0, a.Length);
        Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
        return result;
    }

    // XOR加密/解密（对称加密）
    private byte[] XorEncrypt(byte[] data, int key)
    {
        byte[] keyBytes = BitConverter.GetBytes(key);
        byte[] result = new byte[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
        }

        return result;
    }

    // 计算简单校验和
    private int CalculateChecksum(byte[] data)
    {
        int sum = 0;
        foreach (byte b in data)
        {
            sum += b;
        }
        return sum;
    }

    // 计算MD5哈希
    private string CalculateMD5Hash(string input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }

    // 值类型转字节数组
    private byte[] ConvertValueToBytes<T>(T value)
    {
        if (typeof(T) == typeof(int))
            return BitConverter.GetBytes(Convert.ToInt32(value));
        if (typeof(T) == typeof(float))
            return BitConverter.GetBytes(Convert.ToSingle(value));
        if (typeof(T) == typeof(bool))
            return BitConverter.GetBytes(Convert.ToBoolean(value));
        if (typeof(T) == typeof(string))
            return Encoding.UTF8.GetBytes(Convert.ToString(value));

        // 其他复杂类型尝试用JSON序列化
        string json = JsonUtility.ToJson(value);
        return Encoding.UTF8.GetBytes(json);
    }

    // 字节数组转回值类型
    private T ConvertBytesToValue<T>(byte[] bytes)
    {
        if (typeof(T) == typeof(int))
            return (T)(object)BitConverter.ToInt32(bytes, 0);
        if (typeof(T) == typeof(float))
            return (T)(object)BitConverter.ToSingle(bytes, 0);
        if (typeof(T) == typeof(bool))
            return (T)(object)BitConverter.ToBoolean(bytes, 0);
        if (typeof(T) == typeof(string))
            return (T)(object)Encoding.UTF8.GetString(bytes);

        // 其他复杂类型尝试JSON反序列化
        string json = Encoding.UTF8.GetString(bytes);
        return JsonUtility.FromJson<T>(json);
    }

    #endregion

    #region 辅助类

    // 用于存档序列化的包装类
    [Serializable]
    private class EncryptedSaveData<T>
    {
        public T data;
    }

    #endregion

    #region 编辑器静态加密工具
    public static class EditorEncryptionTools
    {
        // 直接复制你原有的MD5逻辑，做成静态
        public static string CalculateMD5Hash(string input, string secretKey)
        {
            using (MD5 md5 = MD5.Create())
            {
                string combined = input + secretKey;
                byte[] inputBytes = Encoding.UTF8.GetBytes(combined);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        // 生成加密的存档字符串
        public static string GenerateEncryptedSaveString<T>(T data, string secretKey)
        {
            string jsonData = JsonUtility.ToJson(new EncryptedSaveData<T> { data = data });
            string md5Hash = CalculateMD5Hash(jsonData, secretKey);
            return $"{md5Hash}|{jsonData}";
        }

        // 解密存档字符串
        public static T DecryptSaveString<T>(string encryptedString, string secretKey)
        {
            string[] splitData = encryptedString.Split('|');
            if (splitData.Length != 2) return default(T);

            string savedHash = splitData[0];
            string jsonData = splitData[1];

            string calculatedHash = CalculateMD5Hash(jsonData, secretKey);
            if (savedHash != calculatedHash) return default(T);

            try
            {
                var saveData = JsonUtility.FromJson<EncryptedSaveData<T>>(jsonData);
                return saveData.data;
            }
            catch
            {
                return default(T);
            }
        }
    }
    #endregion
}

#region 加密数据包定义

// 数据加密包 适配对象池
public class DataEncryptionPack : IPoolObject
{
    public byte[] EncryptedData;  // 加密后的字节数组
    public int RandomKey;         // 随机密钥（每次加密都不同）
    public int DataID;            // 数据包唯一ID
    public int CheckSum;          // 校验和（防篡改）

    // 静态自增ID
    private static int _idCounter = 0;

    // 构造函数：只有【非对象池创建】才会走
    public DataEncryptionPack()
    {
        GenerateNewID();
        GenerateRandomKey();
    }

    /// <summary>
    /// 重置所有数据，保证干净无残留
    /// </summary>
    public void ReSetDate()
    {
        // 清空加密数据
        EncryptedData = null;

        // 重新生成密钥
        GenerateRandomKey();

        // 重新生成唯一ID
        GenerateNewID();

        // 清空校验和
        CheckSum = 0;
    }

    // 生成新唯一ID
    private void GenerateNewID()
    {
        DataID = ++_idCounter;
    }

    // 生成随机密钥
    private void GenerateRandomKey()
    {
        RandomKey = UnityEngine.Random.Range(1, 10000);
    }

    /// <summary>
    /// 手动销毁
    /// </summary>
    public void Dispose()
    {
        ReSetDate();
    }
}
#endregion
