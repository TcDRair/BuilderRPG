using System.IO;
using System.Linq;
using System.Collections.Generic;
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
      if (_inst == null && (_inst = FindObjectOfType<MapGenerator>()) == null) {
        return _inst = new GameObject("MapGenerator").AddComponent<MapGenerator>();
      }
      return _inst;
    }
    set => _inst = value;
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
  // [SerializeField, Tooltip("지정된 파일과 무관하게 현재 생성된 맵의 정보를 나타냅니다."), ReadOnly]
  #endif
  Texture2D currentTexture;
  [Tooltip("오버레이 시 건설 가능성에 따라 바닥 타일을 덮는 데에 사용되는 프리팹을 할당합니다.")]
  public GameObject overlayAcceptPrefab, overlayDenyPrefab, overlayAdjacentPrefab;
  [Tooltip("오버레이 시 건설 가능성에 따라 오버레이에 적용되는 머티리얼을 할당합니다.")]
  public Material overlayAcceptMat, overlayAdjacentMat, overlayDenyMat;
  [Tooltip("오버레이를 생성할 때 이를 저장해두는 부모 트랜스폼을 할당합니다.")]
  public Transform overlayParent;
  public const float TileDimension = 4f;
  #endregion

  #region prefabObjects
  public GameObject Floor, Ceiling, WallN, CornerNE, Diagonal, EdgeNE;
  GameObject _wE, _wS, _wW;
  GameObject WallE {
    get {
      if (_wE == null) {
        _wE = WallN.InstantiateInvisible();
        _wE.transform.Rotate(0, 90, 0);
      }
      return _wE;
    }
  }
  GameObject WallS {
    get {
      if (_wS == null) {
        _wS = WallN.InstantiateInvisible();
        _wS.transform.Rotate(0, 180, 0);
      }
      return _wS;
    }
  }
  GameObject WallW {
    get {
      if (_wW == null) {
        _wW = WallN.InstantiateInvisible();
        _wW.transform.Rotate(0, 270, 0);
      }
      return _wW;
    }
  }
  GameObject _cSE, _cSW, _cNW;
  GameObject CornerSE {
    get {
      if (_cSE == null) {
        _cSE = CornerNE.InstantiateInvisible();
        _cSE.transform.Rotate(0, 90, 0);
      }
      return _cSE;
    }
  }
  GameObject CornerSW {
    get {
      if (_cSW == null) {
        _cSW = CornerNE.InstantiateInvisible();
        _cSW.transform.Rotate(0, 180, 0);
      }
      return _cSW;
    }
  }
  GameObject CornerNW {
    get {
      if (_cNW == null) {
        _cNW = CornerNE.InstantiateInvisible();
        _cNW.transform.Rotate(0, 270, 0);
      }
      return _cNW;
    }
  }
  GameObject _dR;
  GameObject DiagonalR {
    get {
      if (_dR == null) {
        _dR = Diagonal.InstantiateInvisible();
        _dR.transform.Rotate(0, 90, 0);
      }
      return _dR;
    }
  }
  GameObject _eSE, _eSW, _eNW;
  GameObject EdgeSE {
    get {
      if (_eSE == null) {
        _eSE = EdgeNE.InstantiateInvisible();
        _eSE.transform.Rotate(0, 90, 0);
      }
      return _eSE;
    }
  }
  GameObject EdgeSW {
    get {
      if (_eSW == null) {
        _eSW = EdgeNE.InstantiateInvisible();
        _eSW.transform.Rotate(0, 180, 0);
      }
      return _eSW;
    }
  }
  GameObject EdgeNW {
    get {
      if (_eNW == null) {
        _eNW = EdgeNE.InstantiateInvisible();
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
  RaycastHit hitData;
  public const float maxDistance = 100;
  [HideInInspector]
  public Building currentBuilding;
  #if UNITY_EDITOR
  [ReadOnly]
  #endif
  public Building[] buildArray;
  GameObject[] prefabs;
  private readonly int buildArrayIndex = 0;
  public (Buildable[,] built, Buildable[,] blocked) MapGrid;
  public (Buildable built, Buildable blocked) GridCell(int x, int y) => (MapGrid.built[x, y], MapGrid.blocked[x, y]);
  #endregion
  #endregion

  public void Awake() { Instance = this; }

  public void Start() {
    // 존재하는 모든 구조물 개체 확인
    prefabs = Resources.LoadAll<GameObject>("Prefabs/Building");
    buildArray = new Building[prefabs.Length];
    // 구조물 배열 저장
    for (int i=0; i<prefabs.Length; i++) {
      buildArray[i] = prefabs[i].GetComponent<IBuildingObject>().Obj;
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

  public void LateUpdate() {
    // UI상의 조작으로 건설 프리뷰 모드에 돌입. 해당 모드에서 수행할 동작을 정의합니다.
    if (State.Current.Main == State.MState.Mode_BuildPreview) {
      // 현재 프레임에 [선택된 셀]에 [선택된 구조물]을 설치할 수 있는지 판단하고 그에 따른 오버레이를 생성합니다.
      (int x, int y)? _cell = GetCurrentCell();
      if (_cell == null) {
        if (Input.GetKeyDown(KeyCode.F12)) Debug.Log($"구조물 셀 정보 : {currentBuilding.info.grid.Size}\n현재 선택된 건설 위치 없음. 마우스가 범위 밖을 가리킵니다.");
      }
      else {
        var cell = _cell.Value;
        if (Input.GetKeyDown(KeyCode.F12)) Debug.Log($"구조물 셀 정보 : {currentBuilding.info.grid.Size}\n건설 위치 : {cell} / 맵 크기 : ({width}, {length}) / 선택된 셀 정보 : {GridCell(cell.x, cell.y)}\n플레이어 위치 : {Player.Instance.transform.position}({IsPlayerInside(cell)})");
        BuildableInfo info = BuildOverlay(currentBuilding, cell);
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
          UI.Instance.ShowBuildMessage(info);
          if (info == BuildableInfo.OK) {
            Build(cell);
            State.Current.Set(State.MState.Idle);
          }
        }
  
        if (Input.GetKeyDown(KeyCode.Escape)) {
          State.Current.Set(State.MState.Idle);
        }
      }

      if (Input.GetKeyDown(KeyCode.E)) RotateBuilding(currentBuilding, true);
      else if (Input.GetKeyDown(KeyCode.Q)) RotateBuilding(currentBuilding, false);
    }
    else if (overlayParent.childCount != 0) overlayParent.RemoveAllChildren();
  }

  public void InitBuildable()
  {
    MapGrid = new() { built = new Buildable[width, length], blocked = new Buildable[width, length] };
    for (int i = 0; i < width; i++) {
      for (int j = 0; j < length; j++) {
        var color = currentTexture.GetPixel(i, j);
        if (color != floorColor && color != emptyColor) MapGrid.blocked[i, j] = Buildable.Full;
      }
    }
  }

  /// <summary>
  /// 현재 프레임에서 커서가 위치한 셀 인덱스를 반환합니다.<br/>
  /// 어떤 셀도 가리키고 있지 않을 경우 null을 반환합니다.
  /// </summary>
  public (int x, int y)? GetCurrentCell()
  {
    if (Physics.Raycast(MainCamera.ray, out hitData, maxDistance, floorMask)) {
      Transform cell = hitData.transform;
      // 가장 상위 바닥 오브젝트를 셀로 간주합니다.
      while (cell.parent.gameObject.layer == floorLayer) cell = cell.parent;
      return (
        (int)((cell.position.x - transform.position.x) / TileDimension),
        (int)((cell.position.z - transform.position.z) / TileDimension)
      );
    }
    else return null;
  }
  /// <summary>현재 프레임에서 주어진 벡터가 위치한 셀 인덱스를 반환합니다.</summary>
  public (int, int) GetCell(Vector3 pos)
  {
    return (
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
        // transparent -> empty
        if (pixelColor.a == 0) { tile = new GameObject("void"); tile.transform.parent = mapParent; }
        // floor
        else if (pixelColor.Equals(floorColor)) { tile = Instantiate(Floor, mapParent); tile.name = nameof(Floor); }
        // ceiling
        else if (pixelColor.Equals(ceilingColor)) { tile = Instantiate(Ceiling, mapParent); tile.name = nameof(Ceiling); }
        // wall
        else if (pixelColor.Equals(wallNColor)) { tile = WallN.InstantiateDefault(mapParent); tile.name = nameof(WallN); }
        else if (pixelColor.Equals(wallEColor)) { tile = WallE.InstantiateDefault(mapParent); tile.name = nameof(WallE); }
        else if (pixelColor.Equals(wallSColor)) { tile = WallS.InstantiateDefault(mapParent); tile.name = nameof(WallS); }
        else if (pixelColor.Equals(wallWColor)) { tile = WallW.InstantiateDefault(mapParent); tile.name = nameof(WallW); }
        // corner = Rectangular L Curve
        else if (pixelColor.Equals(cornerNEColor)) { tile = CornerNE.InstantiateDefault(mapParent); tile.name = nameof(CornerNE); }
        else if (pixelColor.Equals(cornerSEColor)) { tile = CornerSE.InstantiateDefault(mapParent) ; tile.name = nameof(CornerSE); }
        else if (pixelColor.Equals(cornerSWColor)) { tile = CornerSW.InstantiateDefault(mapParent) ; tile.name = nameof(CornerSW); }
        else if (pixelColor.Equals(cornerNWColor)) { tile = CornerNW.InstantiateDefault(mapParent) ; tile.name = nameof(CornerNW); }
        // diagonal
        else if (pixelColor.Equals(diagonalColor)) { tile = Diagonal.InstantiateDefault(mapParent); tile.name = nameof(Diagonal); }
        else if (pixelColor.Equals(diagonalRColor)) { tile = DiagonalR.InstantiateDefault(mapParent); tile.name = nameof(DiagonalR); }
        // edge
        else if (pixelColor.Equals(edgeNEColor)) { tile = EdgeNE.InstantiateDefault(mapParent); tile.name = nameof(EdgeNE); }
        else if (pixelColor.Equals(edgeSEColor)) { tile = EdgeSE.InstantiateDefault(mapParent); tile.name = nameof(EdgeSE); }
        else if (pixelColor.Equals(edgeSWColor)) { tile = EdgeSW.InstantiateDefault(mapParent); tile.name = nameof(EdgeSW); }
        else if (pixelColor.Equals(edgeNWColor)) { tile = EdgeNW.InstantiateDefault(mapParent); tile.name = nameof(EdgeNW); }
        // default = empty //? Normally this should never happen
        else if (pixelColor.Equals(emptyColor)) { tile = new GameObject("void"); tile.transform.parent = mapParent; }
        else if (pixelColor.Equals(emptyColor2)) { tile = new GameObject("void"); tile.transform.parent = mapParent; }
        // Error Occurs
        else { tile = null; Debug.LogError($"Error Occurs: {pixelColor} at {i*length+j}."); }
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
      pixels[i] = tile.name switch {
        nameof(Floor) => floorColor,
        nameof(Ceiling) => ceilingColor,
        nameof(WallN) => wallNColor,
        nameof(WallE) => wallEColor,
        nameof(WallS) => wallSColor,
        nameof(WallW) => wallWColor,
        nameof(CornerNE) => cornerNEColor,
        nameof(CornerSE) => cornerSEColor,
        nameof(CornerSW) => cornerSWColor,
        nameof(CornerNW) => cornerNWColor,
        nameof(Diagonal) => diagonalColor,
        nameof(DiagonalR) => diagonalRColor,
        nameof(EdgeNE) => edgeNEColor,
        nameof(EdgeSE) => edgeSEColor,
        nameof(EdgeSW) => edgeSWColor,
        nameof(EdgeNW) => edgeNWColor,
        _ => pixels[i] = emptyColor
      };
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
  public BuildableInfo BuildOverlay(Building building, (int x, int y) cell) {
    //! 임시. overlayChanged 적용 전까지만
    RemoveOverlay();

    // 건설 가능성 판단
    var info = CheckBuildable(building, cell);
    BuildableInfo reason = BuildableInfo.OK;
    //* 건설 불가능 이유 우선순위 : 조건 미달 + 재료 부족 + 재화 부족 / 영역 밖 / 플레이어 위치 / 셀 조건 제한
    foreach (BuildableInfo i in info.Values) if (reason < i) reason = i;
    int bW = building.info.grid.Size.x, bL = building.info.grid.Size.y;
    switch (reason) {
      // 영역 밖, 조건 미달, 재료 부족 or 재화 부족 : 적섹 오버레이 셀 생성, 건물 오버레이 없음
      case BuildableInfo.OutOfBounds:
      case BuildableInfo.NotQualified: 
      case BuildableInfo.NotEnoughMaterial: 
      case BuildableInfo.NotEnoughMoney: {
        for (int x = 0; x < bW; x++) for (int y = 0; y < bL; y++) {
          if (building.info.grid[x, y, true] == Buildable.None) continue;
          Vector3 pos = new((cell.x + x) * TileDimension, 0.01f, (cell.y + y) * TileDimension);
          Instantiate(overlayDenyPrefab, overlayParent).transform.localPosition = pos;
        }
        break;
      }
      // 플레이어 중첩 or 셀 조건 미달 : 건물 셀 조건 미달 시 적색 or 외곽 셀 조건 미달 시 황색, 나머지 셀은 청색 오버레이 생성
      case BuildableInfo.PlayerOverlapped:
      case BuildableInfo.Unbuildable:
      case BuildableInfo.OK: {
        for (int x = -1; x <= bW; x++) for (int y = -1; y <= bL; y++) {
          Vector3 pos = new((cell.x + x) * TileDimension, 0.01f, (cell.y + y) * TileDimension);
          //? 건물 셀 -> 건설 불가 시 적색, 건설 가능 시 청색
          if (x > -1 && x < bW && y > -1 && y < bL) Instantiate((info[(x, y)] > BuildableInfo.OK) ? overlayDenyPrefab : overlayAcceptPrefab, overlayParent).transform.position = pos;
          //? 코너 셀 -> 항상 오버레이 없음
          else if ((x, y) == (-1, -1) || (x, y) == (bW, -1) || (x, y) == (bW, bL) || (x, y) == (-1, bL)) continue;
          //? 경계면 셀
          else if (
            FindAdjacentCells(
              y == -1 ? building.info.grid[x, y+1, true] : Buildable.None,
              x == -1 ? building.info.grid[x+1, y, true] : Buildable.None,
              y == bL ? building.info.grid[x, y-1, true] : Buildable.None,
              x == bW ? building.info.grid[x-1, y, true] : Buildable.None
            ) != Direction.None
          ) if (info[(x, y)] > BuildableInfo.OK) Instantiate(overlayAdjacentPrefab, overlayParent).transform.localPosition = pos;
        }
        Vector3 buildingPos = new(cell.x * TileDimension, 0, cell.y * TileDimension);
        var previewModel = Instantiate(building.info.preview, overlayParent).transform;
        previewModel.localPosition = buildingPos;
        previewModel.localRotation = Quaternion.Euler(0, rotation * 90, 0);
        bool anyIssue = info.Any(i => i.Value > BuildableInfo.OK);
        bool insideIssue = info.Where(i => i.Key.x >= 0 && i.Key.x < bW && i.Key.y >= 0 && i.Key.y < bL).Any(i => i.Value > BuildableInfo.OK);
        previewModel.GetComponent<Renderer>().material = anyIssue ? (insideIssue ? overlayDenyMat : overlayAdjacentMat) : overlayAcceptMat;
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
  /// 해당 구조물을 <paramref name="cell"/> 위치에 건축할 수 있는지 판단하고, 건설 가능성을 <see cref="BuildableInfo"/> 으로 반환합니다.
  /// </summary>
  Dictionary<(int x, int y), BuildableInfo> CheckBuildable(Building building, (int x, int y) cell) {
    (int bX, int bY) = building.info.grid.Size;
    Dictionary<(int, int), BuildableInfo> info = new();
    //* 구조물의 모든 셀에 대해 다음의 조건을 판단합니다 :
    for (int i = -1; i <= bX; i++) {
      for (int j = -1; j <= bY; j++) {
        int x = cell.x + i, y = cell.y + j;
        //* 1. 건물 셀 판단
        if (i > -1 && j > -1 && i < bX && j < bY) {
          //? 1-1. 해당 셀이 건설 가능 영역(Map)을 벗어남 -> OutOfBounds
          if (x >= width || x < 0 || y >= length || y < 0) info[(i, j)] = BuildableInfo.OutOfBounds;
          //? 1-2. 건물 셀이 맵과 겹침 -> Unbuildable
          else if (!CanAddCell((x, y), building.info.grid[i, j])) info[(i, j)] = BuildableInfo.Unbuildable;
          //? 1-3. 건물 셀이 현재 플레이어와 겹침 -> PlayerOverlapped
          else if (IsPlayerInside((x, y))) info[(i, j)] = BuildableInfo.PlayerOverlapped;
          //? 조건 만족
          else info[(i, j)] = BuildableInfo.OK;
        }
        //* 2. 경계면 셀 판단
        else {
          //? 2-0. 경계면 셀이 아님. 즉 인접하는 건물 셀이 없음 -> None (판단 대상 아님)
          if ((i, j) == (-1, -1) || (i, j) == (-1, bY) || (i, j) == (bX, -1) || (i, j) == (bX, bY)) info[(i, j)] = BuildableInfo.None;
          //? 2-1. 해당 셀이 건설 가능 영역을 벗어남 -> None (판단 대상 아님)
          else if (x >= width || x < 0 || y >= length || y < 0) info[(i, j)] = BuildableInfo.None;
          //? 2-2. 경계면 셀이 인접 건물 셀과 충돌함 -> Unbuildable
          else {
            var blocked = GridCell(x, y).blocked;
            if (
              (i == -1 && building.info.grid[i+1, j, true].HasFlag(Buildable.Wall_West ) && blocked.HasFlag(Buildable.Wall_East )) ||
              (i == bX && building.info.grid[i-1, j, true].HasFlag(Buildable.Wall_East ) && blocked.HasFlag(Buildable.Wall_West )) ||
              (j == -1 && building.info.grid[i, j+1, true].HasFlag(Buildable.Wall_South) && blocked.HasFlag(Buildable.Wall_North)) ||
              (j == bY && building.info.grid[i, j-1, true].HasFlag(Buildable.Wall_North) && blocked.HasFlag(Buildable.Wall_South))
            ) info[(i, j)] = BuildableInfo.Unbuildable;
            //? 조건 만족
            else info[(i, j)] = BuildableInfo.OK;
          }
        }
      }
    }
    //* 계산된 건설 가능성 정보를 반환합니다.
    return info;
  }

  #region Direction
  [System.Flags] public enum Direction { None = 0, North = 1, East = 2, South = 4, West = 8 }
  /// <summary>해당 플래그가 어느 방향으로 인접 비트를 가지고 있는지 표시합니다.</summary>
  public Direction HasAdjacentBit(Buildable buildable) {
    Direction dir = Direction.None;
    if (buildable.HasFlag(Buildable.Wall_North)) dir |= Direction.North;
    if (buildable.HasFlag(Buildable.Wall_East)) dir |= Direction.East;
    if (buildable.HasFlag(Buildable.Wall_South)) dir |= Direction.South;
    if (buildable.HasFlag(Buildable.Wall_West)) dir |= Direction.West;
    return dir;
  }
  public void HasAdjacentBit(Buildable buildable, out Direction direction) => direction = HasAdjacentBit(buildable);
  /// <summary>해당 셀이 어느 방향에서 인접 셀의 영향을 받는지 표시합니다.</summary>
  public Direction FindAdjacentCells(params Buildable[] adjacentCells) {
    var dir = Direction.None;
    dir |= adjacentCells[0].HasFlag(Buildable.Wall_South) ? Direction.North : Direction.None;
    dir |= adjacentCells[1].HasFlag(Buildable.Wall_West) ? Direction.East : Direction.None;
    dir |= adjacentCells[2].HasFlag(Buildable.Wall_North) ? Direction.South : Direction.None;
    dir |= adjacentCells[3].HasFlag(Buildable.Wall_East) ? Direction.West : Direction.None;
    return dir;
  }
  public void FindAdjacentCells(out Direction direction, params Buildable[] adjacentCells) => direction = FindAdjacentCells(adjacentCells);
  #endregion

  /// <summary>건설 가능성을 나타내는 열거형입니다. 우선순위 오름차순으로 정의됩니다.</summary>
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
  bool IsPlayerInside((int, int) pos) => GetCell(Player.Instance.transform.position) == pos;

  /// <summary>
  /// 해당 위치의 셀에 주어진 플래그를 가진 구조물을 추가할 수 있는지 판단합니다.<br/>
  /// 하나의 셀에 대해서만 판단하므로 여러 셀을 판단할 때는 각 셀에 대해 사용해주세요.
  /// </summary>
  bool CanAddCell((int x, int y) pos, (Buildable occupying, Buildable needed) flags) {
    //* 0. 빈 플래그는 검사할 필요가 없음
    if ((flags.occupying | flags.needed) is Buildable.None) return true;

    // 아래 체크 과정에서 사용
    Buildable built = MapGrid.built[pos.x, pos.y], blocked = MapGrid.blocked[pos.x, pos.y];

    //* 0. 건축물 플래그의 배타성 확인
    if ((flags.occupying & flags.needed) is not Buildable.None) return false;
    
    //* 1. 건설 구조가 겹치지 않고 셀에 요구 구조가 존재하는지 확인
    if (((built | blocked) & flags.occupying) is not Buildable.None || !built.HasFlag(flags.needed)) return false;

    //? 인접 비트는 해당 셀에서 검사합니다.

    //* E. 모든 조건 만족
    return true;
  }
  /// <summary>해당 위치의 셀에 주어진 구조물의 플래그를 추가합니다.</summary>
  void SetCell((int x, int y) pos, Buildable occupied) {
    MapGrid.built[pos.x, pos.y] |= occupied;
    if (occupied.HasFlag(Buildable.Wall_North) && pos.y < length - 1) MapGrid.blocked[pos.x, pos.y + 1] |= Buildable.Wall_South;
    if (occupied.HasFlag(Buildable.Wall_East) && pos.x < width - 1) MapGrid.blocked[pos.x + 1, pos.y] |= Buildable.Wall_West;
    if (occupied.HasFlag(Buildable.Wall_South) && pos.y > 0) MapGrid.blocked[pos.x, pos.y - 1] |= Buildable.Wall_North;
    if (occupied.HasFlag(Buildable.Wall_West) && pos.x > 0) MapGrid.blocked[pos.x - 1, pos.y] |= Buildable.Wall_East;
  }
  void RemoveCell((int x, int y) pos, Buildable occupied) {
    MapGrid.built[pos.x, pos.y] &= ~occupied;
    if (occupied.HasFlag(Buildable.Wall_North) && pos.y < length - 1) MapGrid.blocked[pos.x, pos.y + 1] &= ~Buildable.Wall_South;
    if (occupied.HasFlag(Buildable.Wall_East) && pos.x < width - 1) MapGrid.blocked[pos.x + 1, pos.y] &= ~Buildable.Wall_West;
    if (occupied.HasFlag(Buildable.Wall_South) && pos.y > 0) MapGrid.blocked[pos.x, pos.y - 1] &= ~Buildable.Wall_North;
    if (occupied.HasFlag(Buildable.Wall_West) && pos.x > 0) MapGrid.blocked[pos.x - 1, pos.y] &= ~Buildable.Wall_East;
  }

  // public bool BuildMaterialCheck(Building building) { return true; } // 건축 재료 기능 구현: 미정. << 인벤토리 먼저 구현.

  // public bool StaminaCheck(int Stamina) { return true; } // 커스텀 소비재화 구현: 미정. << Player에서 구현.

  #endregion
  
  /// <summary>건물을 해당 위치에 생성하고 맵의 <see cref="Buildable"/> 데이터를 업데이트합니다.</summary>
  public void Build((int x, int y) pos)
  {
    GameObject newBuilding = Instantiate(prefabs[buildArrayIndex], buildingParent);
    Building building = newBuilding.GetComponent<IBuildingObject>().Obj;
    BuildingInfo info = building.info;

    float realPosX = (pos.x + (info.grid.Size.x - 1)/2.0f) * TileDimension;
    float realPosY = (pos.y + (info.grid.Size.y - 1)/2.0f) * TileDimension;
    newBuilding.layer = buildLayer;
    newBuilding.transform.localPosition = new Vector3(realPosX, 0, realPosY);
    newBuilding.transform.rotation = Quaternion.Euler(0, 90 * rotation, 0);
    
    building.position = pos;

    for (int x = 0; x < info.grid.Size.x; x++) for (int y = 0; y < info.grid.Size.y; y++) SetCell((pos.x + x, pos.y + y), info.grid[x, y].occupied);
  }

  /// <summary>맵의 <see cref="Buildable"/> 데이터에서 해당 건축물을 제거합니다.</summary>
  public void CleanBuilding(Building building) {
    BuildingInfo info = building.info;
    (int x, int y) size = info.grid.Size, pos = building.position;
    for (int x = 0; x < size.x; x++) for (int y = 0; y < size.y; y++) RemoveCell((pos.x + x, pos.y + y), info.grid[x, y].occupied);
  }
  #endregion

  #region Rotate
  int rotation = 0;
  /// <summary>건물을 회전시킵니다.</summary>
  public void RotateBuilding(Building building, bool clockwise) {
    var original = building.info.grid;
    int x = original.Size.x, y = original.Size.y;
    Buildable[] newOc = new Buildable[x * y], newNe = new Buildable[x * y];
    rotation = (rotation + (clockwise ? 1 : 3)) % 4;
    for(int i = 0; i < x * y; i++) {
      int j = clockwise ? i/y + (y - 1 - i%y) * x : x - 1 - i/y + i%y * x;
      newOc[i] = RotateBit(original.Data.occupiedGrid[j], clockwise);
      newNe[i] = RotateBit(original.Data.neededGrid[j], clockwise);
    }
    building.info.grid.Data = ((y,x), newOc, newNe);
  }

  public Buildable RotateBit(Buildable bit, bool clockwise) {
    Buildable result = Buildable.None;
    if (clockwise) {
      if (bit.HasFlag(Buildable.Wall_North)) result |= Buildable.Wall_East;
      if (bit.HasFlag(Buildable.Wall_East )) result |= Buildable.Wall_South;
      if (bit.HasFlag(Buildable.Wall_South)) result |= Buildable.Wall_West;
      if (bit.HasFlag(Buildable.Wall_West )) result |= Buildable.Wall_North;
      if (bit.HasFlag(Buildable.Attatch_Wall_North)) result |= Buildable.Attatch_Wall_East;
      if (bit.HasFlag(Buildable.Attatch_Wall_East )) result |= Buildable.Attatch_Wall_South;
      if (bit.HasFlag(Buildable.Attatch_Wall_South)) result |= Buildable.Attatch_Wall_West;
      if (bit.HasFlag(Buildable.Attatch_Wall_West )) result |= Buildable.Attatch_Wall_North;
    }
    else {
      if (bit.HasFlag(Buildable.Wall_North)) result |= Buildable.Wall_West;
      if (bit.HasFlag(Buildable.Wall_East )) result |= Buildable.Wall_North;
      if (bit.HasFlag(Buildable.Wall_South)) result |= Buildable.Wall_East;
      if (bit.HasFlag(Buildable.Wall_West )) result |= Buildable.Wall_South;
      if (bit.HasFlag(Buildable.Attatch_Wall_North)) result |= Buildable.Attatch_Wall_West;
      if (bit.HasFlag(Buildable.Attatch_Wall_East )) result |= Buildable.Attatch_Wall_North;
      if (bit.HasFlag(Buildable.Attatch_Wall_South)) result |= Buildable.Attatch_Wall_East;
      if (bit.HasFlag(Buildable.Attatch_Wall_West )) result |= Buildable.Attatch_Wall_South;
    }

    bit ^= bit & Buildable.Attatched_Wall; bit |= result;
    return bit;
  }
  #endregion
}