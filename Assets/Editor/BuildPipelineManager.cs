using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

public class BuildPipelineManager
{
    private const string ANDROID_SUB_PATH = "Android";
    private const string WINDOWS_SUB_PATH = "Windows";

    public static void BuildAllPlatforms(string baseDirPath, bool runAndroid, bool runWindows, bool cleanBuild)
    {
        if (string.IsNullOrEmpty(baseDirPath))
        {
            Debug.Log("빌드가 취소");
            return;
        }

        string version = PlayerSettings.bundleVersion;
        Debug.Log($"빌드를 시작: {version}");

        string versionFolderName = $"PTver_{version}";
        string versionDirPath = Path.Combine(baseDirPath, versionFolderName);

        string[] scenes = GetBuildScenes();

        string androidDirPath = Path.Combine(versionDirPath, ANDROID_SUB_PATH);
        string windowsDirPath = Path.Combine(versionDirPath, WINDOWS_SUB_PATH);

        if (Directory.Exists(androidDirPath) == false)
        {
            Directory.CreateDirectory(androidDirPath);
        }
        
        if (Directory.Exists(windowsDirPath) == false)
        {
            Directory.CreateDirectory(windowsDirPath);
        }

        BuildOptions baseOptions = BuildOptions.None;
        if (cleanBuild)
        {
            baseOptions |= BuildOptions.CleanBuildCache;
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        string androidFileName = $"PTver_{version}.apk";
        string androidOutputPath = Path.Combine(androidDirPath, androidFileName);
        BuildOptions androidOptions = baseOptions;
        if (runAndroid)
        {
            androidOptions |= BuildOptions.AutoRunPlayer;
        }
        BuildPlayer(scenes, androidOutputPath, BuildTarget.Android, BuildTargetGroup.Android, androidOptions);

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        }

        string windowsFileName = $"PTver_{version}.exe";
        string windowsOutputPath = Path.Combine(windowsDirPath, windowsFileName);
        BuildOptions windowsOptions = baseOptions;
        if (runWindows)
        {
            windowsOptions |= BuildOptions.AutoRunPlayer;
        }
        BuildPlayer(scenes, windowsOutputPath, BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, windowsOptions);

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        Debug.Log("빌드 시마이");
    }

    private static string[] GetBuildScenes()
    {
        List<string> scenes = new List<string>();
        EditorBuildSettingsScene[] allScenes = EditorBuildSettings.scenes;
        for (int i = 0; i < allScenes.Length; i++)
        {
            if (allScenes[i].enabled)
            {
                scenes.Add(allScenes[i].path);
            }
        }
        return scenes.ToArray();
    }

    private static void BuildPlayer(string[] scenes, string outputPath, BuildTarget target, BuildTargetGroup group, BuildOptions buildOptions)
    {
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = target,
            targetGroup = group,
            options = buildOptions
        };

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"빌드 {target} 성공스: " + summary.totalSize + " 바이트");
        }
        else if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
        {
            Debug.LogError($"빌드 실패 {target} 실패스.");
        }
    }
}

public class BuildSettingsWindow : EditorWindow
{
    private string m_buildPath;
    private string m_version;
    private int m_versionCode;
    private bool m_runAndroid;
    private bool m_runWindows;
    private bool m_cleanBuild;

    [MenuItem("Tools/Build Settings")]
    public static void ShowWindow()
    {
        GetWindow<BuildSettingsWindow>("Build Settings");
    }

    private void OnEnable()
    {
        m_buildPath = EditorPrefs.GetString("SpaceCaptain_BuildPath", "");
        m_version = PlayerSettings.bundleVersion;
        m_versionCode = PlayerSettings.Android.bundleVersionCode;
        m_runAndroid = EditorPrefs.GetBool("SpaceCaptain_RunAndroid", true);
        m_runWindows = EditorPrefs.GetBool("SpaceCaptain_RunWindows", false);
        m_cleanBuild = EditorPrefs.GetBool("SpaceCaptain_CleanBuild", false);
    }

    private void OnGUI()
    {
        GUILayout.Label("빌드 설정", EditorStyles.boldLabel);
        
        m_version = EditorGUILayout.TextField("버전", m_version);
        m_versionCode = EditorGUILayout.IntField("번들 코드", m_versionCode);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        m_buildPath = EditorGUILayout.TextField("저장 경로", m_buildPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string path = EditorUtility.OpenFolderPanel("경로 선택", m_buildPath, "");
            if (string.IsNullOrEmpty(path) == false)
            {
                m_buildPath = path;
                EditorPrefs.SetString("SpaceCaptain_BuildPath", m_buildPath);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        
        GUILayout.Label("빌드 & 런 옵션", EditorStyles.boldLabel);
        m_runAndroid = EditorGUILayout.Toggle("안드로이드 실행", m_runAndroid);
        m_runWindows = EditorGUILayout.Toggle("윈도우 실행", m_runWindows);
        m_cleanBuild = EditorGUILayout.Toggle("클린 빌드", m_cleanBuild);

        EditorGUILayout.Space();

        if (GUILayout.Button("빌드 시작", GUILayout.Height(30)))
        {
            if (string.IsNullOrEmpty(m_buildPath))
            {
                Debug.LogError("빌드 경로를 선택.");
                return;
            }

            ApplySettings();
            BuildPipelineManager.BuildAllPlatforms(m_buildPath, m_runAndroid, m_runWindows, m_cleanBuild);
        }
    }

    private void ApplySettings()
    {
        PlayerSettings.bundleVersion = m_version;
        PlayerSettings.Android.bundleVersionCode = m_versionCode;
        EditorPrefs.SetString("SpaceCaptain_BuildPath", m_buildPath);
        EditorPrefs.SetBool("SpaceCaptain_RunAndroid", m_runAndroid);
        EditorPrefs.SetBool("SpaceCaptain_RunWindows", m_runWindows);
        EditorPrefs.SetBool("SpaceCaptain_CleanBuild", m_cleanBuild);
    }
}
