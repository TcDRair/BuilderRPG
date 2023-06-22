using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildSector : MonoBehaviour
{
    public static GameObject cell; // 셀 프리팹입니다. 씬 시작 시 로드합니다.
    public float cellWidth = 2, cellHeight = 3; // 셀의 물리적 크기를 설정합니다.
    public float length, width; // 셀의 종횡 개수를 설정합니다.

    public class MapOverlay {
        public Transform grid;
        public MapOverlay(int length, int width, Vector2 pos) {
            // 오버레이 오브젝트를 생성합니다.
        }
        
        public void Pos(Vector2 pos) {
            // snap overlay position
        }

        public void Confirm() { /*현재 오버레이된 좌표로 맵을 확정*/ }
    }

    public static BuildSector buildSector;
    // Singleton 패턴 (인터넷에서 긁어옴)
    public static BuildSector GetSector() {
        if (!buildSector) {
            buildSector = FindAnyObjectByType(typeof(BuildSector)) as BuildSector;
            if (!buildSector) {
                GameObject obj = new("sector");
                buildSector = obj.AddComponent(typeof(BuildSector)) as BuildSector;
            }
        }
        return buildSector;
    }
}
