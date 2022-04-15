using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

public class MapGenScript : MonoBehaviour {

	#region variables
    public Transform mapTransform;
	public Transform buildings;
    public Texture2D mapTexture;
	private GameObject buildMasks;
	public static float TileDimension = 4f;
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
		wallColorN = new Color32(0xFF, 0x00, 0x00, 0xFF),
		wallColorE = new Color32(0xFF, 0x00, 0x20, 0xFF),
		wallColorS = new Color32(0xFF, 0x00, 0x40, 0xFF),
		wallColorW = new Color32(0xFF, 0x00, 0x60, 0xFF),
		cornerColorNE = new Color32(0x00, 0xFF, 0x00, 0xFF),
		cornerColorSE = new Color32(0x00, 0xFF, 0x20, 0xFF),
		cornerColorSW = new Color32(0x00, 0xFF, 0x40, 0xFF),
		cornerColorNW = new Color32(0x00, 0xFF, 0x60, 0xFF),
		diagonalColor = new Color32(0xFF, 0x00, 0xFF, 0xFF),
		diagonalReverseColor = new Color32(0xFF, 0x80, 0xFF, 0xFF),
		edgeColorNE = new Color32(0x00, 0x00, 0xFF, 0xFF),
		edgeColorSE = new Color32(0x00, 0x20, 0xFF, 0xFF),
		edgeColorSW = new Color32(0x00, 0x40, 0xFF, 0xFF),
		edgeColorNW = new Color32(0x00, 0x60, 0xFF, 0xFF),
		emptyColor = new Color32(0x00, 0x00, 0x00, 0x00);
	#endregion

	#region Map var
	public static int width, height; // initialized with minimum value
	public bool mapCreated;
	public void SetWidthHeight() {
		width = mapTexture.width; // Allowed minimum value: 3
		height = mapTexture.height; // Allowed minimum value: 3
		// Debug.Log($"width and height have been set to {width} and {height}.");
	}
	#endregion

	public bool colliderEmbedded;

	#region Build var
    Ray ray;
    RaycastHit hitData;
	public static float maxDistance = 100;
	public static bool buildable;
	public static bool playerOverlapped;
	private int[] recentBuildPos, newBuildPos;
	public static Building currentBuilding;
	public static Building[] buildArray;
	private static int buildArrayIndex = 0;
	public static byte[,] MapBuildable; // see Building.cs for detail.
	public static bool overlayChanged;
	#endregion

    // private int width;
    // private int height;
	#endregion

	void Start() {
		buildArray = new Building[] { Building.Deco1, Building.Deco2 };
		currentBuilding = buildArray[0];
		if (!mapCreated) {
            RemoveAllChild(mapTransform);
			MapBuild();
		}
		// MapBuildable =  InitBuildable();
		SetWidthHeight();
		buildMasks = new GameObject("Build Masks"); //TODO 프리팹으로 만들고 Instantiate로 변경
		buildMasks.transform.SetParent(mapTransform);
		buildMasks.transform.localPosition = Vector3.zero;
		recentBuildPos = new int[2] {int.MinValue, int.MinValue};
	 	overlayChanged = true;
	}

	void LateUpdate() {
		if (false) { //! 제한 필요
			// Q or E input for change building
			if (Input.GetKeyDown(KeyCode.E)) {
				if (++buildArrayIndex > buildArray.Length - 1) buildArrayIndex = 0;
				currentBuilding = buildArray[buildArrayIndex];
				overlayChanged = true;
			}
			else if (Input.GetKeyDown(KeyCode.Q)) {
				if (--buildArrayIndex < 0) buildArrayIndex = buildArray.Length - 1;
				currentBuilding = buildArray[buildArrayIndex];
				overlayChanged = true;
			}
			// check if we need to update build overlay.
			Transform mouseTransform = Mouse_GetTransform();
			if (mouseTransform) { BuildOverlay(currentBuilding, mouseTransform.position); }
			else { /*Debug.Log("Finding...");*/ }

        	if (Input.GetKeyDown(KeyCode.Mouse0)) { LastBuildCheck(); }
			if (Input.GetKeyDown(KeyCode.F12)) {
				Debug.Log($"건설 위치 : ({newBuildPos[0]}, {newBuildPos[1]}) / 맵 크기 : ({width}, {height}) / 마우스 위치의 타일 정보 : ({MapBuildable[newBuildPos[0],newBuildPos[1]]})");
			}
		}
		// else if (BuildMasks.transform.childCount != 0) { RemoveAllChild(BuildMasks.transform); }
	}

	public byte[,] InitBuildable()
	{
		// mapTexture와 동일한 크기의 배열을 생성한다. Building.cs 참조.
		return new byte[0,0];
	}

	public Transform Mouse_GetTransform()
    {
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hitData, maxDistance, 1 << buildLayer)) { return hitData.transform; }
        else { return null; }
    }

	public void MapBuild() { GenerateMap(); mapCreated = true; }
	public void MapTextureCreate() { GetMapTexture(); }
	public void MapDestroy() { RemoveAllChild_InEditMode(mapTransform); mapCreated = false; }

	#region Texture -> Map
	public void GenerateMap() {
		float multiplierFactor = TileDimension + float.Epsilon;
		width = mapTexture.width;
		height = mapTexture.height;
		Debug.Log($"Generating Map Objects....\nwidth: {width}, height: {height}");
		Color32[] pixels = mapTexture.GetPixels32();
        for (int i = 0; i < height; i++) {
            for (int j = 0; j < width; j++) {
                Color32 pixelColor = pixels[i * height + j]; //Each color prefab is assign as follows:
				GameObject tile;
				// floor
				if (pixelColor.Equals(floorColor)) { tile = GameObject.Instantiate(floor, mapTransform); tile.name = nameof(floor); }
				// ceiling
				else if (pixelColor.Equals(ceilingColor)) { tile = GameObject.Instantiate(ceiling, mapTransform); tile.name = nameof(ceiling); }
				// wall
				else if (pixelColor.Equals(wallColorN)) { tile = GameObject.Instantiate(wallNorth, mapTransform); tile.name = nameof(wallNorth); }
				else if (pixelColor.Equals(wallColorE)) { tile = GameObject.Instantiate(wallEast, mapTransform); tile.name = nameof(wallEast); }
				else if (pixelColor.Equals(wallColorS)) { tile = GameObject.Instantiate(wallSouth, mapTransform); tile.name = nameof(wallSouth); }
				else if (pixelColor.Equals(wallColorW)) { tile = GameObject.Instantiate(wallWest, mapTransform); tile.name = nameof(wallWest); }
				// corner = Rectangular L Curve
				else if (pixelColor.Equals(cornerColorNE)) { tile = GameObject.Instantiate(cornerNorthEast, mapTransform); tile.name = nameof(cornerNorthEast); }
				else if (pixelColor.Equals(cornerColorSE)) { tile = GameObject.Instantiate(cornerSouthEast, mapTransform); tile.name = nameof(cornerSouthEast); }
				else if (pixelColor.Equals(cornerColorSW)) { tile = GameObject.Instantiate(cornerSouthWest, mapTransform); tile.name = nameof(cornerSouthWest); }
				else if (pixelColor.Equals(cornerColorNW)) { tile = GameObject.Instantiate(cornerNorthWest, mapTransform); tile.name = nameof(cornerNorthWest); }
				// diagonal
				else if (pixelColor.Equals(diagonalColor)) { tile = GameObject.Instantiate(diagonal, mapTransform); tile.name = nameof(diagonal); }
				else if (pixelColor.Equals(diagonalReverseColor)) { tile = GameObject.Instantiate(diagonalReverse, mapTransform); tile.name = nameof(diagonalReverse); }
				// edge
				else if (pixelColor.Equals(edgeColorNE)) { tile = GameObject.Instantiate(edgeNorthEast, mapTransform); tile.name = nameof(edgeNorthEast); }
				else if (pixelColor.Equals(edgeColorSE)) { tile = GameObject.Instantiate(edgeSouthEast, mapTransform); tile.name = nameof(edgeSouthEast); }
				else if (pixelColor.Equals(edgeColorSW)) { tile = GameObject.Instantiate(edgeSouthWest, mapTransform); tile.name = nameof(edgeSouthWest); }
				else if (pixelColor.Equals(edgeColorNW)) { tile = GameObject.Instantiate(edgeNorthWest, mapTransform); tile.name = nameof(edgeNorthWest); }
				// default = empty (emptyColor exists, but will not be used)
				else { tile = new GameObject("void"); tile.transform.parent = mapTransform; }
				tile.transform.localPosition = new Vector3(j * multiplierFactor, 0, i * multiplierFactor);
            }
        }
		
    }
	#endregion

	#region Map -> Texture
	public void GetMapTexture() {
		SetWidthHeight();
		if (mapTransform.childCount != width * height) {
			Debug.Log("Map 타일 개수가 설정된 변수와 다릅니다. Map 생성 매커니즘을 확인하세요.");
			return; 
		}
		mapTexture.Reinitialize(width, height);
		Color32[] pixels = new Color32[width * height];
		for (int i = 0; i < mapTransform.childCount; i++) {
			Transform tile = mapTransform.GetChild(i);
			switch (tile.name) {
				case nameof(floor): pixels[i] = floorColor; break;
				case nameof(ceiling): pixels[i] = ceilingColor; break;
				case nameof(wallNorth): pixels[i] = wallColorN; break;
				case nameof(wallEast): pixels[i] = wallColorE; break;
				case nameof(wallSouth): pixels[i] = wallColorS; break;
				case nameof(wallWest): pixels[i] = wallColorW; break;
				case nameof(cornerNorthEast): pixels[i] = cornerColorNE; break;
				case nameof(cornerSouthEast): pixels[i] = cornerColorSE; break;
				case nameof(cornerSouthWest): pixels[i] = cornerColorSW; break;
				case nameof(cornerNorthWest): pixels[i] = cornerColorNW; break;
				case nameof(diagonal): pixels[i] = diagonalColor; break;
				case nameof(diagonalReverse): pixels[i] = diagonalReverseColor; break;
				case nameof(edgeNorthEast): pixels[i] = edgeColorNE; break;
				case nameof(edgeSouthEast): pixels[i] = edgeColorSE; break;
				case nameof(edgeSouthWest): pixels[i] = edgeColorSW; break;
				case nameof(edgeNorthWest): pixels[i] = edgeColorNW; break;
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
	
	#region ChildSet
	public void RemoveAllChild(Transform TF)
	{
		for (var i = TF.childCount - 1; i >= 0; i--)
		{
			Destroy(TF.GetChild(i).gameObject);
		}
	}
	public void RemoveAllChild_InEditMode(Transform TF)
	{
		for (var i = TF.childCount - 1; i >= 0; i--)
		{
			DestroyImmediate(TF.GetChild(i).gameObject);
		}
	}
	#endregion


	#region Build
	public void BuildOverlay(Building building, Vector3 position) {
		newBuildPos = new int[2];
        newBuildPos[0] = (int)(position.x - base.transform.position.x)/(int)TileDimension;
        newBuildPos[1] = (int)(position.z - base.transform.position.z)/(int)TileDimension;
		// Update buildable, playerOverlapped & overlayChanged bool, and recentBuildPos based on newBuildPos.
		BuildableUpdate(building, newBuildPos);

		// reduce overlay cost by skipping rendering
		if ( !overlayChanged ) return;
		// remove previous tiles
		RemoveAllChild(buildMasks.transform);
		int buildX = building.scale[0], buildY = building.scale[1];
		// create overlay tiles with un/buildable color
		Color32 color;
		if (buildable) { color = new Color(0.5f,0.8f,1f,0.5f); }
		else { color = new Color(1f,0.5f,0.5f,0.5f); }
		for (int i = 0; i < buildX; i++) { for (int j = 0; j < buildY; j++) {
			if (!IsMapInside(newBuildPos[0] + i, newBuildPos[1] + j)) { continue; }
			GameObject renderTile = GameObject.CreatePrimitive(PrimitiveType.Plane);
			Destroy(renderTile.GetComponent<MeshCollider>());
			renderTile.transform.localScale = new Vector3(0.1f*TileDimension, 1, 0.1f*TileDimension);
			renderTile.GetComponent<Renderer>().material.SetColor("_Color", color);
			renderTile.transform.SetParent(buildMasks.transform);
			renderTile.transform.localPosition = new Vector3(TileDimension*(newBuildPos[0] + i), 0.01f, TileDimension*(newBuildPos[1] + j));
		}}
		overlayChanged = false;
		
	}

	/// <summary>
	/// buildable과 playerOverlapped를 체크해, 궁극적으로 overlayChanged를 업데이트합니다.<br/>
	/// overlayChanged가 true일 경우 타일 업데이트가 실행되므로 필요한 만큼만 적게 업데이트하는 것이 좋습니다.
	/// </summary>
	void BuildableUpdate(Building building, int[] buildPos) {
		// check All building tiles buildable -> 1. check player in or out  2. check cursor tile changed  3. check building changed
		int X = buildPos[0], Y = buildPos[1]; buildable = true;
		int buildX = building.scale[0], buildY = building.scale[1];
		// check all buildPos buildable
		for (int i = 0; i < buildX; i++) {
			if (!buildable) break; 
			for (int j = 0; j < buildY; j++) {
				if      (X+i >= width ) { buildable = false; break; }
				else if (X+i < 0) { buildable = false; break; }
				else if (Y+j >= height) { buildable = false; break; }
				else if (Y+j < 0) { buildable = false; break; }
				if ( (MapBuildable[X+i,Y+j] & building.typeArray[i,j]) != Building.None ) { buildable = false; break; }
			}
		}
		// check player in or out
		playerOverlapped = IsPlayerOverlapped(newBuildPos);
		if (playerOverlapped) buildable = false;
		if ( (recentBuildPos[0] != buildPos[0] || recentBuildPos[1] != buildPos[1]) && !overlayChanged ) { // 커서 타일 변동 확인
			overlayChanged = true;
			recentBuildPos = buildPos;
		}
	}

	/// <summary>
	/// 주어진 X, Y 좌표가 맵의 내부에 존재하면 true, 외부에 존재하면 false를 반환합니다.<br/>
	/// 맵의 외곽선이 빈 공간이어도 true를 반환합니다.
	/// </summary>
	bool IsMapInside(int X, int Y) {
		if (X < 0 || X > width || Y < 0 || Y > height) return false;
		return true;
	}

	bool IsPlayerOverlapped(int[] buildPos) {
		Vector3 playerPos = Player.instance.transform.position;
		float buildX1 = base.transform.position.x + (newBuildPos[0]-0.5f)* TileDimension,
			buildX2 = buildX1 + currentBuilding.scale[0]* TileDimension,
			buildZ1 = base.transform.position.z + (newBuildPos[1]-0.5f)* TileDimension,
			buildZ2 = buildZ1 + currentBuilding.scale[1]* TileDimension;
		if (buildX1 < playerPos.x && buildX2 > playerPos.x && buildZ1 < playerPos.z && buildZ2 > playerPos.z) {
			// overlapped
			if (buildable && !overlayChanged) overlayChanged = true;
			return true;
		}
		// not overlapped
		if (playerOverlapped && buildable && !overlayChanged) overlayChanged = true;
		return false;
	}

	public void LastBuildCheck()
	{
		Build(currentBuilding);
	}

	public bool BuildMaterialCheck(Building building) { return true; } // 건축 재료 기능 구현: 미정. << 인벤토리 먼저 구현.

	public bool StaminaCheck(int Stamina) { return true; } // 커스텀 소비재화 구현: 미정. << Player에서 구현.

	public void Build(Building building)
	{
		float realPosX = (newBuildPos[0] + (building.scale[0] - 1)/2.0f) * TileDimension;
		float realPosY = (newBuildPos[1] + (building.scale[1] - 1)/2.0f) * TileDimension;
		GameObject newBuilding = GameObject.Instantiate(building.Object, buildings);
		newBuilding.layer = buildLayer;
		foreach(Transform TF in newBuilding.transform.GetComponentsInChildren<Transform>()) {
			TF.gameObject.AddComponent<MeshCollider>();
		}
		newBuilding.transform.localPosition = new Vector3(realPosX, 0, realPosY);
		for (int i=0; i < currentBuilding.scale[0]; i++) { for (int j=0; j < currentBuilding.scale[1]; j++) {
			MapBuildable[recentBuildPos[0] + i, recentBuildPos[1] + j] |= currentBuilding.typeArray[i,j];
		}}
		overlayChanged = true;
	}
	#endregion
}