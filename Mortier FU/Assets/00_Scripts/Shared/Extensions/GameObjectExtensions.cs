using UnityEngine;

namespace MortierFu.Shared
{
    public static class GameObjectExtensions {
        public static T GetOrAdd<T>(this GameObject go) where T : Component {
            T component = go.GetComponent<T>();
            if (!component) component = go.AddComponent<T>();

            return component;
        }

        /// <summary>
        /// Returns the object itself if it exists, null otherwise.
        /// | Credits: <see href="https://www.youtube.com/@git-amend">git-amend</see>
        /// </summary>
        /// <remarks>
        /// This method helps differentiate between a null reference and a destroyed Unity object. Unity's "== null" check
        /// can incorrectly return true for destroyed objects, leading to misleading behaviour. The OrNull method use
        /// Unity's "null check", and if the object has been marked for destruction, it ensures an actual null reference is returned,
        /// aiding in correctly chaining operations and preventing NullReferenceExceptions.
        /// </remarks>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="obj">The object being checked.</param>
        /// <returns>The object itself if it exists and not destroyed, null otherwise.</returns>
        public static T OrNull<T>(this T obj) where T : Object => obj ? obj : null;

        /// <summary>
        /// Destroy all children of the specified gameobject.
        /// | Credits: <see href="https://www.youtube.com/@git-amend">git-amend</see>
        /// </summary>
        /// <param name="parent">The parent gameobject of the children to destroy.</param>
        public static void DestroyChildren(this GameObject gameObject) {
            gameObject.transform.DestroyChildren();
        }

        /// <summary>
        /// Enable all children of the specified gameobject.
        /// | Credits: <see href="https://www.youtube.com/@git-amend">git-amend</see>
        /// </summary>
        /// <param name="parent">The parent gameobject of the children to enable.</param>
        public static void EnableChildren(this GameObject gameObject) {
            gameObject.transform.EnableChildren();
        }

        /// <summary>
        /// Disable all children of the specified gameobject.
        /// | Credits: <see href="https://www.youtube.com/@git-amend">git-amend</see>
        /// </summary>
        /// <param name="parent">The parent gameobject of the children to disable.</param>
        public static void DisableChildren(this GameObject gameObject) {
            gameObject.transform.DisableChildren();
        }
    }
}
