using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIMainHome : MonoBehaviour
{
  public GameObject GOMainHome;
  public GameObject GOQuestHome;

  // Start is called before the first frame update
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {

  }

  public void OnButtonQuestClicked()
  {
    GOMainHome.SetActive(false);
    GOQuestHome.SetActive(true);
  }

  //! Temp
  public void OnRequestClicked() => SceneManager.LoadScene("Request(Temp)");
  public void OnCraftClicked() => SceneManager.LoadScene("Craft(Temp)");
  public void OnMasteryClicked() => SceneManager.LoadScene("Mastery(Temp)");
}
