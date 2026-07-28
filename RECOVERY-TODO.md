# 남은 끊긴 참조 작업 목록

> **이 파일은 자동 생성됩니다.** `Tools/Rair/Audit/복구 목록 생성`
> 마지막 생성 2026-07-28

## 요약

| 구분 | 원인 GUID | 참조 발생 | 영향 파일 |
|---|---|---|---|
| **복구 대상** (에셋 유실) | 101 | 217 | 74 |
| **정리 대상** (스크립트 삭제분) | 8 | 14 | 12 |
| 합계 | 109 | 231 | 78 |

참조 발생 수가 원인 수보다 훨씬 큰 것은 정상입니다. 
프리팹 하나가 유실되면 그 안의 오버라이드마다 참조가 하나씩 잡히기 때문입니다.
**작업 단위는 원인 GUID입니다.**

## 복구 대상 — 유실된 에셋 (101건)

에셋 파일을 되찾거나 대체본을 연결해야 합니다.

| GUID | 추정 종류 | 참조 | 영향 파일 |
|---|---|---|---|
| `19e6313ae11973f49acec97736950fd9` | Prefab | 14 | …/Prefabs/Field/Foilage/Flower 1.prefab<br/>…/Prefabs/Field/Foilage/Flower 4.prefab |
| `1311245ee182a2e44b8f2a1966b92ac4` | Prefab | 13 | …/Prefabs/Field/Bush/Bush 3.prefab |
| `fe0224e0f46a3c24cb8afe0bd433f4d1` | Prefab | 13 | …/Prefabs/Field/Bush/Bush 2.prefab |
| `186396b9ea34d434b8dc135ba8f27e7f` | Prefab | 13 | …/Prefabs/Field/Bush/Bush 1.prefab |
| `97662451c4a683a4aba6a95c251bbd9b` | Mesh | 7 | …/Prefabs/Field/Tree/Birch 1.prefab |
| `aa9f32080e4917245b5d28e976edc888` | objectReference | 7 | …/Prefabs/Building/Small Wooden Shelter.prefab |
| `f14b2e45a8a875942a913823730e48ea` | Prefab | 6 | …/Prefabs/UI/Building Tags/Group - HeatSource.prefab<br/>…/Prefabs/UI/Building Tags/Group - Indoor.prefab<br/>…/Prefabs/UI/Building Tags/Group - Road.prefab<br/>… 외 3개 |
| `c0d8f7125aea02b4abab939f84ddbad4` | Mesh | 4 | …/Prefabs/Field/Tree/Birch 4.prefab |
| `04decf04fce375149bffe1b9335321ae` | Mesh | 4 | …/Prefabs/Field/Tree/Birch 2.prefab |
| `e9d8ba9de4645684dad0fc0f71b17a30` | Mesh | 4 | …/Prefabs/Field/Tree/Birch 3.prefab |
| `4c7983e82406d1340a6a4b79b5443527` | Sprite | 4 | Assets/Migrated/Mastery(Temp).unity |
| `bc3d66023a3b5184e852c0eccf02a9f8` | Sprite | 4 | …/Prefabs/UI/_deprecated_Interact/Interact Slot Small.prefab<br/>…/Prefabs/UI/_deprecated_Interact/Interact Slot.prefab<br/>…/Prefabs/UI/Interaction UI.prefab<br/>… 외 1개 |
| `e226fd29c3753d345b82dfc08f061569` | Mesh | 4 | …/Prefabs/Field/Tree/Large Tree 1.prefab |
| `0824121ba71ed444ab4dd7e31c914bc7` | (필드 혼재) | 4 | …/Prefabs/Building/Small Wooden Shelter.prefab<br/>…/Prefabs/Map/Map Generator.prefab |
| `6aaf3224b19f0324289f5569feaca2aa` | Mesh | 3 | …/Prefabs/Field/Bush/Bush 7.prefab |
| `213b6a998ec81f547ba899c1403eb80b` | Mesh | 3 | …/Prefabs/Field/Bush/Bush 9.prefab |
| `f18f1f7f75fb51e4daaa85176d45dfab` | Mesh | 3 | …/Prefabs/Field/Tree/Apple Tree 1.prefab |
| `029e98a0305735448b9854b9e38c11d6` | Mesh | 3 | …/Prefabs/Field/Tree/Birch 5.prefab |
| `1632a9baea1fc13408a2e87d6ffc8532` | Mesh | 3 | …/Prefabs/Field/Bush/Bush 10.prefab |
| `e22cfbfe4d0c4f24a82254e9b3d3564a` | Mesh | 3 | …/Prefabs/Field/Bush/Bush 6.prefab |
| `2ce1b2c146c1df143b6c9d391c40072f` | Mesh | 2 | …/Models/SM_Env_Dirt_Rows_01.prefab |
| `9985e4f73cfdd3248aa3bb81efa3f8bc` | Mesh | 2 | …/Prefabs/Field/Bush/Bush 4.prefab |
| `6302899dc8cce95418c6244c53198b29` | Mesh | 2 | …/Models/SM_Env_Dirt_Rows_Mounds_01.prefab |
| `c863742064cbcf541aa869622b3bf949` | Mesh | 2 | …/Prefabs/Field/Bush/Bush 8.prefab |
| `b3b607796b5fb4a408870929169a70fe` | Mesh | 2 | …/Prefabs/Map/Basic Models/Diagonal Wall.prefab |
| `2ca00a7e0c04965408f4b472f11a9a7b` | Mesh | 2 | …/Models/DirtEnv.prefab<br/>…/Prefabs/Map/Main Models/Dirt Floor.prefab |
| `1cc7c6e1ae116e446be56a253fe91a03` | Sprite | 2 | …/Prefabs/UI/_deprecated_Interact/Interact Slot Small.prefab<br/>…/Prefabs/UI/_deprecated_Interact/Interact Slot.prefab |
| `d86ebf4784f9d664eaa8c05a23b31f64` | Sprite | 2 | …/Prefabs/UI/_deprecated_Interact/Interact Slot Small.prefab<br/>…/Prefabs/UI/_deprecated_Interact/Interact Slot.prefab |
| `a9ae690c61c6b0649a1140ff64613a96` | Sprite | 2 | …/Prefabs/UI/_deprecated_Interact/Interact Slot Small.prefab<br/>…/Prefabs/UI/_deprecated_Interact/Interact Slot.prefab |
| `e95387ab71d79df4bb0be5661f337937` | Mesh | 2 | …/Prefabs/Map/Basic Models/Floor.prefab |
| `f6c085a18ef2c6540aa2f2da6ead7af6` | Mesh | 2 | …/Prefabs/Field/Foilage/Flower 2.prefab<br/>…/Prefabs/Field/Foilage/Flower 3.prefab |
| `de776df8e96f1be448699f9925c68403` | Prefab | 2 | …/Prefabs/Building/Small Wooden Shelter.prefab |
| `a3dc8da8b210b014aa2dfd1e4a35dddf` | Mesh | 2 | …/Prefabs/Map/Basic Models/Wall.prefab |
| `6abef2f485188f4488dfc3a83e431b8c` | Mesh | 2 | …/Models/SM_Env_Tree_Plum_Grown_01.prefab |
| `8c23d6fbdb55d1846a4c49aa147db5d9` | Mesh | 2 | …/Prefabs/Map/Basic Models/Corner Wall.prefab |
| `6ce751d17b4c32047a2fe72bfd547482` | Mesh | 2 | …/Prefabs/Field/Bush/Bush 5.prefab |
| `67583ca214f183b4fb87653eaa5a6890` | Mesh | 2 | …/Prefabs/Map/Basic Models/Edge Wall.prefab |
| `1f55be77a812f2e44aa8ec57d388ca6a` | AnimatorController | 1 | Assets/Migrated/Loading.unity |
| `ea998e3294889624985351560923ba0a` | Loading | 1 | Assets/Migrated/Loading.unity |
| `f83e9462d8dbe654b9dc1ea2294dba0d` | Error | 1 | Assets/Migrated/Loading.unity |
| `f1cd249aa7aad344ea6efaa9c3c98ed8` | Sprite | 1 | …/Prefabs/UI/Building Tags/Group - Shelter.prefab |
| `a152664cecaa1594894c183669d0ef80` | Mesh | 1 | …/Prefabs/Field/Foilage/Mushroom 4.prefab |
| `6fed340dbe940d94d96b34d7813858a9` | Mesh | 1 | …/Models/SM_Env_Vege_Rows_03.prefab |
| `19f3cdde7d5104c4da897ac17045ddfd` | Mesh | 1 | …/Prefabs/Field/Grass/Fern_C.prefab |
| `1ea33222cad50ad49aa4978bc84846e4` | Mesh | 1 | …/Prefabs/Field/Grass/Bush_B.prefab |
| `68b6ee6fe6a466244bbc471f48eaaae1` | Mesh | 1 | …/Prefabs/Field/Tree/Apple Tree 1.prefab |
| `2532d22dd52b5854189d40a50540b705` | Mesh | 1 | …/Prefabs/Field/Grass/Fern_A.prefab |
| `a0ea72ae2a4777947bfa011951a34cc7` | Mesh | 1 | …/Prefabs/Field/Foilage/FlowerField 1.prefab |
| `6d83d18aefaa4e343bf06db4b0f667bd` | AnimatorController | 1 | …/Prefabs/UI/EffectUI.prefab |
| `67b24cfb031102847babf4d513bd51c4` | Mesh | 1 | …/Models/SM_Env_PuddlePlane_01.prefab |
| `d427b65f0342fe84fa77dabe420757ee` | Mesh | 1 | …/Prefabs/Field/Grass/Plant_A.prefab |
| `bc99ef1c2a9812b48b4647b65c1a4f23` | Sprite | 1 | Assets/Migrated/Mastery(Temp).unity |
| `ebb98dadf86d85b4e89b5c98bc816c52` | CategoryButtonPrefab | 1 | Assets/Migrated/Mastery(Temp).unity |
| `4a2f6625ff150754fa8d1fe5363a68aa` | MasteryPrefab | 1 | Assets/Migrated/Mastery(Temp).unity |
| `79c8fc092d78bc74ea1cc6eccbe937cd` | ArrowLinePrefab | 1 | Assets/Migrated/Mastery(Temp).unity |
| `76ef63b9c5ecc7944a79eaa808fb903a` | Sprite | 1 | Assets/Migrated/Mastery(Temp).unity |
| `afcd6756e48191f40b13854d6aa67061` | Sprite | 1 | Assets/Migrated/Mastery(Temp).unity |
| `57ad6470f0e6523479446626d5eed202` | Sprite | 1 | Assets/Migrated/Mastery(Temp).unity |
| `cc032f9003b806b4e93ef66b27d4faf7` | Mesh | 1 | …/Prefabs/Field/Foilage/Mushroom 5.prefab |
| `58195e52e760acd42a6c2a2c84e0b75a` | Mesh | 1 | …/Prefabs/Field/Foilage/Mushroom 2.prefab |
| `9745074a080534948a41c4a27ab7bd9e` | Mesh | 1 | …/Prefabs/Field/Foilage/FlowerField 2.prefab |
| `211a44eba6f5ca34ea3d3ac34bbc3469` | Mesh | 1 | …/Prefabs/Field/Grass/Heather_A.prefab |
| `e318f31bebce62d48a648da3f8ba56c9` | m_DiffuseTexture | 1 | …/Terrain/Layers/Grass.terrainlayer |
| `eb70932bab3d971408c011dbb45767a9` | m_NormalMapTexture | 1 | …/Terrain/Layers/Grass.terrainlayer |
| `0f48eac8617d0de4cbc056e1734e798b` | m_MaskMapTexture | 1 | …/Terrain/Layers/Grass.terrainlayer |
| `3b8f934b79237034b816dfcc8369e9fb` | Mesh | 1 | …/Prefabs/Field/Grass/BushDry_A.prefab |
| `7503e90c66670cd4eb59ce0f4b716259` | Mesh | 1 | …/Prefabs/Field/Foilage/Mushroom 1.prefab |
| `c77860fdd5c7e0344bfa48b9dc5bcb49` | Sprite | 1 | …/Prefabs/UI/Building Tags/Group - Indoor.prefab |
| `fa4ffd32709146c448c403bfd045f3fb` | Mesh | 1 | …/Prefabs/Field/Grass/Heather_B.prefab |
| `c6a491f205bb19a4f89c82d12b97bb17` | Sprite | 1 | …/Prefabs/UI/Interaction UI.prefab |
| `8e23a03b070b02343ae289c317c04cb8` | Texture | 1 | …/Material/Zone Projector.mat |
| `c671cabf34dd5314ca28492ced9c9001` | Mesh | 1 | …/Prefabs/Field/Grass/Plant_C.prefab |
| `7b6e835a2c045b949a796ab8f56c8967` | Sprite | 1 | …/Prefabs/Building/Small Wooden Shelter.prefab |
| `9173f6bf820ba1749adf08e66cc1ecc9` | Prefab | 1 | …/Prefabs/Building/Small Wooden Shelter.prefab |
| `c6a822fbd0c79ee49abee65eca036a8d` | objectReference | 1 | …/Prefabs/Building/Small Wooden Shelter.prefab |
| `2595e06a632e27540b05284f7bbe03e2` | Prefab | 1 | …/Prefabs/Building/Small Wooden Shelter.prefab |
| `8ef50150c6615f244a5742902f30e1f0` | Prefab | 1 | …/Prefabs/Building/Small Wooden Shelter.prefab |
| `a4e9cd7a8157dc44dbd512002baa4e43` | Prefab | 1 | …/Prefabs/Building/Small Wooden Shelter.prefab |
| `8d68c191594d5a24baee81cc22acf55b` | Mesh | 1 | …/Prefabs/Field/Grass/BushDry_B.prefab |
| `2f9d2984488477d498e8d5c61756f557` | Mesh | 1 | …/Prefabs/Field/Grass/Fern_B.prefab |
| `7a4db7b2b211ccf43a295a6eabffce07` | Mesh | 1 | …/Prefabs/Field/Foilage/Mushroom 3.prefab |
| `0c4301a6ce8e5964c8df62a78aca15d1` | Mesh | 1 | …/Prefabs/Field/Grass/Bush_A.prefab |
| `6aaae776b6965754d8eae1882070c385` | Mesh | 1 | …/Models/SM_Env_Tree_Plum_Grown_01.prefab |
| `976b713aa42e1e240a53d5998b9dfc75` | Mesh | 1 | …/Prefabs/Field/Foilage/Mushroom 6.prefab |
| `8f4881ba6ffc2ce48a096f26b3b36bca` | mapTexture | 1 | …/Prefabs/Map/Map Generator.prefab |
| `45dd9c23c5691054f82b1aff11c82bf6` | overlayDenyMat | 1 | …/Prefabs/Map/Map Generator.prefab |
| `30a18c0c57a55ba41a8e68e05e34e859` | ceiling | 1 | …/Prefabs/Map/Map Generator.prefab |
| `6ccb420be26286b40bbfd8c2eb2cb0a9` | wallNorth | 1 | …/Prefabs/Map/Map Generator.prefab |
| `1b3d16ee50c849a4ca48567ec9f7f5c2` | wallEast | 1 | …/Prefabs/Map/Map Generator.prefab |
| `59984063fbe3fa246a13213cbb23d9ac` | wallSouth | 1 | …/Prefabs/Map/Map Generator.prefab |
| `991696768a0b1574da810715235ae906` | wallWest | 1 | …/Prefabs/Map/Map Generator.prefab |
| `97025d35e5b57294f92451cbaf836c12` | Mesh | 1 | …/Prefabs/Field/Grass/Plant_B.prefab |
| `5bd1238cd0972254fb33c43bd8ab38c7` | Sprite | 1 | Assets/Scenes/SampleScene.unity |
| `6a7e8cd5e1ea8794c838b902529f257c` | Mesh | 1 | …/Prefabs/Field/Grass/Plant_D.prefab |
| `d6da14dc53109dc4cb19f254766d7da9` | Mesh | 1 | …/Models/SM_Env_Vege_Rows_01.prefab |
| `84a17cfa13e40ae4082ef42714f0a81c` | Shader | 1 | ProjectSettings/VFXManager.asset |
| `23c51f21a3503f6428b527b01f8a2f4e` | Shader | 1 | ProjectSettings/VFXManager.asset |
| `ea257ca3cfb12a642a5025e612af6b2a` | Shader | 1 | ProjectSettings/VFXManager.asset |
| `8fa6c4009fe2a4d4486c62736fc30ad8` | Shader | 1 | ProjectSettings/VFXManager.asset |
| `33a2079f6a2db4c4eb2e44b33f4ddf6b` | Shader | 1 | ProjectSettings/VFXManager.asset |
| `bc10b42afe3813544bffd38ae2cd893d` | m_RuntimeResources | 1 | ProjectSettings/VFXManager.asset |

## 정리 대상 — 유실된 스크립트 (8건)

스크립트 파일이 삭제된 결과입니다. 되살리는 것이 아니라 참조를 걷어내는 쪽이 맞습니다.

| GUID | 추정 종류 | 참조 | 영향 파일 |
|---|---|---|---|
| `ac01cbc959ad4d845af9f4a45e70e919` | MonoScript | 7 | …/Prefabs/UI/Building Tags/Group - HeatSource.prefab<br/>…/Prefabs/UI/Building Tags/Group - Indoor.prefab<br/>…/Prefabs/UI/Building Tags/Group - Road.prefab<br/>… 외 4개 |
| `cb789faf13a57ef47bcc6cb6171addb1` | MonoScript | 1 | Assets/Migrated/MainOld.unity |
| `04e03b60d2eb6594f89d90cb30ad9aa5` | MonoScript | 1 | Assets/Migrated/MainOld.unity |
| `1f02b6172472bfd4cbd7379293213eb3` | MonoScript | 1 | …/Prefabs/Building/Small Wooden Shelter.prefab |
| `d9f3891d2cbcf1445b2520120ad441d8` | MonoScript | 1 | …/Prefabs/Building/Small Wooden Shelter.prefab |
| `64567d1adeb8b35448a4449552875e66` | MonoScript | 1 | …/Prefabs/Map/Map Generator.prefab |
| `63a2978a97e4fc04cb9d905947216f3d` | MonoScript | 1 | ProjectSettings/HDRPProjectSettings.asset |
| `65bae8b9f1bd244b3a27e92af4b23b2a` | MonoScript | 1 | ProjectSettings/VisualScriptingSettings.asset |

---

<!-- MANUAL: 이 아래는 자동 생성이 덮어쓰지 않습니다 -->

## 확인된 원인 (수동 판정)

- `Small Wooden Shelter.prefab` — `BuildableGrid.cs` (커밋 `0d8ef958`에서 삭제)
- `Small Wooden Shelter.prefab` — `Scripts/Fields/Building` 폴더 (사고 이전 삭제)
- `Map Generator.prefab` — `MapGenScript.cs` (커밋 `e3a6185`에서 삭제)
