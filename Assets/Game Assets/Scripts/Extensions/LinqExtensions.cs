using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LinqExtensions {
  public static T Random<T>(this IEnumerable<T> enumerable)
    => enumerable.ElementAt(UnityEngine.Random.Range(0, enumerable.Count()));
}