using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour
{
    public static Transform trans;
    public static Camera cam;

    
    public Vector3 quaterViewPos;

    public static Ray ray {
        get => cam.ScreenPointToRay(Input.mousePosition);
    }

    // Start is called before the first frame update
    void Start() {
        trans = transform;
        cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update() {
        transform.position = Player.trans.position + quaterViewPos;
    }
}
