#!/usr/bin/env bash
# Unity Play(play.unity.com) 업로드용 zip을 만든다.
#
# Unity Play는 zip 안의 최상단에 index.html이 있어야 인식한다. 그래서 폴더째로
# 압축하지 않고 폴더 "안의 내용"을 담는다. 실행에 필요 없는 Burst 디버그 정보와
# macOS의 .DS_Store는 용량만 차지하므로 뺀다.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC="${1:-$ROOT/Builds/UnityPlay}"
OUT="${2:-$ROOT/Builds/UnityPlay.zip}"

if [ ! -f "$SRC/index.html" ]; then
  echo "빌드 결과물이 없습니다: $SRC" >&2
  echo "Unity에서 '약수 카드게임 > WebGL 빌드 (Unity Play 업로드용)'을 먼저 실행하세요." >&2
  exit 1
fi

rm -f "$OUT"
mkdir -p "$(dirname "$OUT")"

# -r 재귀, -q 조용히, -X macOS 확장 속성 제외
# Vercel용 파일이 섞여 있는 폴더를 가리켜도 그대로 올라가지 않도록 함께 뺀다.
(cd "$SRC" && zip -r -q -X "$OUT" . \
  -x '*_BurstDebugInformation_DoNotShip/*' \
  -x '*.DS_Store' \
  -x '__MACOSX/*' \
  -x '.vercel/*' \
  -x 'vercel.json' \
  -x '.vercelignore' \
  -x '.gitignore' \
  -x 'ProjectVersion.txt')

echo "zip 생성 완료: $OUT ($(du -h "$OUT" | cut -f1))"
echo
echo "업로드 방법:"
echo "  1. https://play.unity.com/ 에 로그인"
echo "  2. 우측 상단 Upload(또는 프로젝트 > New Project) 선택"
echo "  3. 위 zip 파일을 올리고 제목/설명을 입력한 뒤 게시"
