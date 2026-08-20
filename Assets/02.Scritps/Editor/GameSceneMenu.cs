using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DivisorGame.EditorTools
{
    /// <summary>
    /// 에디터 메뉴에서 게임 씬을 바로 열고 플레이할 수 있게 한다.
    /// (Assembly-CSharp-Editor에 포함되므로 별도 asmdef가 필요 없다.)
    /// </summary>
    public static class GameSceneMenu
    {
        private const string ScenePath = "Assets/01.Scene/DivisorCardGame.unity";

        [MenuItem("약수 카드게임/게임 씬 열기", false, 0)]
        public static void OpenScene()
        {
            TryOpenScene();
        }

        [MenuItem("약수 카드게임/씬 열고 바로 플레이", false, 1)]
        public static void OpenSceneAndPlay()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            if (TryOpenScene()) EditorApplication.isPlaying = true;
        }

        [MenuItem("약수 카드게임/씬 열고 바로 플레이", true)]
        public static bool ValidateOpenSceneAndPlay()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying;
        }

        private static bool TryOpenScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError("게임 씬을 찾을 수 없습니다: " + ScenePath);
                return false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return true;
        }
    }
}
