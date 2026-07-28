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

  _targetRenderer = target.GetComponentInChildren<Renderer>().GetInstanceID();
  RenderPipelineManager.beginCameraRendering += MakeTranslucencyProps;
}

protected void OnDestroy() {
  RenderPipelineManager.beginCameraRendering -= MakeTranslucencyProps;
}
```

정적 이벤트라 구독 해제를 빠뜨리면 파괴된 객체를 계속 호출합니다.
`OnDestroy`에서 반드시 떼야 합니다.

콜백 시그니처가 `(ScriptableRenderContext, Camera)`인 것이 중요합니다.
**카메라마다 한 번씩** 불립니다 — 이 사실을 놓친 대가는 5절에서 다룹니다.

### 3.2 가림 판정

카메라에서 플레이어를 향하는 **선분**에 `RaycastAll`을 쏘고, 걸린 것을 전부 후보로 봅니다.

```csharp
Vector3 line = target.position - T.position;

foreach (var hit in Physics.RaycastAll(T.position, line, line.magnitude - 0.2f)) {
  Renderer renderer;
  if (
    ( // 렌더러 존재 검사 (자기 자신 또는 자식)
      (renderer = hit.collider.GetComponent<Renderer>()) == null &&
      (renderer = hit.collider.GetComponentInChildren<Renderer>()) == null
    ) ||
    renderer.GetInstanceID() == _targetRenderer ||   // 플레이어 본인 제외
    renderer.enabled is false ||                     // 꺼진 렌더러 제외
    renderer.material.shader.name is not shaderName  // URP Lit 아닌 것 제외
  ) continue;

  blockings.Add(hit.colliderInstanceID);
  ...
}
```

레이 길이를 `magnitude - 0.2f`로 **살짝 줄인 것**이 실용적인 요령입니다.
플레이어가 딛고 선 바닥 타일이 매 프레임 걸려서 바닥이 통째로 투명해지는 것을 막습니다.

필터 조건을 `||` 체인으로 묶고 `continue`로 빠지는 형태라,
가장 싼 검사(널 체크)부터 가장 비싼 검사(셰이더 이름 비교) 순으로 단락 평가됩니다.

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
const float MIN_ALPHA = 0.85f, FADE_SPEED = 0.02f;

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
  //* 1. 가리는 중 → MIN_ALPHA까지 점점 투명해진다
  if (blockings.Contains(pair.Key)) {
    if (pair.Value.targeted is false)
      pair.Value.material.ToFadeMode();
    var color = pair.Value.material.color;
    color.a = Mathf.Clamp(color.a - FADE_SPEED, MIN_ALPHA, 1f);
    pair.Value.material.color = color;
  }
  //* 2. 안 가림 → 알파를 회복하고, 다 회복하면 추적 해제
  else {
    var color = pair.Value.material.color;
    color.a = Mathf.Clamp(color.a + FADE_SPEED, MIN_ALPHA, pair.Value.alpha);
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
`FADE_SPEED`를 더하다 우연히 일치하기를 기다리는 게 아닙니다.

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

## 5. 한계와 알려진 문제

**콜백이 카메라마다 실행됩니다.** 가장 큰 문제입니다.
`beginCameraRendering`은 **렌더링되는 모든 카메라에 대해** 호출되는데,
`MakeTranslucencyProps`는 받은 `Camera cam` 인자를 **한 번도 쓰지 않습니다.**

```csharp
protected void MakeTranslucencyProps(ScriptableRenderContext context, Camera cam) {
  //                                                          ^^^ 사용되지 않음
```

메인 카메라 외에 UI 카메라나 리플렉션 프로브가 있으면 한 프레임에 여러 번 돌아
**페이드 속도가 카메라 수만큼 빨라집니다.**
에디터에서는 씬 뷰 카메라까지 이걸 트리거합니다.
맨 앞에 `if (cam != Cam) return;` 한 줄이 있어야 합니다.

**`targeted` 플래그가 연결되지 않았습니다.**
`Blender.targeted`는 "이미 투명 모드로 바꿨는가"를 기억해
`ToFadeMode()`를 한 번만 부르려고 만든 필드인데,
**어디서도 `true`로 설정되지 않습니다.**

```csharp
if (pair.Value.targeted is false)   // 항상 참
  pair.Value.material.ToFadeMode();
```

결과적으로 가리는 오브젝트마다 매 프레임 키워드 설정과 패스 토글이 반복됩니다.
동작은 하지만(멱등이라) 불필요한 비용입니다.

**`MIN_ALPHA`가 0.85입니다.** 85% 불투명이면 사실상 거의 안 비칩니다.
튜닝 과정에서 남은 값으로 보이며, 0.2~0.4 정도가 의도에 맞을 것입니다.

**`renderer.material` 접근이 머티리얼을 복제합니다.**
Unity에서 `Renderer.material`(≠ `sharedMaterial`)을 읽으면
그 순간 렌더러 전용 인스턴스가 생성됩니다.
필터 검사 단계에서 이미 호출하고 있어서, **스쳐 지나간 오브젝트까지 인스턴스가 생기고**
SRP 배칭에서 빠집니다. 필터는 `sharedMaterial`로 하고
실제 조작 시점에만 `material`을 잡는 편이 맞습니다.

**플레이어의 렌더러를 하나만 제외합니다.**
`GetComponentInChildren<Renderer>()`는 **첫 번째** 렌더러만 반환합니다.
캐릭터가 몸·옷·무기로 나뉘면 나머지가 가림 대상으로 잡혀 자기 자신이 투명해집니다.

**레이캐스트에 레이어 마스크가 없습니다.**
바닥은 레이 길이를 줄여서 피하고 있지만,
`MainSetting`에 이미 정의된 레이어 마스크를 쓰면 더 싸고 정확합니다.

**`blockings`가 `List<int>`입니다.**
`Contains()`가 선형 탐색이라 추적 대상이 많아지면 `O(n·m)`이 됩니다.
`HashSet<int>`로 바꾸면 됩니다. 현재 규모에서는 문제되지 않습니다.

**플레이어 빌드가 깨집니다.**
같은 파일 상단에 `using UnityEditor;`와 `[CustomEditor]` 클래스가
`#if UNITY_EDITOR` 가드 없이 들어 있습니다.
에디터에서는 동작하지만 플레이어 빌드에서 컴파일 오류가 납니다.
`Prop.cs`도 같은 문제를 가지고 있어, 프로젝트 차원에서 함께 정리해야 합니다.

**`ToOpaqueMode()`가 죽은 코드입니다.**
3.3절에서 설명한 설계 변경의 잔재로, 정의만 있고 호출부가 없습니다.

---

## 6. 코드 위치

| 영역 | 경로 |
|---|---|
| 카메라 · 가림 판정 · 페이드 | `Assets/Game Assets/Scripts/Fields/MainCamera.cs` |
| 머티리얼 상태 구조체 · 확장 메서드 | 같은 파일 하단 (`URPLitVar`, `URPLitExtensions`) |
| 레이어 마스크 상수 | `Assets/Game Assets/Scripts/MainSetting.cs` |
