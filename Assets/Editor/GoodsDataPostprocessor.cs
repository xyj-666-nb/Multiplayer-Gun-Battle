using UnityEngine;
using UnityEditor;

public class GoodsDataPostprocessor : AssetPostprocessor
{
    // 监听所有资源变化
    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        bool needsUpdate = false;

        // 检查是否有 GoodsData 被导入/创建/修改
        foreach (string path in importedAssets)
        {
            if (path.EndsWith(".asset"))
            {
                GoodsData goods = AssetDatabase.LoadAssetAtPath<GoodsData>(path);
                if (goods != null)
                {
                    // 如果没有 GUID，自动生成
                    if (string.IsNullOrEmpty(goods.goodsGuid))
                    {
                        goods.goodsGuid = System.Guid.NewGuid().ToString();
                        EditorUtility.SetDirty(goods);
                        AssetDatabase.SaveAssets();
                        Debug.Log($"[GUID监听] 自动为新商品生成GUID：{goods.goodsName}");
                    }
                    needsUpdate = true;
                }
            }
        }

        // 检查是否有 GoodsData 被删除
        foreach (string path in deletedAssets)
        {
            if (path.EndsWith(".asset"))
            {
                // 只要删除了 .asset，就可能是商品，更新文件
                needsUpdate = true;
                Debug.Log($"[GUID监听] 检测到资源删除，更新GUID映射文件");
            }
        }

        // 如果需要更新，重建加密文件
        if (needsUpdate)
        {
            GoodsDataEditorTool.RebuildEncryptedGuidFile();
        }
    }
}