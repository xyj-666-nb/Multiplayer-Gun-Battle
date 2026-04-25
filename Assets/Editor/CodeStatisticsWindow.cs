using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 代码行数统计工具（支持全项目/自定义目录 + 有效代码检测）
/// </summary>
public class CodeStatisticsWindow : EditorWindow
{
    // 统计模式
    private enum StatMode
    {
        AllProject,     // 全项目统计
        CustomFolders   // 自定义文件夹(Script+Editor)
    }

    // 配置
    private StatMode _statMode = StatMode.CustomFolders;
    private bool _countValidCode = true; // 开启有效代码检测
    private readonly List<string> _customFolders = new List<string> { "Assets/Script", "Assets/Editor" };

    // 统计结果
    private int _totalFiles;
    private int _totalLines;
    private int _validLines;
    private int _emptyLines;
    private int _commentLines;
    private Vector2 _scrollPos;

    // 正则匹配（去除注释/空行）
    private readonly Regex _singleLineComment = new Regex(@"^\s*//.*$", RegexOptions.Compiled);
    private readonly Regex _emptyLine = new Regex(@"^\s*$", RegexOptions.Compiled);

    [MenuItem("Tools/代码行数统计工具")]
    public static void ShowWindow()
    {
        var window = GetWindow<CodeStatisticsWindow>("代码行数统计");
        window.minSize = new Vector2(500, 400);
        window.Show();
    }

    private void OnGUI()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        GUILayout.Space(10);
        GUILayout.Label(" 代码统计设置", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // 统计模式选择
        _statMode = (StatMode)EditorGUILayout.EnumPopup("统计模式", _statMode);

        // 自定义文件夹提示
        if (_statMode == StatMode.CustomFolders)
        {
            EditorGUILayout.HelpBox("仅统计：Assets/Script + Assets/Editor", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("统计Assets目录下所有C#代码文件", MessageType.Info);
        }

        // 有效代码选项
        _countValidCode = EditorGUILayout.Toggle(" 有效代码检测", _countValidCode);
        if (_countValidCode)
        {
            EditorGUILayout.HelpBox("开启后：过滤 空行 / 单行注释 / 多行注释", MessageType.None);
        }

        GUILayout.Space(15);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        // 统计按钮
        if (GUILayout.Button(" 开始统计代码", GUILayout.Width(200), GUILayout.Height(30)))
        {
            StartStatistics();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(15);

        // 分割线
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(5);

        // 统计结果展示
        GUILayout.Label(" 统计结果", EditorStyles.boldLabel);
        GUILayout.Space(5);

        EditorGUILayout.LabelField($" 代码文件总数：{_totalFiles} 个");
        EditorGUILayout.LabelField($" 代码总行数：{_totalLines} 行");

        if (_countValidCode)
        {
            EditorGUILayout.LabelField($"有效代码行：{_validLines} 行");
            EditorGUILayout.LabelField($" 注释行数：{_commentLines} 行");
            EditorGUILayout.LabelField($" 空行数：{_emptyLines} 行");
        }

        // 复制结果按钮
        if (_totalFiles > 0 && GUILayout.Button(" 复制统计结果", GUILayout.Width(150)))
        {
            CopyResultToClipboard();
            EditorUtility.DisplayDialog("成功", "统计结果已复制到剪贴板！", "确定");
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 开始执行统计
    /// </summary>
    private void StartStatistics()
    {
        // 重置数据
        _totalFiles = 0;
        _totalLines = 0;
        _validLines = 0;
        _emptyLines = 0;
        _commentLines = 0;

        // 获取所有cs文件
        List<string> csFiles = GetAllCsFiles();

        // 遍历统计每个文件
        foreach (string file in csFiles)
        {
            StatSingleFile(file);
        }

        // 打印到控制台
        PrintResultToConsole();
    }

    /// <summary>
    /// 获取需要统计的CS文件列表
    /// </summary>
    private List<string> GetAllCsFiles()
    {
        List<string> files = new List<string>();

        if (_statMode == StatMode.AllProject)
        {
            // 全项目：遍历所有Assets下的cs文件
            string[] allFiles = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
            files.AddRange(allFiles);
        }
        else
        {
            // 自定义目录：Script + Editor
            foreach (string folder in _customFolders)
            {
                if (Directory.Exists(folder))
                {
                    string[] folderFiles = Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories);
                    files.AddRange(folderFiles);
                }
            }
        }

        return files;
    }

    /// <summary>
    /// 统计单个文件的代码行数
    /// </summary>
    private void StatSingleFile(string path)
    {
        try
        {
            _totalFiles++;
            string[] lines = File.ReadAllLines(path);
            _totalLines += lines.Length;

            if (!_countValidCode) return;

            bool inMultiComment = false;

            foreach (string line in lines)
            {
                string trimLine = line.Trim();

                // 1. 空行判断
                if (_emptyLine.IsMatch(trimLine))
                {
                    _emptyLines++;
                    continue;
                }

                // 2. 多行注释处理 /* ... */
                if (trimLine.Contains("/*"))
                {
                    inMultiComment = true;
                }
                if (trimLine.Contains("*/"))
                {
                    inMultiComment = false;
                    _commentLines++;
                    continue;
                }
                if (inMultiComment)
                {
                    _commentLines++;
                    continue;
                }

                // 3. 单行注释 //
                if (_singleLineComment.IsMatch(line))
                {
                    _commentLines++;
                    continue;
                }

                // 4. 有效代码
                _validLines++;
            }
        }
        catch (Exception e)
        {
            /* Debug.LogError($"统计文件失败：{path}\n错误：{e.Message}"); */
        }
    }

    /// <summary>
    /// 复制结果到剪贴板
    /// </summary>
    private void CopyResultToClipboard()
    {
        string result = $"代码统计结果\n" +
                        $"统计模式：{_statMode}\n" +
                        $"有效代码检测：{(_countValidCode ? "开启" : "关闭")}\n" +
                        $"文件总数：{_totalFiles} 个\n" +
                        $"总行数：{_totalLines} 行\n";

        if (_countValidCode)
        {
            result += $"有效行数：{_validLines} 行\n" +
                      $"注释行数：{_commentLines} 行\n" +
                      $"空行数：{_emptyLines} 行";
        }

        EditorGUIUtility.systemCopyBuffer = result;
    }

    /// <summary>
    /// 打印结果到控制台
    /// </summary>
    private void PrintResultToConsole()
    {
        /* Debug.LogWarning("===== 代码统计完成 ====="); */
        /* Debug.Log($"模式：{_statMode} | 有效检测：{(_countValidCode ? "开启" : "关闭")}"); */
        /* Debug.Log($"文件：{_totalFiles} 个 | 总行：{_totalLines} 行"); */

        if (_countValidCode)
        {
            /* Debug.Log($"有效：{_validLines} 行 | 注释：{_commentLines} 行 | 空行：{_emptyLines} 行"); */
        }
    }
}
