using System;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using Newtonsoft.Json;

using static MainSetting;

/// <summary>
/// 이미지로부터 맵을 생성하거나 맵을 이미지로 저장합니다.<br/>
/// 해당 맵에 건물을 건설하는 시스템도 여기서 담당합니다.
/// </summary>
public class MapGenerator : MonoBehaviour {
	static MapGenerator _inst;
	public static MapGenerator Instance {
		get {
			if (_inst == null) {
				_inst = FindObjectOfType<MapGenerator>();
				if (_inst == null) {
					GameObject go = new GameObject("MapGenerator");
					_inst = go.AddComponent<MapGenerator>();
				}
			}
			return _inst;
		}
		set { _inst = value; }
	}
	private MapGenerator() {}

	#region variables
	#region inspector / constants
	[Tooltip("맵 생성 시 부모 트랜스폼을 지정합니다.")]
    public Transform mapParent;
	[Tooltip("건물 생성 시 부모 트랜스폼을 지정합니다.")]
	public Transform buildingParent;
	[Tooltip("맵 생성 및 저장 대상 이미지 파일을 지정합니다.")]
    public Texture2D mapTexture;
	#if UNITY_EDITOR
	[SerializeField, Tooltip("지정된 파일과 무관하게 현재 생성된 맵의 정보를 나타냅니다."), ReadOnly]
	#endif
	Texture2D currentTexture;
	[Tooltip("오버레이 시 건설 가능성에 따라 바닥 타일을 덮는 데에 사용되는 프리팹을 할당합니다.")]
	public GameObject overlayAcceptPrefab, overlayDenyPrefab, overlayAdjacentPrefab;
	[Tooltip("오버레이 시 건설 가능성에 따라 오버레이에 적용되는 머티리얼을 할당합니다.")]
	public Material overlayAcceptMat, overlayDenyMat;
	[Tooltip("오버레이를 생성할 때 이를 저장해두는 부모 트랜스폼을 할당합니다.")]
	public Transform overlayParent;
	public const float TileDimension = 4f;
	#endregion

	#region prefabObjects
	[SerializeField]
    GameObject floor, ceiling, wallN, cornerWallNE, diagonal, edgeWallNE;
	GameObject _wE, _wS, _wW;
	GameObject wallE {
		get {
			if (_wE == null) {
				_wE = wallN.InstantiateInvisible();
				_wE.transform.Rotate(0, 90, 0);
			}
			return _wE;
		}
	}
	GameObject wallS {
		get {
			if (_wS == null) {
				_wS = wallN.InstantiateInvisible();
				_wS.transform.Rotate(0, 180, 0);
			}
			return _wS;
		}
	}
	GameObject wallW {
		get {
			if (_wW == null) {
				_wW = wallN.InstantiateInvisible();
				_wW.transform.Rotate(0, 270, 0);
			}
			return _wW;
		}
	}
	GameObject _cSE, _cSW, _cNW;
	GameObject cornerWallSE {
		get {
			if (_cSE == null) {
				_cSE = cornerWallNE.InstantiateInvisible();
				_cSE.transform.Rotate(0, 90, 0);
			}
			return _cSE;
		}
	}
	GameObject cornerWallSW {
		get {
			if (_cSW == null) {
				_cSW = cornerWallNE.InstantiateInvisible();
				_cSW.transform.Rotate(0, 180, 0);
			}
			return _cSW;
		}
	}
	GameObject cornerWallNW {
		get {
			if (_cNW == null) {
				_cNW = cornerWallNE.InstantiateInvisible();
				_cNW.transform.Rotate(0, 270, 0);
			}
			return _cNW;
		}
	}
	GameObject _dR;
	GameObject diagonalR {
		get {
			if (_dR == null) {
				_dR = diagonal.InstantiateInvisible();
				_dR.transform.Rotate(0, 90, 0);
			}
			return _dR;
		}
	}
	GameObject _eSE, _eSW, _eNW;
	GameObject edgeWallSE {
		get {
			if (_eSE == null) {
				_eSE = edgeWallNE.InstantiateInvisible();
				_eSE.transform.Rotate(0, 90, 0);
			}
			return _eSE;
		}
	}
	GameObject edgeWallSW {
		get {
			if (_eSW == null) {
				_eSW = edgeWallNE.InstantiateInvisible();
				_eSW.transform.Rotate(0, 180, 0);
			}
			return _eSW;
		}
	}
	GameObject edgeWallNW {
		get {
			if (_eNW == null) {
				_eNW = edgeWallNE.InstantiateInvisible();
				_eNW.transform.Rotate(0, 270, 0);
			}
			return _eNW;
		}
	}
	#endregion

	#region Map var
	public static int width, length; // initialized with minimum value
	public bool mapCreated;
	public void SetVariables(Texture2D map) {
		if (map == null) {
			width = mapTexture.width;
			length = mapTexture.height;
			currentTexture = mapTexture;
		}
		else {
			width = map.width;
			length = map.height;
			currentTexture = map;
		}
		// Debug.Log($"width and height have been set to {width} and {height}.");
	}
	#endregion

	#region Build var
    Ray ray;
    RaycastHit hitData;
	public const float maxDistance = 100;
	[HideInInspector]
	public Building currentBuilding;
	#if UNITY_EDITOR
	[ReadOnly]
	#endif
	public Building[] buildArray;
	GameObject[] prefabs;
	private int buildArrayIndex = 0;
	public Buildable[,] MapBuildable; // see Building.cs for detail.
	#endregion
	#endregion

	void Awake() { Instance = this; }

	void Start() {
		// 존재하는 모든 구조물 개체 확인
		prefabs = Resources.LoadAll<GameObject>("Prefabs/Building");
		buildArray = new Building[prefabs.Length];
		// 구조물 배열 저장
		for (int i=0; i<prefabs.Length; i++) {
			buildArray[i] = prefabs[i].GetComponent<IBuildingObject>().bldg;
		}
		currentBuilding = buildArray[0];
		BuildSelector_Content.Instance.InitBuildSelectorContents(buildArray);
		// 터레인 생성 (없을 경우)
		if (!mapCreated) {
			DestroyMap(); // 혹시라도 남아있는 것을 제거
			GenerateMap();
		}
		SetVariables(currentTexture);
		InitBuildable();
	}

	void LateUpdate() {
		// UI상의 조작으로 건설 프리뷰 모드에 돌입. 해당 모드에서 수행할 동작을 정의합니다.
		if (State.current.main == State.Main.Menu_BuildPreview) {
			// 현재 프레임에 [선택된 셀]에 [선택된 구조물]을 설치할 수 있는지 판단하고 그에 따른 오버레이를 생성합니다.
			Vector2Int? _cell = GetCurrentCell();
			if (_cell == null) {
				if (Input.GetKeyDown(KeyCode.F12)) Debug.Log($"구조물 셀 정보 : {JsonConvert.SerializeObject(buildArray[buildArrayIndex].info.grid.grid)}\n현재 선택된 건설 위치 없음. 마우스가 범위 밖을 가리킵니다.");
			}
			else {
				Vector2Int cell = (Vector2Int)_cell;
				if (Input.GetKeyDown(KeyCode.F12)) Debug.Log($"구조물 셀 정보 : {JsonConvert.SerializeObject(buildArray[buildArrayIndex].info.grid.grid)}\n건설 위치 : ({cell}) / 맵 크기 : ({width}, {length}) / 선택된 셀 정보 : ({/*MapBuildable[cell.x, cell.y]*/null})");
				BuildableInfo info = BuildOverlay(currentBuilding, cell);
				if (Input.GetKeyDown(KeyCode.Mouse0)) {
					UI.Instance.ShowBuildMessage(info);
					if (info == BuildableInfo.OK) {
						Build(cell);
						State.current.Set(State.Main.Idle);
					}
				}
	
				if (Input.GetKeyDown(KeyCode.Escape)) {
					State.current.Set(State.Main.Idle);
				}
			}
		}
		else if (overlayParent.childCount != 0) overlayParent.RemoveAllChildren();
	}

	public void InitBuildable()
	{
		MapBuildable = new Buildable[width, length];
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < length; j++)
			{
				Color pixelColor = currentTexture.GetPixel(i, j);
				// floor
				if (pixelColor.Equals(floorColor)) MapBuildable[i, j] = Buildable.Floor;
				else MapBuildable[i, j] = Buildable.Unbuildable;
			}
		}
	}

	/// <summary>
	/// 현재 프레임에서 커서가 위치한 셀 인덱스를 반환합니다.<br/>
	/// 어떤 셀도 가리키고 있지 않을 경우 null을 반환합니다.
	/// </summary>
	public Vector2Int? GetCurrentCell()
    {
        if (Physics.Raycast(MainCamera.ray, out hitData, maxDistance, floorMask)) {
			Transform cell = hitData.transform;
			// 가장 상위 바닥 오브젝트를 셀로 간주합니다.
			while (cell.parent.gameObject.layer == floorLayer) cell = cell.parent;
			return new Vector2Int(
        		(int)((cell.position.x - transform.position.x) / TileDimension),
        		(int)((cell.position.z - transform.position.z) / TileDimension)
			);
		}
        else return null;
    }
	/// <summary>현재 프레임에서 주어진 벡터가 위치한 셀 인덱스를 반환합니다.</summary>
	public Vector2Int GetCell(Vector3 pos)
	{
		return new Vector2Int(
			(int)((pos.x - transform.position.x + TileDimension/2f) / TileDimension),
			(int)((pos.z - transform.position.z + TileDimension/2f) / TileDimension)
		);
	}
	public void DestroyMap() { mapParent.RemoveAllChildren(); mapCreated = false; }

	#region Texture <-> Map
	/// <summary><see cref="currentTexture"/>로부터 맵 지형을 생성합니다.</summary>
	public void GenerateMap(Texture2D map = null) {
		if (mapCreated) DestroyMap();
		float multiplierFactor = TileDimension + float.Epsilon;
		SetVariables(map);
		Color32[] pixels = currentTexture.GetPixels32();
        for (int i = 0; i < length; i++) {
            for (int j = 0; j < width; j++) {
                Color32 pixelColor = pixels[i * length + j]; //Each color prefab is assigned as follows:
				GameObject tile;
				// floor
				if (pixelColor.Equals(floorColor)) { tile = GameObject.Instantiate(floor, mapParent); tile.name = nameof(floor); }
				// ceiling
				else if (pixelColor.Equals(ceilingColor)) { tile = GameObject.Instantiate(ceiling, mapParent); tile.name = nameof(ceiling); }
				// wall
				else if (pixelColor.Equals(wallNColor)) { tile = wallN.InstantiateDefault(mapParent); tile.name = nameof(wallN); }
				else if (pixelColor.Equals(wallEColor)) { tile = wallE.InstantiateDefault(mapParent); tile.name = nameof(wallE); }
				else if (pixelColor.Equals(wallSColor)) { tile = wallS.InstantiateDefault(mapParent); tile.name = nameof(wallS); }
				else if (pixelColor.Equals(wallWColor)) { tile = wallW.InstantiateDefault(mapParent); tile.name = nameof(wallW); }
				// corner = Rectangular L Curve
				else if (pixelColor.Equals(cornerNEColor)) { tile = cornerWallNE.InstantiateDefault(mapParent); tile.name = nameof(cornerWallNE); }
				else if (pixelColor.Equals(cornerSEColor)) { tile = cornerWallSE.InstantiateDefault(mapParent) ; tile.name = nameof(cornerWallSE); }
				else if (pixelColor.Equals(cornerSWColor)) { tile = cornerWallSW.InstantiateDefault(mapParent) ; tile.name = nameof(cornerWallSW); }
				else if (pixelColor.Equals(cornerNWColor)) { tile = cornerWallNW.InstantiateDefault(mapParent) ; tile.name = nameof(cornerWallNW); }
				// diagonal
				else if (pixelColor.Equals(diagonalColor)) { tile = diagonal.InstantiateDefault(mapParent); tile.name = nameof(diagonal); }
				else if (pixelColor.Equals(diagonalReverseColor)) { tile = diagonalR.InstantiateDefault(mapParent); tile.name = nameof(diagonalR); }
				// edge
				else if (pixelColor.Equals(edgeNEColor)) { tile = edgeWallNE.InstantiateDefault(mapParent); tile.name = nameof(edgeWallNE); }
				else if (pixelColor.Equals(edgeSEColor)) { tile = edgeWallSE.InstantiateDefault(mapParent); tile.name = nameof(edgeWallSE); }
				else if (pixelColor.Equals(edgeSWColor)) { tile = edgeWallSW.InstantiateDefault(mapParent); tile.name = nameof(edgeWallSW); }
				else if (pixelColor.Equals(edgeNWColor)) { tile = edgeWallNW.InstantiateDefault(mapParent); tile.name = nameof(edgeWallNW); }
				// default = empty (emptyColor exists, but will not be used)
				else if (pixelColor.Equals(emptyColor)) { tile = new GameObject("void"); tile.transform.parent = mapParent; }
				else if (pixelColor.Equals(emptyColor2)) { tile = new GameObject("void"); tile.transform.parent = mapParent; }
				// Error Occurs
				else { tile = null; Debug.LogError("Error Occurs: " + pixelColor); }
				tile.transform.localPosition = new Vector3(j * multiplierFactor, 0, i * multiplierFactor);
            }
        }
		mapCreated = true;
    }
	#if UNITY_EDITOR
	public void SaveMap() {
		SetVariables(currentTexture);
		if (mapParent.childCount != width * length) {
			Debug.Log("Map 타일 개수가 설정된 변수와 다릅니다. Map 생성 매커니즘을 확인하세요.");
			return; 
		}
		currentTexture.Reinitialize(width, length);
		Color32[] pixels = new Color32[width * length];
		for (int i = 0; i < mapParent.childCount; i++) {
			Transform tile = mapParent.GetChild(i);
			switch (tile.name) {
				case nameof(floor): pixels[i] = floorColor; break;
				case nameof(ceiling): pixels[i] = ceilingColor; break;
				case nameof(wallN): pixels[i] = wallNColor; break;
				case nameof(wallE): pixels[i] = wallEColor; break;
				case nameof(wallS): pixels[i] = wallSColor; break;
				case nameof(wallW): pixels[i] = wallWColor; break;
				case nameof(cornerWallNE): pixels[i] = cornerNEColor; break;
				case nameof(cornerWallSE): pixels[i] = cornerSEColor; break;
				case nameof(cornerWallSW): pixels[i] = cornerSWColor; break;
				case nameof(cornerWallNW): pixels[i] = cornerNWColor; break;
				case nameof(diagonal): pixels[i] = diagonalColor; break;
				case nameof(diagonalR): pixels[i] = diagonalReverseColor; break;
				case nameof(edgeWallNE): pixels[i] = edgeNEColor; break;
				case nameof(edgeWallSE): pixels[i] = edgeSEColor; break;
				case nameof(edgeWallSW): pixels[i] = edgeSWColor; break;
				case nameof(edgeWallNW): pixels[i] = edgeNWColor; break;
				default: pixels[i] = emptyColor; break;
			}
		}
		mapTexture.SetPixels32(pixels);
		mapTexture.Apply();

		// save mapTexture;
		// mapTexture 저장
		string assetPath = Application.dataPath.TrimEnd("Assets".ToCharArray()) + AssetDatabase.GetAssetPath(mapTexture);
		File.WriteAllBytes(assetPath, mapTexture.EncodeToPNG());
		// mapTexture = null;
		Debug.Log($"Map Texture 저장 완료. 저장 경로 : {assetPath}");
	}
	#endif
	#endregion

	#region Build + Overlay
	/// <summary>해당 구조물을 해당 셀에 건설할 수 있는지 확인하고 오버레이를 생성합니다.</summary>
	public BuildableInfo BuildOverlay(Building building, Vector2Int cell) {
		//! 임시. overlayChanged 적용 전까지만
		RemoveOverlay();

		// 건설 가능성 판단
		BuildableInfo[,] info = CanBuildOnCell(building, cell);
		BuildableInfo reason = BuildableInfo.OK;
		//* 건설 불가능 이유 우선순위 : 조건 미달 + 재료 부족 + 재화 부족 / 영역 밖 / 플레이어 위치 / 셀 조건 제한
		foreach (BuildableInfo i in info) if (reason < i) reason = i;
		int bW = building.info.width, bL = building.info.length;
		switch (reason) {
			// 영역 밖 : 오버레이 생성 없음
			case BuildableInfo.OutOfBounds: break;
			// 조건 미달 or 재료 부족 or 재화 부족 : 적섹 오버레이 셀 생성, 건물 오버레이 없음
			case BuildableInfo.NotQualified: 
		    case BuildableInfo.NotEnoughMaterial: 
		    case BuildableInfo.NotEnoughMoney: {
				for (int x=-1; x<=bW; x++) for (int y=-1; y<=bL; y++) {
					if (building.info.grid[x, y] == Buildable.None) continue;
					Vector3 pos = new Vector3((cell.x + x) * TileDimension, 0.01f, (cell.y + y) * TileDimension);
					Instantiate(overlayDenyPrefab, overlayParent).transform.localPosition = pos;
				}
				break;
			}
			// 플레이어 중첩 or 셀 조건 미달 or 이상 없음 : 문제가 생긴 셀과 건물은 적색, 나머지 셀은 일반(청색/황색) 오버레이 생성
			case BuildableInfo.PlayerOverlapped:
			case BuildableInfo.Unbuildable:
			case BuildableInfo.OK: {
				for (int x=-1; x<=bW; x++) for (int y=-1; y<=bL; y++) {
					if (building.info.grid[x, y] == Buildable.None) continue;
					Vector3 pos = new Vector3((cell.x + x) * TileDimension, 0.01f, (cell.y + y) * TileDimension);
					if (info[x+1, y+1] > BuildableInfo.OK) Instantiate(overlayDenyPrefab, overlayParent).transform.position = pos;
					else if (x == -1 || x == bW || y == -1 || y == bL) Instantiate(overlayAdjacentPrefab, overlayParent).transform.localPosition = pos;
					else Instantiate(overlayAcceptPrefab, overlayParent).transform.localPosition = pos;
				}
				Vector3 buildingPos = new Vector3(cell.x * TileDimension, 0, cell.y * TileDimension);
				GameObject previewModel = Instantiate(building.info.preview, overlayParent);
				previewModel.transform.localPosition = buildingPos;
				previewModel.GetComponent<Renderer>().material = (reason > BuildableInfo.OK) ? overlayDenyMat : overlayAcceptMat;
				break;
			}
			default : {
				Debug.Log($"예상치 못한 조건({reason})이 도출되었습니다. 검증 코드를 다시 확인하세요.");
				break;
			}
		}
		return reason;
	}

	public void RemoveOverlay() { overlayParent.RemoveAllChildren(); }

	/// <summary>
	/// 해당 구조물을 해당 셀에 건축할 수 있는지 판단하고, 건설 가능성을 <see cref="BuildableInfo"/> 으로 반환합니다.
	/// </summary>
	BuildableInfo[,] CanBuildOnCell(Building building, Vector2Int cell) {
		BuildableInfo[,] cellInfo = new BuildableInfo[building.info.width+2, building.info.length+2]; // None으로 초기화.
		//* 구조물의 모든 셀에 대해 다음의 조건을 판단합니다 :
		for (int i = -1; i <= building.info.width; i++) {
			for (int j = -1; j <= building.info.length; j++) {
				Vector2Int pos = new Vector2Int(cell.x + i, cell.y + j);
				// 건설 가능 영역(Map)을 벗어남
				if      (pos.x >= width || pos.x < 0 || pos.y >= length || pos.y < 0) {
					cellInfo[i+1, j+1] = BuildableInfo.OutOfBounds;
					continue; // 이하 조건을 검증할 수 없으므로 건너뜀
				}
				// 해당 셀에 건설할 수 없음
				else if (!CanAddCell(pos, building.info.grid[i, j])) cellInfo[i+1, j+1] = BuildableInfo.Unbuildable;
				//* 인접 셀을 제외하고 플레이어와 겹침
				if ((i > -1 && i < building.info.width && j > -1 && j < building.info.length) && IsPlayerInside(pos)) cellInfo[i+1, j+1] = BuildableInfo.PlayerOverlapped;
			}
		}
		//* 계산된 건설 가능성 정보를 반환합니다.
		return cellInfo;
	}

	/// <summary>건설 가능성과 그 이유를 담은 열거형입니다. 우선순위 오름차순으로 정의됩니다.</summary>
	public enum BuildableInfo {
		/// <summary>검증 플래그</summary>
		None = 0,
		/// <summary>건설해도 좋음</summary>
		OK,
		/// <summary>플레이어가 겹침</summary>
		PlayerOverlapped,
		/// <summary>해당 셀에 건설할 수 없음</summary>
		Unbuildable,
		/// <summary>건설 재료가 부족함</summary>
		NotEnoughMaterial,
		/// <summary>건설 재화가 부족함</summary>
		NotEnoughMoney,
		/// <summary>건설할 조건을 갖추지 못함. 이는 설계도나 건설 가능 레벨 등의 조건이 될 수 있습니다.</summary>
		NotQualified,
		/// <summary>건설 가능 영역에서 벗어남</summary>
		OutOfBounds,
	}

	/// <summary>
	/// 주어진 X, Y 좌표가 맵의 내부에 존재하면 true, 외부에 존재하면 false를 반환합니다.<br/>
	/// 맵의 외곽선이 빈 공간이어도 true를 반환합니다.
	/// </summary>

	#region Buildable 관련 메서드
	/// <summary>현재 플레이어가 주어진 셀 위에 있으면 <see langword="true"/>를 반환합니다.</summary>
	bool IsPlayerInside(Vector2Int pos) {
		return GetCell(Player.Instance.transform.position) == pos;
	}

	/// <summary>
	/// 해당 위치의 셀에 주어진 플래그를 가진 구조물을 추가할 수 있는지 판단합니다.<br/>
	/// 하나의 셀에 대해서만 판단하므로 여러 셀을 판단할 때는 각 셀에 대해 사용해주세요.
	/// </summary>
	bool CanAddCell(Vector2Int pos, Buildable buildable) {
		//* 0. 빈 플래그는 검사할 필요가 없음
		if (buildable == Buildable.None) return true;

		// 아래 체크 과정에서 사용
		Buildable cell = MapBuildable[pos.x, pos.y], both = cell & buildable, either = cell | buildable;

		//* 1. 건설 불가능한 셀
		if (cell.HasOneFlag(Buildable.Unbuildable)) { return false; }

		//* 2. 구조물이 겹치는 경우
		//  2-1. 벽/천장/내부/바닥/부착물이 겹치는 경우 (사실상 검증 과정)
		if (both.HasOneFlag(Buildable.FullStruct)) { return false; }
		//  2-2. 부착물이 겹치는 경우. 어느 위치에 부착되었든 두 개 이상 존재할 수 없다.
		//? 구조물이나 셀 각각에 여러 부착물이 존재하지 않는다고 가정, (이전의 판단을 신뢰)
		//? 셀과 구조물 둘 다에 부착물이 존재하는지만 체크한다.
		if (cell.HasOneFlag(Buildable.Inside) && buildable.HasOneFlag(Buildable.Inside)) { return false; }

		//* 3. 구조물에 존재하는 부착물이 건설 후 부착될 수 없는 경우. (벽 부착물은 벽에, 천장 구조물은 천장에)
		//? 셀에 부착물이 존재하는 경우 항상 부착 가능할 것이라 간주 (이전의 판단을 신뢰)
		if (buildable.HasOneFlag(Buildable.Attach_C) && !either.HasOneFlag(Buildable.Ceiling)) { return false; }
		if (buildable.HasOneFlag(Buildable.Attach_N) && !either.HasOneFlag(Buildable.Wall_N)) { return false; }
		if (buildable.HasOneFlag(Buildable.Attach_E) && !either.HasOneFlag(Buildable.Wall_E)) { return false; }
		if (buildable.HasOneFlag(Buildable.Attach_S) && !either.HasOneFlag(Buildable.Wall_S)) { return false; }
		if (buildable.HasOneFlag(Buildable.Attach_W) && !either.HasOneFlag(Buildable.Wall_W)) { return false; }
		
		//* 4. 천장이 있는데 벽이 2개 이상 있지 않을 경우.
		if (both.HasOneFlag(Buildable.Ceiling) && !both.HasNFlag(Buildable.Wall, 2)) { return false; }

		//* 5. 가벽과 천장이 존재하는 경우.
		//? 4-1. 이미 천장이나 벽이 존재하는 셀에 가벽 구조물이 들어오면 가벽 플래그 제거 (추가 가능 조건과는 무관)
		//? 4-2. 가벽이 존재하는 셀에 천장 + 벽 구조물이 들어오는 경우 -> 천장과 벽이 있으므로 가벽 플래그 제거 (추가 가능 조건과는 무관)
		//? 4-3. 가벽이 존재하는 셀에 실제 벽 구조물이 들어오는 경우 -> 가벽 플래그 제거 (추가 가능 조건과는 무관)

		//? 4-4. 가벽이 존재하는 셀에 벽 없는 천장 구조물이 들어오는 경우 -> 추가 불가
		if (cell.HasOneFlag(Buildable.IsFalseWall) && buildable.HasOneFlag(Buildable.Ceiling) && !buildable.HasOneFlag(Buildable.Wall)) { return false; }

		//* E. 모든 조건 만족
		return true;
	}
	/// <summary>해당 위치의 셀에 주어진 구조물의 플래그를 추가합니다.<br/>추가 가능 여부는 이미 검사한 것으로 간주합니다.</summary>
	void SetCell(Vector2Int pos, Buildable buildable) {
		// 이전 셀과 이후 셀 플래그 미리 설정
		Buildable cell = MapBuildable[pos.x, pos.y], both = cell & buildable, result = cell | buildable;
		//* 1. 가벽 플래그 처리
		//? 둘 다 가벽 플래그 존재 : 가벽 플래그 유지
		if (both.HasAllFlag(Buildable.IsFalseWall)) {}
		//? 가벽 플래그 존재 + 벽이나 천장 구조물 추가 : 가벽 플래그 제거
		else if (cell.HasAllFlag(Buildable.IsFalseWall) && buildable.HasOneFlag(Buildable.Wall | Buildable.Ceiling)) result ^= Buildable.IsFalseWall;
		//? 벽이나 천장 구조물 존재 + 가벽 플래그 추가 : 가벽 플래그 제거
		else if (cell.HasOneFlag(Buildable.Wall | Buildable.Ceiling) && buildable.HasAllFlag(Buildable.IsFalseWall)) result ^= Buildable.IsFalseWall;

		//* X. 최종 플래그 적용
		MapBuildable[pos.x, pos.y] = result;
	}

	public bool BuildMaterialCheck(Building building) { return true; } // 건축 재료 기능 구현: 미정. << 인벤토리 먼저 구현.

	public bool StaminaCheck(int Stamina) { return true; } // 커스텀 소비재화 구현: 미정. << Player에서 구현.

	#endregion
	
	/// <summary>건물을 해당 위치에 생성하고 맵의 <see cref="Buildable"/> 데이터를 업데이트합니다.</summary>
	public void Build(Vector2Int pos)
	{
		GameObject newBuilding = GameObject.Instantiate<GameObject>(prefabs[buildArrayIndex], buildingParent);
		Building building = newBuilding.GetComponent<IBuildingObject>().bldg;
		BuildingInfo info = building.info;

		float realPosX = (pos.x + (info.width - 1)/2.0f) * TileDimension;
		float realPosY = (pos.y + (info.length - 1)/2.0f) * TileDimension;
		newBuilding.layer = buildLayer;
		newBuilding.transform.localPosition = new Vector3(realPosX, 0, realPosY);
		for (int i=-1; i <= info.width; i++) { for (int j=-1; j <= info.length; j++) {
			MapBuildable[pos.x + i, pos.y + j] |= (info.grid[i,j] | Buildable.UnderConstruction);
		}}
		building.SavePosition(pos);
	}

	/// <summary>맵의 <see cref="Buildable"/> 데이터에서 해당 건축물을 제거합니다.</summary>
	public void CleanBuilding(Building building) {
		if (building.buildProgress != 1f) for (int i=-1; i <= building.info.width; i++) for (int j=-1; j <= building.info.length; j++) {
			MapBuildable[building.pos.x + i, building.pos.y + j] ^= (building.info.grid[i,j] | Buildable.UnderConstruction);
		}
		else for (int i=-1; i <= building.info.width; i++) for (int j=-1; j <= building.info.length; j++) {
			MapBuildable[building.pos.x + i, building.pos.y + j] ^= building.info.grid[i,j];
		}
	}
	#endregion

}