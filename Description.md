# Overview

For a 2D Unity Project, I needed a really efficient way to get the intersection points between a line segment (as defined by two `Vector2`) and an axis-aligned `Rect` in 2D space, where the intersection points are returned as a parametric fraction of the length of the original line segment:

![](https://i.imgur.com/XL6NVkd.gif)

_Note how when the line intersects the rect, the green line represents the portion inside the rect, and the parametric representation of the point of intersection along the line is displayed_

In my use case the vast majority of lines are *either* completely inside *or* completely outside the rect, and so I needed an approach that was very efficient in cases where there is guaranteed no intersection and avoids unecessary raycasts when possible.

I also needed both the point of entry and point of exit.


# Approach

We spacially partition the area around a rect into sectors, and given what sectors the line segment starts and ends in, we can can use a pre-computed lookup table to know what edges of the Rect should be raycast.

This minimizes the actual raycasts preformed significantly and is very efficient in cases where line segments are often completely inside or compltely outside the rect.

# Details

We divide the Rect into the following sectors:
```
S0| S1 |S2
--+----+--
S3| S4 |S5
--+----+--
S6| S7 |S8
```
Where S4 is the Rect itself.

Given the sectors of the start and end point of a line segment, and whether we are checking for entering or exiting the Rect, we know what Raycasts could possibly need to be performed.

For example:
- From **Sector 0** to **Sector 1**
  - `Enter`/`Exit`: Can never hit the rect 
  - `Result`: **0** raycasts.
- From **Sector 4** to **Sector 7**: 
  - `Enter`: Already inside the rect
  - `Exit`: Can only hit edge **S7**
  - `Result`: **1** raycast.
- From **Sector 6** to **Sector 5**
  - `Enter`: Can hit either edge **S3** *or* **S7** 
  - `Exit`: Can only hit edge **S5**
  - `Result`: **2 or 3** raycasts.

The necessary raycasts for all possible permutations of (1)Start Sector, (2)End Sector and (3)if we are checking for entering or exiting the rect are pre-computed in a static lookup table of `9 * 9 * 2 = 162` elements, which I have done offline.

That means at runtime, we determine the Sector of the start and end point, then perform only the raycasts necessary for entering and exiting the rect as determined by the lookup table. Since raycasting the sides of the Rect is the most computationally intensive part, this small bit of pre-processing saves time overall, especially in use cases where lots of line segments are tested but the majority are either completely inside or completely outside of the Rect.

The raycasts between the line segment and the sides of the Rect are also optimized to either solve for a horizontal or a vertical side intersection, since the sides of the rect are always axis aligned.

-----

# Notes
- This approach is specifically optimized for cases where the majority of line segments tested don't intersect with the Rect, and either lie completely inside or completely outside. If intersections were much more common then a brute force approach of testing the 4 sides until 2 hits are found might be marginally faster.
- If you only need the point of entry you could modify the lookup table to only store that, and simply return after that first raycast pass.
- This approach returns the parametric representation of the intersection and not a specific point. That is because in my use-case I actually don't need the point, but rather the fraction along the line where it intersects, which I then use to perform further calculations on. It also makes handling non-intersections easier.
- While I did some preliminary benchmarking during testing I don't feel it's extensive enough to warrant including here. Benchmarking was performed against the approach listed here https://stackoverflow.com/a/38944633/928062 which only returns the entry point.

# References
- https://ncase.me/sight-and-light/ : 2D Lighting tutorial that does a good job of explaining how to derive the parametric equation for line intersections. 
  - Note that their final `T1 = ...` formula can divide by zero. See the issue here: https://github.com/ncase/sight-and-light/issues/3