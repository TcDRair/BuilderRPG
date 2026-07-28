using UnityEngine;

/// <summary>인스펙터상에서 보이는 변수명을 주어진 문자열로 변경합니다.</summary>
//? 전역 네임스페이스에 두어 UnityEngine.InspectorNameAttribute보다 우선 해석되게 합니다.
//? (같은 수준에서 네임스페이스 멤버가 using 임포트보다 먼저 조회됨)
public class InspectorNameAttribute : PropertyAttribute
{
    public string name;
    private InspectorNameAttribute() {}
    public InspectorNameAttribute(string name) {
        this.name = name;
    }
}
