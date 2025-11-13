using System.Collections.Generic;
using UnityEngine;

namespace MortierFu.Shared
{
    public static class ListExtensions
    {
        public static T RandomElement<T>(this IList<T> list)
        {
            return list[Random.Range(0, list.Count)];
        }
    }
}
