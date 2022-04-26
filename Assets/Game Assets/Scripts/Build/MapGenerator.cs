using System;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

/// <summary>
/// 이미지로부터 맵을 생성하거나 맵을 이미지로 저장합니다.<br/>
/// 해당 맵에 건물을 건설하는 시스템도 여기서 담당합니다.
/// </summary>
public class MapGenerator : MonoBehaviour {

	#region variables
	[Tooltip("맵 생성 시 부모 트랜스폼을 지정합니다.")]
    public Transform mapTransform;
	[Tooltip("건물 생성 시 부모 트랜스폼을 지정합니다.")]
	public Transform buildings;
	[Tooltip("맵 생성 및 저장 대상 이미지 파일을 지정합니다.")]
    public Texture2D mapTexture;
	[Tooltip("오버레이 시 건설 가능성에 따라 바닥 타일을 덮는 데에 사용되는 프리팹을 할당합니다.")]
	public GameObject overlayAllowPrefab, overlayDenyPrefab, overlayAdjacentPrefab;
	[Tooltip("오버레이 시 건설 가능성에 따라 오버레이에 적용되는 머티리얼을 할당합니다.")]
	public Material overlayAcceptMat, overlayDenyMat;
	[Tooltip("오버레이를 생성할 때 이를 저장해두는 부모 트랜스폼을 할당합니다.")]
	public Transform overlayObjects;
	public const float TileDimension = 4f;
	[SerializeField]
	private int buildLayer = 8;

	#region prefabObjects
    public GameObject floor;
    public GameObject ceiling;
    public GameObject wallNorth, wallEast, wallSouth, wallWest;
	public GameObject cornerNorthEast, cornerNorthWest, cornerSouthEast, cornerSouthWest;
	public GameObject diagonal, diagonalReverse;
	public GameObject edgeNorthEast, edgeNorthWest, edgeSouthEast, edgeSouthWest;
	#endregion

	#region Color32
	readonly Color32 floorColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF),
		ceilingColor = new Color32(0x80, 0x80, 0x80, 0xFF),
		wallNColor = new Color32(0xFF, 0x00, 0x00, 0xFF),
		wallEColor = new Color32(0xFF, 0x00, 0x20, 0xFF),
		wallSColor = new Color32(0xFF, 0x00, 0x40, 0xFF),
		wallWColor = new Color32(0xFF, 0x00, 0x60, 0xFF),
		cornerNEColor = new Color32(0x00, 0xFF, 0x00, 0xFF),
		cornerSEColor = new Color32(0x00, 0xFF, 0x20, 0xFF),
		cornerSWColor = new Color32(0x00, 0xFF, 0x40, 0xFF),
		cornerNWColor = new Color32(0x00, 0xFF, 0x60, 0xFF),
		diagonalColor = new Color32(0xFF, 0x00, 0xFF, 0xFF),
		diagonalReverseColor = new Color32(0xFF, 0x80, 0xFF, 0xFF),
		edgeNEColor = new Color32(0x00, 0x00, 0xFF, 0xFF),
		edgeSEColor = new Color32(0x00, 0x20, 0xFF, 0xFF),
		edgeSWColor = new Color32(0x00, 0x40, 0xFF, 0xFF),
		edgeNWColor = new Color32(0x00, 0x60, 0xFF, 0xFF),
		emptyColor = new Color32(0x00, 0x00, 0x00, 0x00);
	#endregion

	#region Map var
	public static int width, length; // initialized with minimum value
	public bool mapCreated;
	public void SetVariables() {
		width = mapTexture.width; // Allowed minimum value: 3
		length = mapTexture.height; // Allowed minimum value: 3
		// Debug.Log($"width and height have been set to {width} and {height}.");
	}
	#endregion

	#region Build var
    Ray ray;
    RaycastHit hitData;
	public static float maxDistance = 100;
	public static Building currentBuilding;
	public static Building[] buildArray;
	static GameObject[] prefabs;
	private static int buildArrayIndex = 0;
	public static Buildable[,] MapBuildable; // see Building.cs for detail.
	#endregion

	#endregion

	void Start() {
		// 존재하는 모든 구조물 개체 확인
		prefabs = Resources.LoadAll<GameObject>("Prefabs/Building");
		buildArray = new Building[prefabs.Length];
		// 구조물 배열 저장 //TODO 다이얼에서 선택으로 변경
		for (int i=0; i<prefabs.Length; i++) {
			buildArray[i] = prefabs[i].GetComponent<IBuildingObject>().obj;
		}
		currentBuilding = buildArray[0];
		BuildSelector_Content.Instance.InitBuildSelectorContents(buildArray);
		// 터레인 생성 (없을 경우)
		if (!mapCreated) {
			DestroyMap(); // 혹시라도 남아있는 것을 제거
			GenerateMap();
		}
		// 있을 경우 변수만 초기화
		else SetVariables();
		InitBuildable();
	}

	void LateUpdate() {
		// 특정 UI 조작으로 건설 모드 돌입.
		if (UI.buildPreview) {
			// 현재 프레임에 [선택된 셀]에 [선택된 구조물]을 설치할 수 있는지 판단하고 그에 따른 오버레이를 생성합니다.
			Vector2Int? _cell = GetCurrentCell();
			if (_cell == null) {
				if (Input.GetKeyDown(KeyCode.F12)) Debug.Log($"구조물 셀 정보 : {buildArray[buildArrayIndex].info.grid}\n현재 선택된 건설 위치 없음. 마우스가 범위 밖을 가리킵니다.");
				return;
			}
			Vector2Int cell = (Vector2Int)_cell;
			if (Input.GetKeyDown(KeyCode.F12)) Debug.Log($"구조물 셀 정보 : {buildArray[buildArrayIndex].info.grid}\n건설 위치 : ({cell}) / 맵 크기 : ({width}, {length}) / 선택된 셀 정보 : ({MapBuildable[cell.x, cell.y]})");
			BuildableInfo info = BuildOverlay(currentBuilding, cell);
			if (Input.GetKeyDown(KeyCode.Mouse0)) {
				UI.ui.ShowBuildMessage(info);
				if (info == BuildableInfo.OK) {
					Build(cell);
					UI.buildPreview = false;
				}
			}

			if (Input.GetKeyDown(KeyCode.Escape)) {
				UI.buildPreview = false;
			}
		}
		else if (overlayObjects.childCount != 0) overlayObjects.RemoveAllChildren();
	}

	public void InitBuildable()
	{
		MapBuildable = new Buildable[width, length];
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < length; j++)
			{
				Color pixelColor = mapTexture.GetPixel(i, j);
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
        if (Physics.Raycast(MainCamera.ray, out hitData, maxDistance, 1 << buildLayer)) {
			return new Vector2Int(
        	(int)(hitData.transform.position.x - transform.position.x)/(int)TileDimension,
        	(int)(hitData.transform.position.z - transform.position.z)/(int)TileDimension
			);
		}
        else return null;
    }
	/// <summary>현재 프레임에서 주어진 벡터가 위치한 셀 인덱스를 반환합니다.</summary>
	public Vector2Int GetCell(Vector3 pos)
	{
		return new Vector2Int(
			(int)(pos.x - transform.position.x + TileDimension/2f)/(int)TileDimension,
			(int)(pos.z - transform.position.z + TileDimension/2f)/(int)TileDimension
		);
	}
	public void DestroyMap() { mapTransform.RemoveAllChildren(); mapCreated = false; }

	#region Texture <-> Map
	/// <summary><see cref="mapTexture"/>로부터 맵 지형을 생성합니다.</summary>
	public void GenerateMap() {
		float multiplierFactor = TileDimension + float.Epsilon;
		SetVariables();
		Color32[] pixels = mapTexture.GetPixels32();
        for (int i = 0; i < length; i++) {
            for (int j = 0; j < width; j++) {
                Color32 pixelColor = pixels[i * length + j]; //Each color prefab is assign as follows:
				GameObject tile;
				// floor
				if (pixelColor.Equals(floorColor)) { tile = GameObject.Instantiate(floor, mapTransform); tile.name = nameof(floor); }
				// ceiling
				else if (pixelColor.Equals(ceilingColor)) { tile = GameObject.Instantiate(ceiling, mapTransform); tile.name = nameof(ceiling); }
				// wall
				else if (pixelColor.Equals(wallNColor)) { tile = GameObject.Instantiate(wallNorth, mapTransform); tile.name = nameof(wallNorth); }
				else if (pixelColor.Equals(wallEColor)) { tile = GameObject.Instantiate(wallEast, mapTransform); tile.name = nameof(wallEast); }
				else if (pixelColor.Equals(wallSColor)) { tile = GameObject.Instantiate(wallSouth, mapTransform); tile.name = nameof(wallSouth); }
				else if (pixelColor.Equals(wallWColor)) { tile = GameObject.Instantiate(wallWest, mapTransform); tile.name = nameof(wallWest); }
				// corner = Rectangular L Curve
				else if (pixelColor.Equals(cornerNEColor)) { tile = GameObject.Instantiate(cornerNorthEast, mapTransform); tile.name = nameof(cornerNorthEast); }
				else if (pixelColor.Equals(cornerSEColor)) { tile = GameObject.Instantiate(cornerSouthEast, mapTransform); tile.name = nameof(cornerSouthEast); }
				else if (pixelColor.Equals(cornerSWColor)) { tile = GameObject.Instantiate(cornerSouthWest, mapTransform); tile.name = nameof(cornerSouthWest); }
				else if (pixelColor.Equals(cornerNWColor)) { tile = GameObject.Instantiate(cornerNorthWest, mapTransform); tile.name = nameof(cornerNorthWest); }
				// diagonal
				else if (pixelColor.Equals(diagonalColor)) { tile = GameObject.Instantiate(diagonal, mapTransform); tile.name = nameof(diagonal); }
				else if (pixelColor.Equals(diagonalReverseColor)) { tile = GameObject.Instantiate(diagonalReverse, mapTransform); tile.name = nameof(diagonalReverse); }
				// edge
				else if (pixelColor.Equals(edgeNEColor)) { tile = GameObject.Instantiate(edgeNorthEast, mapTransform); tile.name = nameof(edgeNorthEast); }
				else if (pixelColor.Equals(edgeSEColor)) { tile = GameObject.Instantiate(edgeSouthEast, mapTransform); tile.name = nameof(edgeSouthEast); }
				else if (pixelColor.Equals(edgeSWColor)) { tile = GameObject.Instantiate(edgeSouthWest, mapTransform); tile.name = nameof(edgeSouthWest); }
				else if (pixelColor.Equals(edgeNWColor)) { tile = GameObject.Instantiate(edgeNorthWest, mapTransform); tile.name = nameof(edgeNorthWest); }
				// default = empty (emptyColor exists, but will not be used)
				else { tile = new GameObject("void"); tile.transform.parent = mapTransform; }
				tile.transform.localPosition = new Vector3(j * multiplierFactor, 0, i * multiplierFactor);
            }
        }
		mapCreated = true;
    }

	public void SaveMap() {
		SetVariables();
		if (mapTransform.childCount != width * length) {
			Debug.Log("Map 타일 개수가 설정된 변수와 다릅니다. Map 생성 매커니즘을 확인하세요.");
			return; 
		}
		mapTexture.Reinitialize(width, length);
		Color32[] pixels = new Color32[width * length];
		for (int i = 0; i < mapTransform.childCount; i++) {
			Transform tile = mapTransform.GetChild(i);
			switch (tile.name) {
				case nameof(floor): pixels[i] = floorColor; break;
				case nameof(ceiling): pixels[i] = ceilingColor; break;
				case nameof(wallNorth): pixels[i] = wallNColor; break;
				case nameof(wallEast): pixels[i] = wallEColor; break;
				case nameof(wallSouth): pixels[i] = wallSColor; break;
				case nameof(wallWest): pixels[i] = wallWColor; break;
				case nameof(cornerNorthEast): pixels[i] = cornerNEColor; break;
				case nameof(cornerSouthEast): pixels[i] = cornerSEColor; break;
				case nameof(cornerSouthWest): pixels[i] = cornerSWColor; break;
				case nameof(cornerNorthWest): pixels[i] = cornerNWColor; break;
				case nameof(diagonal): pixels[i] = diagonalColor; break;
				case nameof(diagonalReverse): pixels[i] = diagonalReverseColor; break;
				case nameof(edgeNorthEast): pixels[i] = edgeNEColor; break;
				case nameof(edgeSouthEast): pixels[i] = edgeSEColor; break;
				case nameof(edgeSouthWest): pixels[i] = edgeSWColor; break;
				case nameof(edgeNorthWest): pixels[i] = edgeNWColor; break;
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
	#endregion

	#region Build Overlay
	/// <summary>해당 구조물을 해당 셀에 건설할 수 있는지 확인하고 오버레이를 생성합니다.</summary>
	public BuildableInfo BuildOverlay(Building building, Vector2Int cell) {
		//! 임시. overlayChanged 적용 전까지만
		overlayObjects.RemoveAllChildren();

		// 건설 가능성 판단
		BuildableInfo[,] info = CanBuildOnCell(building, cell);
		BuildableInfo reason = BuildableInfo.OK;
		// 건설 불가능 이유 우선순위 지정 : 조건 미달 + 재료 부족 + 재화 부족 / 영역 밖 / 플레이어 위치 / 셀 조건 제한
		foreach (BuildableInfo i in info) reason |= i;
		int bW = building.info.width, bL = building.info.length;
		// 조건 미달 or 재료 부족 or 재화 부족 : 적섹 오버레이 셀 생성
		if      (reason.HasFlag(BuildableInfo.NotQualified)) {
			for (int x=-1; x<=bW; x++) for (int y=-1; y<=bL; y++) {
				if (building.info.grid[x, y] == Buildable.None) continue;
				Vector3 pos = new Vector3((cell.x + x) * TileDimension, 0.01f, (cell.y + y) * TileDimension);
				Instantiate(overlayAllowPrefab, overlayObjects).transform.localPosition = pos;
			}
			return BuildableInfo.NotQualified;
		}
		else if (reason.HasFlag(BuildableInfo.NotEnoughMaterial)) {
			for (int x=-1; x<=bW; x++) for (int y=-1; y<=bL; y++) {
				if (building.info.grid[x, y] == Buildable.None) continue;
				Vector3 pos = new Vector3((cell.x + x) * TileDimension, 0.01f, (cell.y + y) * TileDimension);
				Instantiate(overlayAllowPrefab, overlayObjects).transform.localPosition = pos;
			}
			return BuildableInfo.NotEnoughMaterial;
		}
		else if (reason.HasFlag(BuildableInfo.NotEnoughMoney)) {
			for (int x=-1; x<=bW; x++) for (int y=-1; y<=bL; y++) {
				if (building.info.grid[x, y] == Buildable.None) continue;
				Vector3 pos = new Vector3((cell.x + x) * TileDimension, 0.01f, (cell.y + y) * TileDimension);
				Instantiate(overlayAllowPrefab, overlayObjects).transform.localPosition = pos;
			}
			return BuildableInfo.NotEnoughMoney;
		}
		// 영역 밖 : 오버레이 생성 없음
		else if (reason.HasFlag(BuildableInfo.OutOfBounds)) return BuildableInfo.OutOfBounds;
		// 플레이어 중첩 or 셀 조건 미달 : 해당 셀 및 프리뷰 모델 적색 오버레이 생성, 나머지 셀 청색 오버레이 생성
		else if (reason.HasFlag(BuildableInfo.PlayerOverlapped)) {
			for (int x=-1; x<=bW; x++) for (int y=-1; y<=bL; y++) {
				if (building.info.grid[x, y] == Buildable.None) continue;
				Vector3 pos = new Vector3((cell.x + x) * TileDimension, 0.01f, (cell.y + y) * TileDimension);
				if (info[x+1, y+1] != BuildableInfo.OK) Instantiate(overlayDenyPrefab, overlayObjects).transform.position = pos;
				else if (x == -1 || x == bW || y == -1 || y == bL) Instantiate(overlayAdjacentPrefab, overlayObjects).transform.localPosition = pos;
				else Instantiate(overlayAllowPrefab, overlayObjects).transform.localPosition = pos;
			}
			Vector3 buildingPos = new Vector3(cell.x * TileDimension, 0, cell.y * TileDimension);
			GameObject previewModel = Instantiate(building.info.preview, overlayObjects);
			previewModel.transform.localPosition = buildingPos;
			previewModel.GetComponent<Renderer>().material = overlayDenyMat;
			return BuildableInfo.PlayerOverlapped;
		}
		else if (reason.HasFlag(BuildableInfo.Unbuildable)) {
			for (int x=-1; x<=bW; x++) for (int y=-1; y<=bL; y++) {
				if (building.info.grid[x, y] == Buildable.None) continue;
				Vector3 pos = new Vector3((cell.x + x) * TileDimension, 0.01f, (cell.y + y) * TileDimension);
				if (info[x+1, y+1] != BuildableInfo.OK) Instantiate(overlayDenyPrefab, overlayObjects).transform.position = pos;
				else if (x == -1 || x == bW || y == -1 || y == bL) Instantiate(overlayAdjacentPrefab, overlayObjects).transform.localPosition = pos;
				else Instantiate(overlayAllowPrefab, overlayObjects).transform.localPosition = pos;
			}
			Vector3 buildingPos = new Vector3(cell.x * TileDimension, 0, cell.y * TileDimension);
			GameObject previewModel = Instantiate(building.info.preview, overlayObjects);
			previewModel.transform.localPosition = buildingPos;
			previewModel.GetComponent<Renderer>().material = overlayDenyMat;
			return BuildableInfo.Unbuildable;
		}
		else {
			for (int x=-1; x<=bW; x++) for (int y=-1; y<=bL; y++) {
				if (building.info.grid[x, y] == Buildable.None) continue;
				Vector3 pos = new Vector3((cell.x + x) * TileDimension, 0.01f, (cell.y + y) * TileDimension);
				if (x == -1 || x == bW || y == -1 || y == bL) Instantiate(overlayAdjacentPrefab, overlayObjects).transform.localPosition = pos;
				else Instantiate(overlayAllowPrefab, overlayObjects).transform.localPosition = pos;
			}
			Vector3 buildingPos = new Vector3(cell.x * TileDimension, 0, cell.y * TileDimension);
			GameObject previewModel = Instantiate(building.info.preview, overlayObjects);
			previewModel.transform.localPosition = buildingPos;
			previewModel.GetComponent<Renderer>().material = overlayAcceptMat;
			return BuildableInfo.OK;
		}
	}

	public void RemoveOverlay() { overlayObjects.RemoveAllChildren(); }

	/// <summary>
	/// 해당 구조물을 해당 셀에 건축할 수 있는지 판단하고, 건설 가능성을 <see cref="BuildableInfo"/> 으로 반환합니다.
	/// </summary>
	BuildableInfo[,] CanBuildOnCell(Building building, Vector2Int cell) {
		BuildableInfo[,] cellInfo = new BuildableInfo[building.info.width+2, building.info.length+2]; // OK로 초기화됨
		//* 구조물의 모든 셀에 대해 다음의 조건을 판단합니다 :
		for (int i = 0; i < building.info.width; i++) {
			for (int j = 0; j < building.info.length; j++) {
				Vector2Int pos = new Vector2Int(cell.x + i, cell.y + j);
				// 건설 가능 영역(Map)을 벗어남
				if (pos.x >= width || pos.x < 0 || pos.y >= length || pos.y < 0) cellInfo[i+1, j+1] = BuildableInfo.OutOfBounds;
				// 플레이어와 겹침
				if (IsPlayerInside(pos)) cellInfo[i+1, j+1] = BuildableInfo.PlayerOverlapped;
			}
		}
		//* 모든 구조물 셀 및 그 인접 셀에 대해 다음의 조건을 판단합니다 :
		for (int i = -1; i <= building.info.width; i++) {
			for (int j = -1; j <= building.info.length; j++) {
				if (!CanAddCell(new Vector2Int(cell.x+i, cell.y+j), building.info.grid[i, j])) cellInfo[i+1, j+1] = BuildableInfo.Unbuildable;
			}
		}
		// 모든 셀이 조건을 만족하므로 건설이 가능합니다.
		return cellInfo;
	}

	[Flags]
	/// <summary>건설 가능성과 그 이유를 담은 열거형입니다.</summary>
	public enum BuildableInfo {
		/// <summary>검증 플래그</summary>
		None = 0,
		/// <summary>건설해도 좋음</summary>
		OK = 1 << 0,
		/// <summary>플레이어가 겹침</summary>
		PlayerOverlapped = 1 << 1,
		/// <summary>건설 가능 영역에서 벗어남</summary>
		OutOfBounds = 1 << 2,
		/// <summary>해당 셀에 건설할 수 없음</summary>
		Unbuildable = 1 << 3,
		/// <summary>건설 재료가 부족함</summary>
		NotEnoughMaterial = 1 << 4,
		/// <summary>건설 재화가 부족함</summary>
		NotEnoughMoney = 1 << 5,
		/// <summary>건설할 조건을 갖추지 못함. 이는 설계도나 건설 가능 레벨 등의 조건이 될 수 있습니다.</summary>
		NotQualified = 1 << 6,
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
		// 자주 쓴다 싶어서 저장.
		Buildable cell = MapBuildable[pos.x, pos.y], both = cell & buildable, either = cell | buildable;
		//* 0. 빈 플래그는 검사할 필요가 없음
		if (buildable == Buildable.None) return true;

		//* 1. 건설 불가능한 셀
		if (cell.HasAllFlag(Buildable.Unbuildable)) { return false; }

		//* 2. 구조물이 겹치는 경우
		//  2-1. 벽/천장/내부/바닥/부착물이 겹치는 경우 (사실상 검증 과정)
		if (both.HasOneFlag(Buildable.FullStruct)) { return false; }
		//  2-2. 부착물이 겹치는 경우. 어느 위치에 부착되었든 두 개 이상 존재할 수 없다.
		//? 구조물이나 셀 각각에 여러 부착물이 존재하지 않는다고 가정, (이전의 판단을 신뢰)
		//? 셀과 구조물 둘 다에 부착물이 존재하는지만 체크한다.
		if (cell.HasOneFlag(Buildable.Inside) && buildable.HasOneFlag(Buildable.Inside)) { return false; }

		//* 3. 구조물에 존재하는 부착물이 건설 후 부착될 수 없는 경우. (벽 부착물은 벽에, 천장 구조물은 천장에)
		//? 셀에 부착물이 존재하는 경우는 고려하지 않음 (이전의 판단을 신뢰)
		if (buildable.HasAllFlag(Buildable.Attach_C) && !either.HasAllFlag(Buildable.Ceiling)) { return false; }
		if (buildable.HasAllFlag(Buildable.Attach_N) && !either.HasAllFlag(Buildable.Wall_N)) { return false; }
		if (buildable.HasAllFlag(Buildable.Attach_E) && !either.HasAllFlag(Buildable.Wall_E)) { return false; }
		if (buildable.HasAllFlag(Buildable.Attach_S) && !either.HasAllFlag(Buildable.Wall_S)) { return false; }
		if (buildable.HasAllFlag(Buildable.Attach_W) && !either.HasAllFlag(Buildable.Wall_W)) { return false; }
		
		//* 4. 천장이 있는데 벽이 없는 경우
		if (both.HasAllFlag(Buildable.Ceiling) && (both & Buildable.Wall) == Buildable.None) { return false; }

		//* 5. 가벽과 천장이 존재하는 경우.
		//? 4-1. 이미 천장이나 벽이 존재하는 셀에 가벽 구조물이 들어오면 가벽 플래그 제거 (추가 가능 조건과는 무관)
		//? 4-2. 가벽이 존재하는 셀에 천장 + 벽 구조물이 들어오는 경우 -> 천장과 벽이 있으므로 가벽 플래그 제거 (추가 가능 조건과는 무관)
		//? 4-3. 가벽이 존재하는 셀에 실제 벽 구조물이 들어오는 경우 -> 가벽 플래그 제거 (추가 가능 조건과는 무관)
		//? 4-4. 가벽이 존재하는 셀에 벽 없는 천장 구조물이 들어오는 경우 -> 추가 불가
		if (cell.HasAllFlag(Buildable.IsFalseWall) && buildable.HasAllFlag(Buildable.Ceiling) && !buildable.HasAllFlag(Buildable.Wall)) { return false; }

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
	}

	public bool BuildMaterialCheck(Building building) { return true; } // 건축 재료 기능 구현: 미정. << 인벤토리 먼저 구현.

	public bool StaminaCheck(int Stamina) { return true; } // 커스텀 소비재화 구현: 미정. << Player에서 구현.

	#endregion
	
	public void Build(Vector2Int pos)
	{
		GameObject newBuilding = GameObject.Instantiate<GameObject>(prefabs[buildArrayIndex], buildings);
		BuildingInfo info = newBuilding.GetComponent<IBuildingObject>().obj.info;
		float realPosX = (pos.x + (info.width - 1)/2.0f) * TileDimension;
		float realPosY = (pos.y + (info.length - 1)/2.0f) * TileDimension;
		newBuilding.layer = buildLayer;
		newBuilding.transform.localPosition = new Vector3(realPosX, 0, realPosY);
		for (int i=-1; i <= info.width; i++) { for (int j=-1; j <= info.length; j++) {
			MapBuildable[pos.x + i, pos.y + j] |= (info.grid[i,j] | Buildable.UnderConstruction);
		}}
	}
	#endregion
}

public static class BuildableMethods {
	/// <summary>이 플래그가 해당 플래그와 하나라도 겹치는 것이 있는지 확인합니다.</summary>
	public static bool HasOneFlag(this Buildable buildable, Buildable flags) => ((buildable & flags) != Buildable.None);
	/// <summary>이 플래그가 해당 플래그 모두를 갖고 있는지 확인합니다.</summary>
	public static bool HasAllFlag(this Buildable buildable, Buildable flags) => ((buildable & flags) == flags);
}

public static class TransformMethods {
	/// <summary>해당 트랜스폼의 모든 자식 오브젝트를 삭제합니다.</summary>
	public static void RemoveAllChildren(this Transform parent) {
		if (Application.isPlaying) for (int i=0; i<parent.childCount; i++) {
			GameObject.Destroy(parent.GetChild(i).gameObject);
		}
		else for (int i=0; i<parent.childCount; i++) {
			GameObject.DestroyImmediate(parent.GetChild(i).gameObject);
		}
	}
}