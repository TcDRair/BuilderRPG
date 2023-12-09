using UnityEngine;

namespace Rair.Core
{
  public class LoadManager
  {
    private static LoadManager _instance;
    private LoadManager() { }
    public static LoadManager Instance => _instance ??= new();

    public bool LoadWhenGameStarts()
    {
      return true;
    }
    public bool LoadWhenSceneChanges()
    {
      return true;
    }
    public bool LoadInFrontOfScreen()
    {
      return true; 
    }
    public bool LoadBehindScreen()
    {
      return true;
    }
  }
}