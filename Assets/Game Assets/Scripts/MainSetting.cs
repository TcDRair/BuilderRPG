using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MainSetting
{
	public static readonly int buildLayer = LayerMask.NameToLayer("Building");
	public static readonly int buildMask = 1 << buildLayer;
	public static readonly int mapLayer = LayerMask.NameToLayer("Map");
	public static readonly int mapMask = 1 << mapLayer;
	public static readonly int floorLayer = LayerMask.NameToLayer("Map Floor");
	public static readonly int floorMask = 1 << floorLayer;

	public static readonly int interactableMask = buildMask; //TODO | natureMask | creatureMask;

	#region Map Color
	public static readonly Color32 floorColor = new(0xFF, 0xFF, 0xFF, 0xFF),
		ceilingColor = new(0x80, 0x80, 0x80, 0xFF),
		wallNColor = new(0xFF, 0x00, 0x00, 0xFF),
		wallEColor = new(0xFF, 0x00, 0x20, 0xFF),
		wallSColor = new(0xFF, 0x00, 0x40, 0xFF),
		wallWColor = new(0xFF, 0x00, 0x60, 0xFF),
		cornerNEColor = new(0x00, 0xFF, 0x00, 0xFF),
		cornerSEColor = new(0x00, 0xFF, 0x20, 0xFF),
		cornerSWColor = new(0x00, 0xFF, 0x40, 0xFF),
		cornerNWColor = new(0x00, 0xFF, 0x60, 0xFF),
		diagonalColor = new(0xFF, 0x00, 0xFF, 0xFF),
		diagonalRColor = new(0xFF, 0x80, 0xFF, 0xFF),
		edgeNEColor = new(0x00, 0x00, 0xFF, 0xFF),
		edgeSEColor = new(0x00, 0x20, 0xFF, 0xFF),
		edgeSWColor = new(0x00, 0x40, 0xFF, 0xFF),
		edgeNWColor = new(0x00, 0x60, 0xFF, 0xFF),
		emptyColor = new(0x00, 0x00, 0x00, 0x00),
		emptyColor2 = new(0x00, 0x00, 0x00, 0xFF)
	;
  #endregion

  #region Text Color
	public static readonly string TextColor_Interested = "#bbbbbb",
		TextColor_Ignored = "#808080";
  #endregion
}

public static class TransformMethods
{
	/// <summary>해당 트랜스폼의 모든 자식 오브젝트를 삭제합니다.</summary>
	public static void RemoveAllChildren(this Transform parent)
	{
		if (Application.isPlaying) for (int i = parent.childCount - 1; i >= 0; i--)
			{
				Object.Destroy(parent.GetChild(i).gameObject);
			}
		else for (int i = parent.childCount; i > 0; i--)
			{
				Object.DestroyImmediate(parent.GetChild(0).gameObject);
			}
	}
}

public static class GameObjectMethods
{
	/// <summary>게임오브젝트의 <see cref="UnityEngine.HideFlags"/>나 <see cref="GameObject.active"/>와 무관하게 디폴트 상태로 인스턴스화하여 반환합니다.</summary>
	public static GameObject InstantiateDefault(this GameObject prefab)
	{
		GameObject newObj = Object.Instantiate(prefab);
		newObj.hideFlags = HideFlags.None;
		newObj.SetActive(true);
		return newObj;
	}
	/// <summary>게임오브젝트의 <see cref="UnityEngine.HideFlags"/>나 <see cref="GameObject.active"/>와 무관하게 디폴트 상태로 인스턴스화하여 반환합니다.</summary>
	public static GameObject InstantiateDefault(this GameObject prefab, Transform parent)
	{
		GameObject newObj = Object.Instantiate(prefab, parent);
		newObj.hideFlags = HideFlags.None;
		newObj.SetActive(true);
		return newObj;
	}
	/// <summary>
	/// 게임오브젝트를 에디터나 씬에서 보이지 않는 상태로 인스턴스화하여 반환합니다.<br/>
	/// 해당 게임오브젝트는 저장되지 않으며 직접 조작할 수 없습니다.
	/// </summary>
	public static GameObject InstantiateInvisible(this GameObject prefab)
	{
		GameObject newObj = Object.Instantiate(prefab);
		newObj.hideFlags = HideFlags.HideAndDontSave;
		newObj.SetActive(false);
		return newObj;
	}
}

public static class Vector3QuaternionMethods
{
	public static float GetHorizontalMagnitude(this Vector3 velocity)
	{
		velocity.y = 0;
		return velocity.magnitude;
	}

	public static float GetAngularSpeed(this Quaternion currentRotation, Quaternion previousRotation)
	{
		Vector3 currRV = currentRotation.eulerAngles;
		Vector3 prevRV = previousRotation.eulerAngles;
		return (currRV.y - prevRV.y) % 360;
	}
}