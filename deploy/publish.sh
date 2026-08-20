#!/usr/bin/env bash
# WebGL 배포 스크립트.
#
# Unity 빌드는 divisorGame/ 폴더를 다시 만들기 때문에 Vercel 설정 파일이 사라진다.
# 이 저장소의 deploy/ 안에 있는 정본을 빌드 결과물로 복사한 뒤 배포한다.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="$ROOT/divisorGame"

if [ ! -f "$OUT/index.html" ]; then
  echo "빌드 결과물이 없습니다: $OUT" >&2
  echo "Unity에서 '약수 카드게임 > WebGL 빌드 (배포용)'을 먼저 실행하세요." >&2
  exit 1
fi

cp "$ROOT/deploy/vercel.json"    "$OUT/vercel.json"
cp "$ROOT/deploy/.vercelignore"  "$OUT/.vercelignore"
echo "Vercel 설정 복사 완료"

cd "$OUT"
vercel --prod "$@"
