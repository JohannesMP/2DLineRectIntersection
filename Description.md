I needed a really efficient way to get the intersection points between a line segment (as defined by two `Vector2`) and an axis-aligned `Rect` in 2D space, where the intersection points are a parametric fraction of the length of the original line segment:

![](https://i.imgur.com/XL6NVkd.gif)
_note how when the line intersects the rect, the green line represents the portion inside the rect, and the parametric representation of the point of intersection along the line is displayed_

In my use case the vast majority of lines are either completely inside or completely outside the rect, and so I needed an approach that gave both the point of entry and point of exit, but was very efficient in cases where there is guaranteed no intersection.

To solve this we Divide the Rect into the followign sectors:
```
S0| S1 |S2
--+----+--
S3| S4 |S5
--+----+--
S6| S7 |S8
```
Where S4 is the Rect itself.

For a given set of sectors and whether we are checking for entering or exiting the Rect, we know what Raycasts need to be performed.

For example:
- From **Sector 0** to **Sector 1**
  - `Enter`/`Exit`: Can never hit the rect 
  - `Result`: **0** raycasts.
- From **Sector 4** to **Sector 7**: 
  - `Enter`: Already inside the rect
  - `Exit`: Can only hit edge **S7**
  - `Result`: **1** raycast.
- From **Sector 6** to **Sector 5**
  - `Enter`: Can hit either edge **S6** *or* **S6** 
  - `Exit`: Can only hit edge **S5**
  - `Result`: **2 or 3** raycasts.

All possible permutations of Start and End Sector and if we are checking for entering or exiting the rect is pre-computed in a static lookup table of `9 * 9 * 2 = 162` elements.

That means at runtime, we determine the Sector of the start and end point, then perform the raycasts for entering and exiting the rect as determined by the lookup table.

The raycasts between the line segment and the sides of the Rect are also optimized to either solve for a horizontal side intersection or a vertical, since the sides of the rect are always axis aligned.


Note that this approach is specifically optimized for cases where intersections are rare. If intersections were much more common then a brute force approach of testing the 4 sides until 2 hits are found would be marginally faster.