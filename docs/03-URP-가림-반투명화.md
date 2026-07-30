# URP 가림 오브젝트 반투명화

> Unity 2023.2 / URP 16.0.4 · C# · 개인 프로젝트 *BuilderRPG* 중 카메라 파트

## 1. 개요

쿼터뷰 시점 게임에서 늘 나오는 문제입니다.
**카메라와 플레이어 사이에 나무나 건물이 끼면 캐릭터가 안 보입니다.**

해결 방향 자체는 흔합니다 — 가리는 오브젝트를 반투명하게 만들면 됩니다.
문제는 이걸 **URP에서** 하려니 빌트인 렌더 파이프라인 시절의 방법이 하나도 안 통했다는 점입니다.

분량은 200줄 남짓이지만, 엔진 내부 제약 세 개를 차례로 우회해야 했습니다.
이 문서는 그 세 개를 다룹니다.

| 제약 | 우회 |
|---|---|
| SRP에서 카메라 렌더 콜백이 호출되지 않는다 | `RenderPipelineManager` 이벤트 구독 |
| URP Lit의 투명 전환은 단일 스위치가 아니다 | 9개 상태를 한 번에 조작하는 확장 메서드 |
| 원래 상태로 정확히 되돌려야 한다 | 상태 전체를 구조체로 스냅샷 후 복원 |

| 적용 전 | 적용 후 |
|---|---|
| ![불투명](images/translucency-01-opaque.png) | ![반투명](images/translucency-02-faded.png) |
| 가림 오브젝트가 캐릭터를 완전히 덮는다 | `minAlpha` 0.6까지 페이드되어 캐릭터가 드러난다 |

같은 카메라·같은 배치이고, 차이는 이 문서가 다루는 로직이 도는지 여부뿐입니다.
왼쪽은 에디트 모드(`Awake`가 실행되지 않아 콜백이 등록되지 않은 상태),
오른쪽은 플레이 모드입니다. 오른쪽에 HP/SP·피로 UI가 함께 뜨는 것도 그 때문입니다.

> 확인에 쓴 것은 임시 Lit 큐브입니다.
> 원래 대상인 GridCell 기반 건축물 프리팹은 `m_Mesh`가 유실 상태라 쓸 수 없었습니다(5절).

---

## 2. 문제

### 2.1 `OnPreCull()`이 죽는다

빌트인 렌더 파이프라인에서는 카메라가 컬링/렌더링을 시작할 때
같은 게임오브젝트의 `MonoBehaviour`에 매직 메서드가 불립니다.

```csharp
void OnPreCull()   { }   // 컬링 직전
void OnPreRender() { }   // 렌더 직전
void OnPostRender(){ }   // 렌더 직후
```

가림 처리는 관례적으로 `OnPreCull()`에 넣습니다.
그런데 **URP를 포함한 SRP에서는 이 메서드들이 전혀 호출되지 않습니다.**
렌더 루프를 엔진이 아니라 파이프라인 에셋이 돌리기 때문입니다.

조용히 아무 일도 일어나지 않는 종류의 문제라, 원인을 찾는 데 시간이 걸렸습니다.
코드는 멀쩡하고 컴파일도 되고 에러도 없는데 그냥 실행되지 않습니다.

### 2.2 URP Lit의 "투명"은 스위치가 아니다

머티리얼을 반투명하게 만들려면 알파만 낮추면 될 것 같지만, 아닙니다.
URP Lit 셰이더에서 불투명↔투명 전환은 **아홉 가지 상태를 동시에** 맞춰야 합니다.

| 상태 | 불투명 | 투명 | 안 맞추면 |
|---|---|---|---|
| `_SURFACE_TYPE_TRANSPARENT` 키워드 | off | on | 셰이더 분기가 불투명 경로를 탄다 |
| `_ALPHAPREMULTIPLY_ON` 키워드 | off | on | 알파 합성이 어긋난다 |
| `renderQueue` | `-1` (셰이더 기본) | `3000` | 그리는 순서가 틀려 뒤 물체가 사라진다 |
| `_DstBlend` | `Zero` | `OneMinusSrcAlpha` | 알파가 화면에 반영되지 않는다 |
| `_DstBlendAlpha` | `Zero` | `OneMinusSrcAlpha` | 알파 채널 합성이 어긋난다 |
| `_Surface` | `0` | `1` | 인스펙터 표시와 실제가 어긋난다 |
| `_ZWrite` | `1` | `0` | 자기 자신이 자기를 가린다 |
| `DepthOnly` 패스 | on | off | **깊이 프리패스가 여전히 불투명으로 기록된다** |
| `ShadowCaster` 패스 | on | off | 그림자가 불투명한 채로 남는다 |

인스펙터에서 Surface Type을 바꾸면 URP의 `ShaderGUI`가 이 아홉 개를 한 번에 처리합니다.
하지만 **런타임에서 같은 일을 해주는 공개 API가 없습니다.**
직접 다 만져야 합니다.

특히 아래 두 줄이 함정이었습니다.

```csharp
material.SetShaderPassEnabled("DepthOnly", false);
material.SetShaderPassEnabled("ShadowCaster", false);
```

키워드와 블렌드만 바꾸고 이 둘을 빠뜨리면,
**색은 반투명해졌는데 깊이 버퍼에는 여전히 불투명하게 기록되어**
뒤에 있는 플레이어가 계속 가려집니다. 정확히 하려던 일이 안 되는 상태입니다.

### 2.3 되돌리기

가림이 풀리면 원래대로 돌아와야 합니다.
"원래대로"를 **불투명 기본값으로 가정하면 안 됩니다** —
애초에 반투명이었던 유리창이나, 알파가 0.9였던 오브젝트가 섞여 있기 때문입니다.

---

## 3. 접근

### 3.1 렌더 파이프라인 훅

`OnPreCull` 대신 `RenderPipelineManager`의 정적 이벤트를 구독합니다.

```csharp
protected void Awake() {
  Cam = GetComponent<Camera>();
  T = transform;
  Instance = this;

  // 캐릭터가 몸·옷·무기로 나뉠 수 있으므로 렌더러를 전부 수집한다
  foreach (var r in target.GetComponentsInChildren<Renderer>(true))
    _targetRenderers.Add(r.GetInstanceID());

  RenderPipelineManager.beginCameraRendering += MakeTranslucencyProps;
}

protected void OnDestroy() {
  RenderPipelineManager.beginCameraRendering -= MakeTranslucencyProps;
}
```

정적 이벤트라 구독 해제를 빠뜨리면 파괴된 객체를 계속 호출합니다.
`OnDestroy`에서 반드시 떼야 합니다.

콜백 시그니처가 `(ScriptableRenderContext, Camera)`인 것이 중요합니다.
**렌더링되는 카메라마다 한 번씩** 불리므로, 자기 카메라인지 먼저 걸러야 합니다.

```csharp
protected void MakeTranslucencyProps(ScriptableRenderContext context, Camera cam) {
  if (cam != Cam) return;   // UI 카메라·리플렉션 프로브·씬 뷰 카메라 제외
  ...
```

이 한 줄이 처음에는 없었습니다. 그 대가는 5절에 적었습니다.

### 3.2 가림 판정

카메라에서 플레이어를 향하는 **선분**에 `RaycastAll`을 쏘고, 걸린 것을 전부 후보로 봅니다.

```csharp
Vector3 line = target.position - T.position;

foreach (var hit in Physics.RaycastAll(T.position, line, line.magnitude - floorGap, blockingLayers)) {
  Renderer renderer;
  if (
    ( // 렌더러 존재 검사 (자기 자신 또는 자식)
      (renderer = hit.collider.GetComponent<Renderer>()) == null &&
      (renderer = hit.collider.GetComponentInChildren<Renderer>()) == null
    ) ||
    _targetRenderers.Contains(renderer.GetInstanceID()) ||  // 플레이어 본인 제외
    renderer.enabled is false ||                            // 꺼진 렌더러 제외
    renderer.sharedMaterial == null ||
    renderer.sharedMaterial.shader.name is not shaderName   // URP Lit 아닌 것 제외
  ) continue;

  blockings.Add(hit.colliderInstanceID);
  ...
}
```

레이 길이를 `floorGap`(기본 0.2)만큼 **살짝 줄인 것**이 실용적인 요령입니다.
플레이어가 딛고 선 바닥 타일이 매 프레임 걸려서 바닥이 통째로 투명해지는 것을 막습니다.

필터 조건을 `||` 체인으로 묶고 `continue`로 빠지는 형태라,
가장 싼 검사(널 체크)부터 가장 비싼 검사(셰이더 이름 비교) 순으로 단락 평가됩니다.

> **필터에서는 반드시 `sharedMaterial`을 봐야 합니다.**
> `Renderer.material`은 읽는 순간 렌더러 전용 인스턴스를 생성하므로,
> 여기서 쓰면 **스쳐 지나간 오브젝트까지 머티리얼이 복제되고** SRP 배칭에서 빠집니다.
> 실제 조작 대상으로 확정된 뒤에만 `material`을 한 번 잡아 재사용합니다.

### 3.3 상태 스냅샷 — `URPLitVar`

2.2절의 아홉 상태를 구조체 하나로 묶었습니다.

```csharp
public struct URPLitVar {
  public bool APM_ON, STT;      // 키워드 2종
  public int RQ, DB, DBA, S, ZW;// renderQueue, DstBlend, DstBlendAlpha, Surface, ZWrite
  public bool DO, SC;           // DepthOnly, ShadowCaster 패스
}
```

읽기와 쓰기를 대칭 확장 메서드로 만듭니다.

```csharp
public static URPLitVar GetLitVar(this Material material)
  => new() {
    APM_ON = material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"),
    STT    = material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"),
    RQ     = material.renderQueue,
    DB     = material.GetInt("_DstBlend"),
    DBA    = material.GetInt("_DstBlendAlpha"),
    S      = material.GetInt("_Surface"),
    ZW     = material.GetInt("_ZWrite"),
    DO     = material.GetShaderPassEnabled("DepthOnly"),
    SC     = material.GetShaderPassEnabled("ShadowCaster")
  };

public static void SetLitVar(this Material material, URPLitVar var) {
  if (var.APM_ON) material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
  else            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
  if (var.STT)    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
  else            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
  material.renderQueue = var.RQ;
  material.SetShaderPassEnabled("DepthOnly",   var.DO);
  material.SetShaderPassEnabled("ShadowCaster",var.SC);
  material.SetInt("_DstBlend",      var.DB);
  material.SetInt("_DstBlendAlpha", var.DBA);
  material.SetInt("_Surface",       var.S);
  material.SetInt("_ZWrite",        var.ZW);
}
```

그리고 투명 전환은 별도 메서드로 묶습니다.

```csharp
public static void ToFadeMode(this Material material) {
  material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
  material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
  material.renderQueue = (int)RenderQueue.Transparent;
  material.SetShaderPassEnabled("DepthOnly",    false);
  material.SetShaderPassEnabled("ShadowCaster", false);
  material.SetInt("_DstBlend",      (int)BlendMode.OneMinusSrcAlpha);
  material.SetInt("_DstBlendAlpha", (int)BlendMode.OneMinusSrcAlpha);
  material.SetInt("_Surface", 1);
  material.SetInt("_ZWrite",  0);
}
```

**설계가 한 번 뒤집힌 지점입니다.**
처음에는 `ToFadeMode()`의 짝으로 `ToOpaqueMode()`를 만들어 복원에 쓰려 했습니다.
그런데 2.3절의 문제 — 원래 불투명이 아니었던 머티리얼 — 를 만나면서
**"불투명으로 되돌린다"가 아니라 "찍어둔 상태로 되돌린다"**로 바꿨습니다.
그래서 복원은 `ToOpaqueMode()`가 아니라 `SetLitVar(snapshot)`이 담당합니다.

> `ToOpaqueMode()`는 지금도 코드에 남아 있지만 호출되지 않습니다.
> 설계가 바뀐 흔적입니다. 5절에 정리했습니다.

### 3.4 페이드 상태 기계

추적 대상을 딕셔너리에 담고, 매 프레임 두 방향으로 알파를 움직입니다.

```csharp
//? 눈으로 보면서 정해야 하는 값이라 인스펙터로 노출한다 (5절)
[Range(0f, 1f)]     public float minAlpha  = 0.6f;
[Range(0.001f, .2f)]public float fadeSpeed = 0.02f;

class Blender {
  public Material material;
  public URPLitVar matVar;   // 원래 상태 스냅샷
  public float alpha;        // 원래 알파
  public bool targeted = false;
}
readonly Dictionary<int, Blender> blenders = new();
```

```csharp
foreach (var pair in blenders.ToArray()) {   // 순회 중 제거하므로 복사
  //* 1. 가리는 중 → minAlpha까지 점점 투명해진다
  if (blockings.Contains(pair.Key)) {
    if (pair.Value.targeted is false) {
      pair.Value.material.ToFadeMode();
      pair.Value.targeted = true;          // 전환은 한 번만
    }
    var color = pair.Value.material.color;
    color.a = Mathf.Clamp(color.a - fadeSpeed, minAlpha, 1f);
    pair.Value.material.color = color;
  }
  //* 2. 안 가림 → 알파를 회복하고, 다 회복하면 추적 해제
  else {
    var color = pair.Value.material.color;
    color.a = Mathf.Clamp(color.a + fadeSpeed, minAlpha, pair.Value.alpha);
    pair.Value.material.color = color;
    if (color.a == pair.Value.alpha) {
      pair.Value.material.SetLitVar(pair.Value.matVar);   // 상태 원복
      blenders.Remove(pair.Key);                          // 추적 종료
    }
  }
}
```

**부동소수 비교가 안전한 이유.** `color.a == pair.Value.alpha`는 보통 위험한 코드지만
여기서는 성립합니다. `Mathf.Clamp`의 상한이 정확히 `pair.Value.alpha`라
포화되는 순간 **비트가 같은 값**이 되기 때문입니다.
`fadeSpeed`를 더하다 우연히 일치하기를 기다리는 게 아닙니다.

복원이 끝나면 딕셔너리에서 빼서 **추적 비용이 자연히 사라집니다.**
가릴 것이 없으면 매 프레임 도는 대상이 0개가 됩니다.

---

## 4. 설계 판단 기록

**왜 오브젝트를 끄지 않고 반투명화했나.**
가리는 오브젝트를 `SetActive(false)` 하는 방법이 가장 싸지만,
나무가 깜빡깜빡 사라져 시각적으로 거슬립니다.
알파 페이드는 그림자와 실루엣을 남겨서 "저기 나무가 있다"는 정보를 유지합니다.

**왜 셰이더 대신 머티리얼 상태를 조작했나.**
디더링 기반 가림 셰이더를 따로 만드는 방법이 정석에 가깝습니다.
다만 그러면 **가림 대상이 될 수 있는 모든 에셋의 머티리얼을 교체**해야 합니다.
이 프로젝트는 서드파티 에셋 프롭을 그대로 쓰고 있어서,
런타임에 표준 URP Lit을 그대로 다루는 편이 도입 비용이 훨씬 낮았습니다.

**왜 콜라이더 인스턴스 ID를 키로 썼나.**
`Renderer` 참조를 키로 쓰면 오브젝트가 파괴될 때 딕셔너리에 죽은 참조가 남습니다.
`int` 키는 그럴 일이 없고, 해시 비용도 낮습니다.

**왜 딕셔너리를 매 프레임 비우지 않나.**
페이드는 여러 프레임에 걸친 상태 전이입니다.
가림이 풀린 뒤에도 알파를 되돌리는 동안 계속 추적해야 하므로,
"가리는 목록(`blockings`)"과 "추적 목록(`blenders`)"을 분리했습니다.
전자는 매 프레임 초기화되고, 후자는 복원 완료 시점에만 줄어듭니다.

---

## 5. 이후 보완된 것

이 문서를 쓰면서 정리한 결함들을 별도 보완 작업으로 처리했습니다.
경위는 [보완 필요 항목](05-보완-필요-항목.md) 문서에 있습니다.

| 당시 상태 | 현재 |
|---|---|
| 콜백이 모든 카메라에서 실행 | 진입부에 `if (cam != Cam) return;` |
| `targeted` 플래그 미연결 | `ToFadeMode()` 직후 `true` 대입 |
| `blockings`가 `List<int>` | `HashSet<int>` |
| 필터 단계에서 `renderer.material` 접근 | `sharedMaterial`로 검사, 조작 시점에만 `material` |
| 플레이어 렌더러를 하나만 제외 | `HashSet<int>`로 전부 수집 |
| 튜닝 값이 `const` | `minAlpha`·`fadeSpeed`·`blockingLayers`·`floorGap` 인스펙터 노출 |
| 플레이어 빌드 실패 | 어셈블리 분리로 에디터 코드 격리 |
| `ToOpaqueMode()` 죽은 코드 | 삭제 대신 **왜 못 쓰는지**를 문서 주석으로 명시 |

`ToOpaqueMode()`에 남긴 것은 "왜 안 쓰는지"가 아니라 **왜 못 쓰는지**입니다 —
대상 머티리얼이 원래 Opaque라는 보장이 없어 상수 복원이 성립하지 않고,
그래서 `GetLitVar`/`SetLitVar` 스냅샷 방식으로 바뀌었다는 것.
이걸 적어 두지 않으면 "복원 함수가 있는데 왜 안 쓰지?"에서 다시 출발하게 됩니다.

### `MIN_ALPHA` — 추정이 틀렸습니다

이 문서는 *"85% 불투명이면 사실상 거의 안 비칩니다"*라고 적고
0.2~0.4를 제안했습니다. **둘 다 틀렸습니다.**

플레이 모드에서 실제로 비교한 결과입니다.

| 값 | 결과 |
|---|---|
| 0.85 | 플레이어는 **보입니다**. 다만 다소 씻긴 느낌 |
| **0.60** | **가림 오브젝트의 형태가 남으면서 플레이어가 또렷함** ← 채택 |
| 0.30 | 오브젝트가 거의 사라져 공간감을 잃음 |

`_ALPHAPREMULTIPLY_ON` 블렌딩이라 **수치보다 투명하게 보입니다**(2.2절).
알파 값만 보고 화면을 추측한 것이 잘못이었습니다.

### 값이 몇 달간 방치된 진짜 이유

`minAlpha`를 0.85에서 0.3으로 낮췄는데 **화면이 전혀 변하지 않는** 구간이 있었습니다.
원인은 3.2절의 셰이더 필터입니다.

```csharp
renderer.sharedMaterial.shader.name is not shaderName   // "Universal Render Pipeline/Lit"
```

씬 프롭이 쓰는 셰이더 분포는 이렇습니다.

| 셰이더 | 개수 |
|---|---|
| `Polyart/Dreamscape Surface` (+WorldAligned) | 3 — 바위 등 대부분의 프롭 |
| `Universal Render Pipeline/Lit` | 2 |
| TerrainLit / SimpleLit / ToonWater / Sprite-Lit / Decal | 각 1 |

**이것은 결함이 아니라 의도된 범위입니다.**
GridCell 기반 건축물(`Wooden Shelter`, `Wall` 등)이 Lit 기반이라 거기에만 적용한 로직이고,
그 에셋들이 현재 씬에 없어서 효과가 보이지 않았을 뿐입니다.

`MIN_ALPHA = 0.85`가 방치된 이유가 이걸로 설명됩니다 — **눈에 보인 적이 없습니다.**
확인은 임시 Lit 큐브로 했고, 씬에 `Translucency Test Block`(비활성)으로 남겨 두었습니다.

> 튜닝 값을 `const`로 둔 것이 문제를 키웠습니다.
> 값을 바꿀 때마다 재컴파일 후 다시 플레이해야 해서
> "눈으로 보면서 정한다"가 사실상 불가능했습니다.
> 인스펙터로 빼고 나서야 세 값을 한자리에서 비교할 수 있었습니다.

---

## 6. 남은 한계

- **적용 범위가 URP Lit 전용입니다.**
  의도한 대상(건축물)이 Lit 기반이라 그렇게 두었지만,
  현재 씬 프롭 대부분은 `Polyart/Dreamscape Surface`를 씁니다.
  범위를 넓힐지는 건축 시스템이 돌아온 뒤에 판단하는 편이 낫습니다.
- **`blockingLayers`가 아직 "전체"입니다.**
  인스펙터로 빼두기만 했고 레이어를 고르지 않았습니다.
  그래서 `floorGap`(레이 길이 단축 0.2)도 남겨 두었습니다 —
  마스크가 전체인 상태에서 길이까지 늘리면 플레이어 발밑 바닥이 가림 대상이 됩니다.
  레이어를 정하고 나면 `floorGap`은 0으로 두면 됩니다.
- **원래 대상으로 검증하지 못했습니다.**
  건축물 프리팹(`Corner Wall`·`Edge Wall`·`Diagonal Wall`·`Floor`·`Wall`)의
  `m_Mesh`가 전부 유실 상태라 임시 큐브로 대신했습니다.
  에셋 복구가 선행되어야 실제 대상에서 다시 확인할 수 있습니다.
- **캐릭터가 임시 모델입니다.**
  개요의 비교 이미지에 나오는 T 포즈 흰색 형상은 `Dummy Player`의 플레이스홀더입니다.
  가림 판정 자체는 `target`의 렌더러 전체를 제외 목록에 넣으므로 모델과 무관하게 동작하지만,
  실제 캐릭터로 바꾸면 실루엣이 커져 `minAlpha` 재조정이 필요할 수 있습니다.

---

## 7. 코드 위치

| 영역 | 경로 |
|---|---|
| 카메라 · 가림 판정 · 페이드 | `Assets/Game Assets/Scripts/Fields/MainCamera.cs` |
| 머티리얼 상태 구조체 · 확장 메서드 | 같은 파일 하단 (`URPLitVar`, `URPLitExtensions`) |
| 레이어 마스크 상수 | `Assets/Game Assets/Scripts/MainSetting.cs` |
