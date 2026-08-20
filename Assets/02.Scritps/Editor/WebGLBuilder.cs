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
    /// 용도가 세 가지라 진입점을 나눠 두었다.
    ///  - 로컬 확인용: 압축 없음. 아무 정적 서버에서나 바로 열린다.
    ///  - Vercel 배포용: Brotli 압축 + 압축 해제 폴백 없음. 서버가 Content-Encoding
    ///    헤더를 내려주는 것을 전제로 한다. divisorGame/vercel.json이 그 헤더를 지정한다.
    ///  - Unity Play 업로드용: Brotli 압축 + 압축 해제 폴백 있음. 업로드형 호스팅은
    ///    응답 헤더를 우리가 지정할 수 없으므로, 헤더가 없어도 로더가 스스로 풀 수 있어야 한다.
    /// </summary>
    public static class WebGLBuilder
    {
        private const string LocalOutputDirectory = "Builds/WebGL";
        private const string DeployOutputDirectory = "divisorGame";
        private const string UnityPlayOutputDirectory = "Builds/UnityPlay";

        /// <summary>브라우저 창 크기에 맞춰 캔버스를 채우는 커스텀 템플릿.</summary>
        private const string ResponsiveTemplate = "PROJECT:Responsive";

        [MenuItem("약수 카드게임/WebGL 빌드 (로컬 확인용)", false, 20)]
        public static void BuildFromMenu()
        {
            BuildReport report = Build(LocalOutputDirectory, WebGLCompressionFormat.Disabled, false);
            if (report == null) return;

            if (report.summary.result == BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(Path.GetFullPath(LocalOutputDirectory));
            }
        }

        [MenuItem("약수 카드게임/WebGL 빌드 (Vercel 배포용)", false, 21)]
        public static void BuildDeployFromMenu()
        {
            BuildReport report = Build(DeployOutputDirectory, WebGLCompressionFormat.Brotli, false);
            if (report == null) return;

            if (report.summary.result == BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(Path.GetFullPath(DeployOutputDirectory));
            }
        }

        [MenuItem("약수 카드게임/WebGL 빌드 (Unity Play 업로드용)", false, 22)]
        public static void BuildUnityPlayFromMenu()
        {
            BuildReport report = Build(UnityPlayOutputDirectory, WebGLCompressionFormat.Brotli, true);
            if (report == null) return;

            if (report.summary.result != BuildResult.Succeeded) return;

            Debug.Log("Unity Play 업로드용 빌드 완료. deploy/unityplay-zip.sh 를 실행해 zip을 만든 뒤 "
                      + "play.unity.com 에서 업로드하세요.");
            EditorUtility.RevealInFinder(Path.GetFullPath(UnityPlayOutputDirectory));
        }

        /// <summary>CLI용 진입점(로컬 확인용). 실패하면 0이 아닌 코드로 종료한다.</summary>
        public static void BuildFromCommandLine()
        {
            Exit(Build(LocalOutputDirectory, WebGLCompressionFormat.Disabled, false));
        }

        /// <summary>CLI용 진입점(Vercel 배포용). 실패하면 0이 아닌 코드로 종료한다.</summary>
        public static void BuildDeployFromCommandLine()
        {
            Exit(Build(DeployOutputDirectory, WebGLCompressionFormat.Brotli, false));
        }

        /// <summary>CLI용 진입점(Unity Play 업로드용). 실패하면 0이 아닌 코드로 종료한다.</summary>
        public static void BuildUnityPlayFromCommandLine()
        {
            Exit(Build(UnityPlayOutputDirectory, WebGLCompressionFormat.Brotli, true));
        }

        private static void Exit(BuildReport report)
        {
            bool ok = report != null && report.summary.result == BuildResult.Succeeded;
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static BuildReport Build(
            string outputDirectory, WebGLCompressionFormat compression, bool decompressionFallback)
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

            // 폴백을 켜면 서버가 Content-Encoding을 내려주지 않아도 로더가 스스로 압축을 푼다.
            // 응답 헤더를 우리가 지정할 수 있는 곳(Vercel)에서는 꺼서 로더 용량을 아낀다.
            PlayerSettings.WebGL.decompressionFallback = decompressionFallback;

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
