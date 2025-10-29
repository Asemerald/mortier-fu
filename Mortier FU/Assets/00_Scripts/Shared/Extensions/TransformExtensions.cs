using System.Collections.Generic;
using UnityEngine;

namespace MortierFu.Shared
{
    public static class TransformExtensions {
        /// <summary>
        /// Return an IEnumerable of transform representing the children of the specified Transform.
        /// | Credits: <see href="https://www.youtube.com/@git-amend">git-amend</see>
        /// </summary>
        /// <remarks>
        /// You should use the transform directly to use a foreach loop or better use a for loop to prevent GC.
        /// </remarks>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static IEnumerable<Transform> Children(this Transform parent) {
            foreach(Transform child in parent) {
                yield return child;
            }
        }

        /// <summary>
        /// Destroy all children of the specified transform.
        /// | Credits: <see href="https://www.youtube.com/@git-amend">git-amend</see>
        /// </summary>
        /// <param name="parent">The parent transform of the children to destroy.</param>
        public static void DestroyChildren(this Transform parent) {
            parent.PerformActionOnChildren(child => Object.Destroy(child));
        }

        /// <summary>
        /// Enable all children of the specified transform.
        /// | Credits: <see href="https://www.youtube.com/@git-amend">git-amend</see>
        /// </summary>
        /// <param name="parent">The parent transform of the children to enable.</param>
        public static void EnableChildren(this Transform parent) {
            parent.PerformActionOnChildren(child => child.gameObject.SetActive(true));
        }

        /// <summary>
        /// Disable all children of the specified transform.
        /// | Credits: <see href="https://www.youtube.com/@git-amend">git-amend</see>
        /// </summary>
        /// <param name="parent">The parent transform of the children to disable.</param>
        public static void DisableChildren(this Transform parent) {
            parent.PerformActionOnChildren(child => child.gameObject.SetActive(false));
        }

        private static void PerformActionOnChildren(this Transform parent, System.Action<Transform> action) {
            for (int i = parent.childCount - 1; i >= 0; i--) {
                action(parent.GetChild(i));
            }
        }
    }
}