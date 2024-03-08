using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;

public static class LinqExtensions
{
	public static T Random<T>(this IEnumerable<T> enumerable)
		=> enumerable.ElementAt(UnityEngine.Random.Range(0, enumerable.Count()));

	public static IEnumerable<T> Linear<T>(this T[,] array)
	{
		foreach (var item in array) yield return item;
	}
	public static IEnumerable<(Vector2Int pos, T value)> GetValues<T>(this T[,] array)
	{
		for (int x = 0; x < array.GetLength(0); x++)
		{
			for (int y = 0; y < array.GetLength(1); y++)
			{
				yield return (new(x, y), array[x, y]);
			}
		}
	}
}
