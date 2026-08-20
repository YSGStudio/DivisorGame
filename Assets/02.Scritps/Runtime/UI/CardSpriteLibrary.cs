using UnityEngine;

namespace DivisorGame.UI
{
    /// <summary>
    /// 숫자 카드 그림을 번호로 찾아 준다.
    ///
    /// 그림은 Assets/04.Images/Resources/cards/{숫자}.png 이다. 씬에 프리팹이나 참조를 두지 않고
    /// 코드로만 화면을 만드는 구조라, 인스펙터 연결이 필요 없는 Resources 로드를 쓴다.
    /// 처음 쓰인 숫자만 불러와 캐시하므로, 게임 한 판에서 실제로 등장한 카드만 메모리에 올라간다.
    /// </summary>
    public static class CardSpriteLibrary
    {
        public const int MinNumber = 1;
        public const int MaxNumber = 100;

        /// <summary>카드 그림의 가로/세로 비율(원본 1260 x 1760).</summary>
        public const float AspectRatio = 1260f / 1760f;

        private const string ResourceFolder = "cards/";

        private static readonly Sprite[] Cache = new Sprite[MaxNumber - MinNumber + 1];
        private static readonly bool[] Attempted = new bool[MaxNumber - MinNumber + 1];

        /// <summary>해당 숫자의 카드 그림. 준비된 그림이 없으면 null.</summary>
        public static Sprite Get(int number)
        {
            if (number < MinNumber || number > MaxNumber) return null;

            int index = number - MinNumber;
            if (!Attempted[index])
            {
                Cache[index] = Resources.Load<Sprite>(ResourceFolder + number);
                Attempted[index] = true;
            }
            return Cache[index];
        }

        /// <summary>세로 길이를 정하면 그림 비율에 맞는 가로 길이를 함께 돌려준다.</summary>
        public static Vector2 SizeForHeight(float height)
        {
            return new Vector2(Mathf.Round(height * AspectRatio), height);
        }
    }
}
