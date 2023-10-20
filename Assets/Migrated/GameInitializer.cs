using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using Data;
public class GameInitializer : MonoBehaviour
{
  [SerializeField] protected Image LoadingImage;
  [SerializeField] protected Sprite Loading, Error;
  [SerializeField] protected Text LoadingText;
  [SerializeField] protected CanvasGroup group;
  protected void Start() => StartCoroutine(Initialize());

  float m_lastFrameTime = 0;
  float ElapsedTime => Time.time - m_lastFrameTime;
  bool Tick => ElapsedTime > .01f && (m_lastFrameTime = Time.time) > 0;

  bool jsonProblem = false;
  IEnumerator LoadData<T>(string path, System.Action<T> action) {
    string text = Resources.Load<TextAsset>(path).text;
    int index = 0, count = 0;
    T data;
    while ((index = GetFirstItemWithinBracket(text, index, out var item)) > 0) {
      count++;
      /*try { data = JsonConvert.DeserializeObject<T>(item); }
      catch (System.Exception e) {
        Debug.Log($"Error Occured while loading {typeof(T)} Data");
        Debug.LogError($"Json Error : {e.Message}\n{e.StackTrace}\n{item}");
        jsonProblem = true;
        yield break;
      }
      action?.Invoke(data);*/
      if (Tick) { LoadingText.text = $"Loaded {count} {typeof(T).Name}..."; yield return null; }
    }
  }

  IEnumerator LoadConstData<T>(string path, Dictionary<int, T> dict) where T : IConstantData {
    yield return LoadData<T>(path, data => dict.Add(data.ID, data));
  }
  IEnumerator LoadVarData<T>(string path, Dictionary<int, T> dict) where T : IVariableData {
    yield return LoadData<T>(path, data => dict.Add(data.ID, data));
  }
  IEnumerator LoadVarData<T>(string path, List<T> list) where T : IVariableData {
    yield return LoadData<T>(path, data => list.Add(data));
  }

  IEnumerator SetDependencies() {
    var data = ConstantData.MasteryCategoryData.Values;
    foreach (var c in data) {
      var ms = c.Masteries.Select(id => ConstantData.MasteryData[id]);
      foreach (var m in ms) {
      m.SetDependencies(ms);
      if (Tick) { LoadingText.text = $"Loading Dependencies..."; yield return null; }
      }
    }
  }

  public const string DATA_PATH = "Migrated/Json/Data/", VAR_PATH = "Migrated/Json/Var/";
  IEnumerator Initialize() {
    LoadingImage.sprite = Loading;
    m_lastFrameTime = Time.time;
    //* Load Json Data (Constant)
    //? Mastery Category Data
    yield return LoadConstData(DATA_PATH + "MasteryCategoryData", ConstantData.MasteryCategoryData);
    //? Mastery Data
    yield return LoadConstData(DATA_PATH + "MasteryData", ConstantData.MasteryData);
    yield return SetDependencies();

    //* Load Json Info (Variable)
    //? Masteries
    yield return LoadVarData(VAR_PATH + "Mastery", VariableData.Mastery);
    //? Mastery Categories
    yield return LoadVarData(VAR_PATH + "MasteryCategory", VariableData.MasteryCategory);

    //* Set Missing(Initial) Data
    foreach (var c in ConstantData.MasteryCategoryData) if (!VariableData.MasteryCategory.ContainsKey(c.Key))
      VariableData.MasteryCategory.Add(c.Key, new MasteryCategory(c.Key, 0));
    foreach (var m in ConstantData.MasteryData) if (!VariableData.Mastery.ContainsKey(m.Key))
      VariableData.Mastery.Add(m.Key, new Mastery(m.Key, 0));

    //! DEBUG
    // foreach (var m in VariableData.MasteryCategory.Values) Debug.Log($"{m.Info.Name} Lv.{m.Level} ({m.Ratio*100:F1}%)");
    // foreach (var m in VariableData.Mastery.Values) Debug.Log($"{m.Info.Name} (Lv.{m.Level})\n{m.Description}");

    if (jsonProblem) {
      LoadingText.text = "Json Error Occured. Please check the log.";
      LoadingImage.sprite = Error;
      yield break;
    }

    //? Load Main Menu
    LoadingText.text = "Load Completed. Starting soon...";
    while (group.alpha > 0) {
      group.alpha -= Time.deltaTime * 1.2f;
      yield return null;
    }

    //! Temp
    SceneManager.LoadScene("MainOld");
  }

  private int GetFirstItemWithinBracket(string text, int startIndex, out string item) {
    if (startIndex < 0 || startIndex > text.Length) { item = ""; return -1; }
    int count = 0, start = text.IndexOf('{', startIndex);
    if (start == -1) { item = ""; return -1; }
    for (int i = start; i < text.Length; i++) {
      if      (text[i] == '{') count++;
      else if (text[i] == '}') count--;

      if (count == 0) {
        item = text[start..(i+1-start)];
        return i + 1;
      }
    }
    item = "";
    return -1;
  }
}