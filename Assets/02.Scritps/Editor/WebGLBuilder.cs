using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DivisorGame.EditorTools
{
    /// <summary>
    /// WebGL 빌드 (T13).
    /// 메뉴에서 실행하거나, CLI에서 -executeMethod로 호출한다.
    ///
    /// 압축은 꺼 둔다. gzip/Brotli로 압축하면 웹서버가 Content-Encoding 헤더를 정확히
    /// 내려줘야 해서 로컬에서 간단히 열어 볼 때 실패하기 쉽다. 용량은 커지지만
    /// 아무 정적 서버에서나 바로 동작하는 쪽을 택했다.
    /// </summary>
    public static class WebGLBuilder
    {
        private const string OutputDirectory = "Builds/WebGL";

        [MenuItem("약수 카드게임/WebGL 빌드", false, 20)]
        public static void BuildFromMenu()
        {
            BuildReport report = Build();
            if (report == null) return;

            if (report.summary.result == BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(Path.GetFullPath(OutputDirectory));
            }
        }

        /// <summary>CLI용 진입점. 실패하면 0이 아닌 코드로 종료한다.</summary>
        public static void BuildFromCommandLine()
        {
            BuildReport report = Build();
            bool ok = report != null && report.summary.result == BuildResult.Succeeded;
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static BuildReport Build()
        {
            string[] scenes = GetEnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("빌드에 포함된 씬이 없습니다. Build Settings를 확인하세요.");
                return null;
            }

            Directory.CreateDirectory(OutputDirectory);

            // 로컬에서 바로 열어 볼 수 있도록 압축을 끈다.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputDirectory,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log(string.Format(
                    "WebGL 빌드 성공: {0}  ({1:N1} MB, {2:N0}초)",
                    Path.GetFullPath(OutputDirectory),
                    summary.totalSize / (1024f * 1024f),
                    summary.totalTime.TotalSeconds));
            }
            else
            {
                Debug.LogError("WebGL 빌드 실패: " + summary.result + " (오류 " + summary.totalErrors + "건)");
            }

            return report;
        }

        private static string[] GetEnabledScenes()
        {
            var scenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled) scenes.Add(scene.path);
            }
            return scenes.ToArray();
        }
    }
}
