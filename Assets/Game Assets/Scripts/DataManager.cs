using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Rair.Core
{
  public class DataManager : MonoBehaviour
  {
    [SerializeField] protected TextAsset MapJson;
    [SerializeField] protected Text Loading;
    protected void Start() => StartCoroutine(InitData());
    protected void Update() => Loading.text = $"{Time.time:F2}s";
 
    IEnumerator InitData() {
      // yield return LoadData(Storage.Map, MapJson.text);

      Loading.CrossFadeAlpha(0, 1, true);
      yield return new WaitForSeconds(1);
      SceneManager.LoadScene("Tech Scene");
    }

    public readonly float Interval = .02f;
    float m_prev = 0;
    bool Elapsed => (m_prev + Interval > Time.time) && (m_prev = Time.time) > 0;
  }

  public static class Storage {

  }
}
