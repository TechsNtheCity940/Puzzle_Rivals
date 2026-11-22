using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#if UNITY_6000_OR_NEWER
using UnityEngine.Build;
#endif

public static class PuzzleRivalsWebGLDeployBuild
{
    public static void Build()
    {
        // Use relative path for CI/CD compatibility (Linux runners cannot access Windows paths)
        string output = Path.Combine(Application.dataPath, "..", "WebGL-Deploy-Staging");
        Directory.CreateDirectory(output);
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled && File.Exists(s.path))
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new Exception("No enabled scenes found in EditorBuildSettings.");

        // Set compression to Uncompressed for GitHub Pages compatibility
        PlayerSettings.WebGL.compressionFormat = CompressionFormat.Uncompressed;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.Standard;
        
        Debug.Log("PR_WEBGL_BUILD_START scenes=" + scenes.Length + " output=" + output);
        Debug.Log("PR_WEBGL_BUILD_UNITY=" + Application.unityVersion);
        Debug.Log("PR_WEBGL_BUILD_COMPRESSION=" + PlayerSettings.WebGL.compressionFormat);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(output, "index.html"),
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("PR_WEBGL_BUILD_RESULT=" + report.summary.result);
        Debug.Log("PR_WEBGL_BUILD_ERRORS=" + report.summary.totalErrors);
        Debug.Log("PR_WEBGL_BUILD_WARNINGS=" + report.summary.totalWarnings);
        Debug.Log("PR_WEBGL_BUILD_SIZE=" + report.summary.totalSize);
        if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors > 0)
            throw new Exception("WebGL build failed: " + report.summary.result + " errors=" + report.summary.totalErrors);
    }
}