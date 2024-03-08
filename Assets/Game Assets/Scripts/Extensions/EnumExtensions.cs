using System;
using System.Collections;

public static class EnumExtensions {
  /// <summary>Returns the highest bit set in the enum</summary>
  public static T MaxBit<T>(this T flag) where T : Enum {
    //todo FlagAttribute
    int value = Convert.ToInt32(flag);
    if (value == 0) return flag;

    int maxBit = 0;
    while ((value >>= 1) > 0)
      maxBit++;
    return (T)Enum.ToObject(typeof(T), 1 << maxBit);
  }
}
