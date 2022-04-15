using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using UnityEditor;

public struct Building
{
    public string name;
    public string type;
    public byte[,] typeArray;
    public int[] scale; // int[2]
    public int Stamina;
    public GameObject Object;

    public bool isValid {
        get { return this.isValid; }
        set {
            if ( (this.Stamina > 0) || (this.scale[0] == typeArray.GetLength(0)) || (this.scale[1] == typeArray.Length) ) { this.isValid = true; }
            else { this.isValid = false; }
        }
    }

    public static Building Deco1 = new Building() {
        name = "Deco1", type = "decoration",
        scale = new int[2] {2,2},
        typeArray = new byte[2,2] { {FullStruct,FullStruct}, {FullStruct,FullStruct} },
        Stamina = 0,
        Object = Resources.Load<GameObject>(ResPath + "BuildingEx1")
        // Object = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath + "BuildingEx1.blend")
    };

    public static Building Deco2 = new Building() {
        name = "Deco2", type = "decoration",
        scale = new int[2] {1,1},
        typeArray = new byte[1,1] { {FullStruct} },
        Stamina = 0,
        Object = Resources.Load<GameObject>(ResPath + "BuildingEx2")
        // Object = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath + "BuildingEx2.blend")
    };
    
    // Building is for all types of buildings subordinate to Map (Tile) coordinates.
    // There can be same typeInt with different type. typeInt is just for non-overlap function.
    // Required components would be
    // Making custom Type byte is allowed.
    // Ex: Doorframe without a threshold and door will be: 0b_0010_0010.
    //     ↑ It uses Flag1 to avoid overlapping with Door type entity. (then Door will be: 0b_0100_0010.)
    //
    //*                                           //  Required      ||  Avoided       ||  Examples & Comments
    public const byte None        = 0b_0000_0000; //                ||                ||  Pings, Effects, Traps, etc.
    public const byte EmptyStruct = 0b_0001_0000; //                ||                ||  Empty Structure
    public const byte Floor       = 0b_0000_0001; //  Structure(4)  ||                ||  Flooring, Carpet
    public const byte Wall        = 0b_0000_0010; //  Structure(4)  ||                ||  Wall, Door
    public const byte Ceiling     = 0b_0000_0100; //  Structure(4)  ||                ||  Ceiling light
    public const byte Inside      = 0b_0000_1000; //  Structure(4)  ||                ||
    public const byte Ground      = 0b_0000_0001; //                ||  Floor(1)      ||  Road tile
    public const byte FullStruct  = 0b_0001_1111; //                ||                ||  Full Structure
    public const byte Flag1       = 0b_0010_0000; //                ||                ||  For avoid overlapped types
    public const byte Flag2       = 0b_0100_0000; //                ||                ||  For avoid overlapped types
    public const byte Flag3       = 0b_1000_0000; //                ||                ||  For avoid overlapped types
    public const byte FULL        = 0b_1111_1111; //                ||  All           ||  Will be used for something

    private const string AssetPath = "Assets/UsingAssets/Resources/Prefabs/Buildings/";
    private const string ResPath = "Prefabs/Buildings/";
}