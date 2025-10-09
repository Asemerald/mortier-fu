using UnityEngine;

namespace MortierFu.Shared
{
    public static class Vector3Extensions {
        /// <summary>
        /// Give a new vector with each component if specified during method call.
        /// | Credits: <see href="https://www.youtube.com/@git-amend">git-amend</see>
        /// </summary>
        /// <param name="vector">The base vector.</param>
        /// <param name="x">The x component of the new vector.</param>
        /// <param name="y">The y component of the new vector.</param>
        /// <param name="z">The z component of the new vector.</param>
        /// <returns>A new constructed Vector from the given vector with the provided x, y and z component.</returns>
        public static Vector3 With(this Vector3 vector, float? x = null, float? y = null, float? z = null) {
            return new Vector3(x ?? vector.x, y ?? vector.y, z ?? vector.z);
        }

        /// <summary>
        /// Give the vector
        /// | Credits: <see href="https://www.youtube.com/@git-amend">git-amend</see>
        /// </summary>
        /// <param name="vector">The base vector.</param>
        /// <param name="x">The value to add to the x component of the vector.</param>
        /// <param name="y">The value to add to the y component of the vector.</param>
        /// <param name="z">The value to add to the z component of the vector.</param>
        /// <returns>A new vector made from the specified vector with the added values of each specified components.</returns>
        public static Vector3 Add(this Vector3 vector, float? x = null, float? y = null, float? z = null) {
            return new Vector3(vector.x + (x ?? 0), vector.y + (y ?? 0), vector.z + (z ?? 0));
        }
    }
}