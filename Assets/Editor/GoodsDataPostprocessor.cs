using UnityEditor;

public class GoodsDataPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        bool needsUpdate = false;

        foreach (string path in importedAssets)
        {
            if (!path.EndsWith(".asset"))
                continue;

            GoodsData goods = AssetDatabase.LoadAssetAtPath<GoodsData>(path);
            if (goods != null)
            {
                needsUpdate = true;
                break;
            }
        }

        foreach (string path in deletedAssets)
        {
            if (path.EndsWith(".asset"))
            {
                needsUpdate = true;
                break;
            }
        }

        if (!needsUpdate)
            return;

        GoodsDataEditorTool.EnsureAllGoodsHaveUniqueGuids();
        GoodsDataEditorTool.RebuildEncryptedGuidFile();
    }
}
