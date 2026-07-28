using UnityEngine;

/// <summary>인스펙터상에서 값을 변경할 수 없도록 처리합니다.</summary>
//? 어트리뷰트는 런타임 어셈블리에 있어야 합니다. 직렬화 대상 필드에 붙는 이상
//? 플레이어 빌드에서도 타입이 존재해야 하며, 드로어만 Rair.Editor로 분리됩니다.
public class ReadOnlyAttribute : PropertyAttribute {
  public ReadOnlyAttribute() {} // to find references
}
