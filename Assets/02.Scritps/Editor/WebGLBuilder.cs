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
    /// 용도가 두 가지라 진입점을 나눠 두었다.
    ///  - 로컬 확인용: 압축 없음. 아무 정적 서버에서나 바로 열린다.
    ///  - 배포용: Brotli 압축. 전송량이 1/4로 줄지만, 웹서버가 Content-Encoding 헤더를
    ///    정확히 내려줘야 한다. divisorGame/vercel.json이 그 헤더를 지정한다.
    /// </summary>
    public static class WebGLBuilder
    {
        private const string LocalOutputDirectory = "Builds/WebGL";
        private const string DeployOutputDirectory = "divisorGame";

        /// <summary>브라우저 창 크기에 맞춰 캔버스를 채우는 커스텀 템플릿.</summary>
        private const string ResponsiveTemplate = "PROJECT:Responsive";

        [MenuItem("약수 카드게임/WebGL 빌드 (로컬 확인용)", false, 20)]
        public static void BuildFromMenu()
        {
            BuildReport report = Build(LocalOutputDirectory, WebGLCompressionFormat.Disabled);
            if (report == null) return;

            if (report.summary.result == BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(Path.GetFullPath(LocalOutputDirectory));
            }
        }

        [MenuItem("약수 카드게임/WebGL 빌드 (배포용)", false, 21)]
        public static void BuildDeployFromMenu()
        {
            BuildReport report = Build(DeployOutputDirectory, WebGLCompressionFormat.Brotli);
            if (report == null) return;

            if (report.summary.result == BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(Path.GetFullPath(DeployOutputDirectory));
            }
        }

        /// <summary>CLI용 진입점(로컬 확인용). 실패하면 0이 아닌 코드로 종료한다.</summary>
        public static void BuildFromCommandLine()
        {
            Exit(Build(LocalOutputDirectory, WebGLCompressionFormat.Disabled));
        }

        /// <summary>CLI용 진입점(배포용). 실패하면 0이 아닌 코드로 종료한다.</summary>
        public static void BuildDeployFromCommandLine()
        {
            Exit(Build(DeployOutputDirectory, WebGLCompressionFormat.Brotli));
        }

        private static void Exit(BuildReport report)
        {
            bool ok = report != null && report.summary.result == BuildResult.Succeeded;
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static BuildReport Build(string outputDirectory, WebGLCompressionFormat compression)
        {
            string[] scenes = GetEnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("빌드에 포함된 씬이 없습니다. Build Settings를 확인하세요.");
                return null;
            }

            Directory.CreateDirectory(outputDirectory);

            PlayerSettings.WebGL.compressionFormat = compression;
            PlayerSettings.WebGL.template = ResponsiveTemplate;

            // 압축을 쓰는 배포 빌드에서는 서버가 Content-Encoding을 내려주므로
            // 로더에 압축 해제 코드를 넣지 않는다(로더 용량 절감).
            PlayerSettings.WebGL.decompressionFallback = false;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputDirectory,
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
                    Path.GetFullPath(outputDirectory),
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
