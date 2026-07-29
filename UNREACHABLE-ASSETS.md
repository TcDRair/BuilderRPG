# 미참조 에셋 후보

> **이 파일은 자동 생성됩니다.** `Tools/Rair/Audit/미참조 에셋 목록 생성`
> 마지막 생성 2026-07-29

## 읽는 법

**삭제 목록이 아닙니다.** 빌드 씬에서 출발해 참조를 따라갔을 때 닿지 않은 것들입니다.
Unity에서 "참조되지 않음"과 "쓰이지 않음"은 다릅니다 —
경로 문자열 로드, 에디터 전용 사용, 셰이더 변형 등은 정적 참조로 드러나지 않습니다.

판정할 수 없는 것들은 아예 뿌리로 취급해 목록에서 뺐습니다.

| 제외 경로 | 이유 |
|---|---|
| `Assets/Resources/` | Resources.Load 경로 로드 |
| `Assets/Editor/` | 에디터 도구 |
| `Assets/Imported Assets/` | 벤더 에셋 — 우리가 관리하지 않음 |
| `Assets/Samples/` | 패키지 샘플 |
| `Assets/TextMesh Pro/` | 패키지 리소스 |
| `Assets/Dagger's-AssetCleaner/` | 서드파티 도구 |
| `**/Resources/**` | `Resources.Load` 경로 로드 |
| `*.cs` | 사용 여부를 컴파일이 정함 — 에셋 참조로 판정할 수 없음 |

`ProjectSettings/*.asset`이 참조하는 것(렌더 파이프라인 에셋 등)도 뿌리에 넣었습니다.
`GetDependencies`가 `Assets/` 밖에서 출발하지 못해 생기던 오탐입니다.

## 후보 (28건 · 19.4 MB)

용량 내림차순입니다.

| 크기 | 종류 | 경로 |
|---:|---|---|
| 6,530 KB | VideoClip | `Assets/Ideas/Today's Shot/2023.08/230808.mp4` |
| 3,674 KB | VideoClip | `Assets/Ideas/Today's Shot/2023.08/230901.mp4` |
| 2,957 KB | Texture2D | `Assets/Ideas/Today's Shot/2023.08/230804.png` |
| 2,483 KB | Texture2D | `Assets/Ideas/Today's Shot/2023.08/230803.png` |
| 1,442 KB | Texture2D | `Assets/Ideas/Today's Shot/2023.08/230810.png` |
| 823 KB | Texture2D | `Assets/Ideas/Today's Shot/2023.08/230824.png` |
| 671 KB | VideoClip | `Assets/Ideas/Today's Shot/2023.08/230817.mp4` |
| 348 KB | VideoClip | `Assets/Ideas/Today's Shot/2023.08/230815.mp4` |
| 229 KB | VideoClip | `Assets/Ideas/Today's Shot/2023.08/230828.mp4` |
| 173 KB | DefaultAsset | `Assets/Ideas/필드 프롭 UI 배치.pptx` |
| 138 KB | Texture2D | `Assets/Ideas/Today's Shot/2023.08/230821.png` |
| 85 KB | SceneAsset | `Assets/Migrated/MainOld.unity` |
| 47 KB | Texture2D | `Assets/Ideas/Today's Shot/2023.08/230825.png` |
| 45 KB | Texture2D | `Assets/Ideas/Today's Shot/2023.08/230816.png` |
| 43 KB | Texture2D | `Assets/Ideas/Today's Shot/2023.08/230809.png` |
| 40 KB | SceneAsset | `Assets/Migrated/Mastery(Temp).unity` |
| 38 KB | Texture2D | `Assets/Ideas/Today's Shot/2023.08/230818.png` |
| 24 KB | Texture2D | `Assets/Ideas/Today's Shot/2023.08/230814.jpg` |
| 18 KB | SceneAsset | `Assets/Migrated/Loading.unity` |
| 18 KB | VolumeProfile | `Assets/DefaultVolumeProfile.asset` |
| 14 KB | Texture2D | `Assets/Ideas/Today's Shot/2023.08/230811.jpg` |
| 10 KB | Texture2D | `Assets/Ideas/Today's Shot/2023.08/230822.png` |
| 9 KB | DefaultAsset | `Assets/Ideas/Knowledge Structures.graphml` |
| 6 KB | Texture2D | `Assets/Ideas/Today's Shot/2023.08/230820.jpg` |
| 3 KB | Material | `Assets/Game Assets/Scripts/MapGenerator/polygon-map-unity/Unity-delaunay/Plane Material.mat` |
| 1 KB | DefaultAsset | `Assets/Game Assets/Scripts/Editors/VS Snippets/Ability.snippet` |
| 1 KB | DefaultAsset | `Assets/Game Assets/Scripts/MapGenerator/polygon-map-unity/MIT-LICENSE` |
| 1 KB | DefaultAsset | `Assets/Migrated/Json/Data/TagData.jsonc` |
