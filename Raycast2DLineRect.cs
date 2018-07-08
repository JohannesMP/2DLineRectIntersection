using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Utils
{
    /// <summary>
    /// Find the 0, 1 or 2 intersections between a line segment and an axis-aligned Rect.
    /// </summary>
    /// Uses spacial partitioning of the rect to perform the bare minimum raycasts necessary.
    public static class Raycast2DLineRect
    {
        /// <summary>
        /// Fraction of the line segment where it enters and exits the rect..
        /// </summary>
        public struct LineRectResult
        {
            /// <summary>
            /// The fraction along the line segment where it entered the rect.
            /// 0 if starting inside the rect.
            /// float.PositiveInfinity if no hits (entry or exit).
            /// </summary>
            public float t_entry;
            /// <summary>
            /// The fraction along the line segment where it exited the rect.
            /// 1 if ending inside the rect.
            /// float.PositiveInfinity if no hits (entry or exit).
            /// </summary>
            public float t_exit;

            public bool HaveHit { get { return t_entry != float.PositiveInfinity; } }
            public override string ToString() { return string.Format("t_entry={0}, t_exit={1}", t_entry, t_exit); }
        }

        /// <summary>
        /// Sectors around a rect
        /// </summary>
        /// Given a rectangle we divide the space around it into the following Sectors (S4 is the rectangle itself):
        ///      S0| S1 |S2
        ///      --+----+--
        ///      S3| S4 |S5
        ///      --+----+--
        ///      S6| S7 |S8
        ///
        /// Note that Sector enums are also used to identify the Side or Corner of the rectangle bordered by a sector
        enum Sector
        {
            __, // No sector - underscore used to make viewing RaycastLookup below easier
            S0, S1, S2,
            S3, S4, S5,
            S6, S7, S8
        }

        /// <summary>
        /// Pre-computed lookup to quickly know what raycasts to perform, given the sector that a line segment's start and end point are in.
        /// </summary>
        /// This pre-computed lookup tables enables us to bypass lots of potential conditionals.
        /// 
        /// Lookup is done as follows: [Sector1, Sector2, state(0:1)]
        ///     - state == 0: the line segment is entering the Rect
        ///     - state == 1: the line segment is exiting the Rect
        ///
        /// - If the result is Sector.__ a raycast would not return anything.
        /// - Otherwise the return enum indicates which Edge(s) should be raycast
        ///     - ex: S0 means that the edges that create the corner at sector 0 should be raycast, meaning Edge 1 and Edge 3.
        ///
        /// Examples: 
        ///     RaycastLookup[1,4,0] == S1
        ///         - A line segment from Sector 1 to 4 enters the rect at Edge 1
        ///     RaycastLookup[1,4,1] == __
        ///         - A line segment from Sector 1 to 4 does not exit the rect
        ///         
        ///     RaycastLookup[0,5,0] == S0
        ///         - A line segment from Sector 0 to 5 enters the rect at the Edges touching corner 0 (Edge 1 and Edge 3)
        ///     RaycastLookup[0,5,1] == S5
        ///         - A line segment from Sector 0 to 5 exits the rect at Edge 5
        static Sector[,,] RaycastLookup =
        {
            // From 0 - top left corner
            {
                {Sector.__, Sector.__},     {Sector.__, Sector.__},     {Sector.__, Sector.__},
                {Sector.__, Sector.__},     {Sector.S0, Sector.S4},     {Sector.S0, Sector.S5},
                {Sector.__, Sector.__},     {Sector.S0, Sector.S7},     {Sector.S0, Sector.S8},
            },
            // From 1 - top edge
            {
                {Sector.__, Sector.__},     {Sector.__, Sector.__},     {Sector.__, Sector.__},
                {Sector.S1, Sector.S3},     {Sector.S1, Sector.S4},     {Sector.S1, Sector.S5},
                {Sector.S1, Sector.S6},     {Sector.S1, Sector.S7},     {Sector.S1, Sector.S8},
            },
            // From 2 - top right corner
            {
                {Sector.__, Sector.__},     {Sector.__, Sector.__},     {Sector.__, Sector.__},
                {Sector.S2, Sector.S3},     {Sector.S2, Sector.S4},     {Sector.__, Sector.__},
                {Sector.S2, Sector.S6},     {Sector.S2, Sector.S7},     {Sector.__, Sector.__},
            },
            // From 3 - left edge
            {
                {Sector.__, Sector.__},     {Sector.S3, Sector.S1},     {Sector.S3, Sector.S2},
                {Sector.__, Sector.__},     {Sector.S3, Sector.S4},     {Sector.S3, Sector.S5},
                {Sector.__, Sector.__},     {Sector.S3, Sector.S7},     {Sector.S3, Sector.S8},
            },
            // From 4 - center; no entry, only exit
            {
                {Sector.S4, Sector.S0},     {Sector.S4, Sector.S1},     {Sector.S4, Sector.S2},
                {Sector.S4, Sector.S3},     {Sector.S4, Sector.S4},     {Sector.S4, Sector.S5},
                {Sector.S4, Sector.S6},     {Sector.S4, Sector.S7},     {Sector.S4, Sector.S8},
            },
            // From 5 - right edge
            {
                {Sector.S5, Sector.S0},     {Sector.S5, Sector.S1},     {Sector.__, Sector.__},
                {Sector.S5, Sector.S3},     {Sector.S5, Sector.S4},     {Sector.__, Sector.__},
                {Sector.S5, Sector.S6},     {Sector.S5, Sector.S7},     {Sector.__, Sector.__},
            },
            // From 6 - bottom left corner
            {
                {Sector.__, Sector.__},     {Sector.S6, Sector.S1},     {Sector.S6, Sector.S2},
                {Sector.__, Sector.__},     {Sector.S6, Sector.S4},     {Sector.S6, Sector.S5},
                {Sector.__, Sector.__},     {Sector.__, Sector.__},     {Sector.__, Sector.__},
            },
            // From 7 - bottom edge
            {
                {Sector.S7, Sector.S0},     {Sector.S7, Sector.S1},     {Sector.S7, Sector.S2},
                {Sector.S7, Sector.S3},     {Sector.S7, Sector.S4},     {Sector.S7, Sector.S5},
                {Sector.__, Sector.__},     {Sector.__, Sector.__},     {Sector.__, Sector.__},
            },
            // From 8 - bottom right corner
            {
                {Sector.S8, Sector.S0},   {Sector.S8, Sector.S1},   {Sector.__, Sector.__},
                {Sector.S8, Sector.S3},   {Sector.S8, Sector.S4},   {Sector.__, Sector.__},
                {Sector.__, Sector.__},   {Sector.__, Sector.__},   {Sector.__, Sector.__},
            },
        };

        /// <summary>
        /// Check if a given line segment intersects an axis-aligned Rect
        /// </summary>
        public static void RaycastLineRect(Vector2 begin, Vector2 end, Rect rect, ref LineRectResult res)
        {
            Vector3 dir = end - begin;

            int sectorBegin = GetRectPointSector(rect, begin);
            int sectorEnd   = GetRectPointSector(rect, end);

            res.t_entry = GetRayToRectSide(begin, dir, RaycastLookup[sectorBegin, sectorEnd, 0], rect, 0f);
            res.t_exit  = GetRayToRectSide(begin, dir, RaycastLookup[sectorBegin, sectorEnd, 1], rect, 1f);
        }

        /// <summary>
        /// Check in which sector relative to a rect a point is located in.
        /// </summary>
        /// 
        /// Return value maps to the rect as follows, where 4 is inclusive:
        ///     0| 1 |2     
        ///     -+---+-   ^
        ///     3| 4 |5   |
        ///     -+---+-   y
        ///     6| 7 |8    x-->
        /// 
        /// <returns>The sector relative to the rect that the point is in</returns>
        private static int GetRectPointSector(Rect rect, Vector2 point)
        { 
            // 0, 1 or 2
            if(point.y > rect.yMax)
            {
                if      (point.x < rect.xMin) return 0;
                else if (point.x > rect.xMax) return 2;
                return 1;
            }
            // 6, 7, or 8
            else if(point.y < rect.yMin)
            {
                if      (point.x < rect.xMin) return 6;
                else if (point.x > rect.xMax) return 8;
                return 7;
            }
            // 3, 4 or 5
            else
            {
                if      (point.x < rect.xMin) return 3;
                else if (point.x > rect.xMax) return 5;
                return 4;
            }
        }

        /// <summary>
        /// Perform the raycast on an edge or two edges on a corner of a rect.
        /// </summary>
        /// Edges are a guaranteed single raycast
        /// Corners consist of two edges to raycast. if the first is a miss, return the second.
        /// 
        /// <param name="side">The Direction(s) of the rect to raycast against</param>
        /// <param name="r4val">The intersect value to return for when we start or stop in the rect</param>
        /// <returns>The fraction along the ray</returns>
        private static float GetRayToRectSide(Vector2 begin, Vector2 dir, Sector side, Rect rect, float r4val)
        {
            float curFrac;
            switch (side)
            {
                // Raycast Corner S0 - consists of S1 and S3
                case Sector.S0:
                    curFrac = GetRayToRectSide(begin, dir, Sector.S1, rect, r4val);
                    return (curFrac == float.PositiveInfinity) ? GetRayToRectSide(begin, dir, Sector.S3, rect, r4val) : curFrac;

                // Raycast Edge S1 - Horizontal
                case Sector.S1: return RayToHoriz(begin, dir, rect.xMin, rect.yMax, rect.width);

                // Raycast Corner S2 - consists of S1 and S5
                case Sector.S2:
                    curFrac = GetRayToRectSide(begin, dir, Sector.S1, rect, r4val);
                    return (curFrac == float.PositiveInfinity) ? GetRayToRectSide(begin, dir, Sector.S5, rect, r4val) : curFrac;

                // Raycast Edge S3 - Vertical
                case Sector.S3: return RayToVert(begin, dir, rect.xMin, rect.yMin, rect.height);

                // Center S4 - return the intersection value provided by caller
                case Sector.S4: return r4val;

                // Raycast Edge S5 - Vertical
                case Sector.S5: return RayToVert(begin, dir, rect.xMax, rect.yMin, rect.height);

                // Raycast Corner S6 - consists of S3 and S7
                case Sector.S6:
                    curFrac = GetRayToRectSide(begin, dir, Sector.S3, rect, r4val);
                    return (curFrac == float.PositiveInfinity) ? GetRayToRectSide(begin, dir, Sector.S7, rect, r4val) : curFrac;

                // Raycast Edge S7 - Horizontal
                case Sector.S7: return RayToHoriz(begin, dir, rect.xMin, rect.yMin, rect.width);

                // Raycast Corner S8 - consists of S5 and S7
                case Sector.S8:
                    curFrac = GetRayToRectSide(begin, dir, Sector.S5, rect, r4val);
                    return (curFrac == float.PositiveInfinity) ? GetRayToRectSide(begin, dir, Sector.S7, rect,  r4val) : curFrac;

                // No Intersection
                default: return float.PositiveInfinity;
            }
        }

        /// <summary>
        /// Check if a given line segment and a strictly Horizontal line segment intersect. float.PositiveInfinity if no hit.
        /// </summary>
        /// /// <param name="width">The width of the horizontal line segment</param>
        /// <returns>The parametric fraction where the intersection lies on the 'from' segment. float.PositiveInfinity if no intersection</returns>
        private static float RayToHoriz(Vector2 fromPoint, Vector2 fromDir, float x, float y, float width)
        {
            // 1. Check if the extended line could intersect the segment
            float fromParam = (y - fromPoint.y) / fromDir.y;
            if (fromParam < 0 || fromParam > 1)
                return float.PositiveInfinity;
            // 2. Check if line could reach the segment
            float lineParam = (fromPoint.x + fromDir.x * fromParam - x) / width;
            if (lineParam < 0 || lineParam > 1)
                return float.PositiveInfinity;
            return fromParam;
        }

        /// <summary>
        /// If a given line segment and a strictly Vertical line segment intersect. float.PositiveInfinity if no intersection.
        /// </summary>
        /// <param name="height">The height of the vertical line segment</param>
        /// <returns>The parametric fraction where the intersection lies on the 'from' segment. float.PositiveInfinity if no intersection</returns>
        private static float RayToVert(Vector2 fromPoint, Vector2 fromDir, float x, float y, float height)
        {
            float fromParam = (x - fromPoint.x) / fromDir.x;
            // 1. Check if the extended line could intersect the segment
            if (fromParam < 0 || fromParam > 1)
                return float.PositiveInfinity;
            // 2. Check if line could reach the segment
            float lineParam = (fromPoint.y + fromDir.y * fromParam - y) / height;
            if (lineParam < 0 || lineParam > 1)
                return float.PositiveInfinity;
            return fromParam;
        }

        /// <summary>
        /// Check if a given 'from' line segment and a 'to' line segment intersect. float.PositiveInfinity if no intersection.
        /// </summary>
        /// Just for reference - RayToHoriz and RayToVert are specialized versions of this.
        /// <returns>The parametric fraction where the intersection lies on the 'from' segment. float.PositiveInfinity if no intersection</returns>
        private static float RayToRay(Vector2 fromPoint, Vector2 fromDir, Vector2 toPoint, Vector2 toDir)
        {
            // 1. Check where the extended 'to' line segment would intersect the 'from' line segment.
            float fromParam = (toDir.x * (fromPoint.y - toPoint.y) + toDir.y * (toPoint.x - fromPoint.x)) / (fromDir.x * toDir.y - fromDir.y * toDir.x);
            if (fromParam < 0 || fromParam > 1)
                return float.PositiveInfinity;

            // 2. Now check if the extended 'from' line segment would intersect the 'to' line segment.
            float toParam = (fromDir.x * (toPoint.y - fromPoint.y) + fromDir.y * (fromPoint.x - toPoint.x)) / (toDir.x * fromDir.y - toDir.y * fromDir.x);
            if (toParam < 0 || toParam > 1)
                return float.PositiveInfinity;

            return fromParam;
        }
    }
}

