## Testing, Performance & Delivery

This final phase of the project was mostly about validating that the experience worked *outside the Unity Editor* and understanding why some devices performed significantly better than others.

Our biggest performance findings came from two places:

1. **Device constraints (Android)** — the same build felt noticeably worse on our Android test device than on an **iPhone 15**.
2. **3D scan complexity** — the scanned station model was heavier than necessary for our use case.


## Testing on Real Devices

### Cross-device differences
Even with the same Unity project and settings, AR performance differed drastically between our test phones:

- On the **iPhone 15**, tracking and rendering stayed smooth and responsive.
- On our **Android device**, the app felt heavier: frame drops were more common and the experience degraded faster over time.

This wasn’t one single bug — it was the combined cost of:

- running AR tracking (camera feed + pose estimation)
- rendering the scene on top of the camera background
- loading and drawing a large scanned mesh

The biggest takeaway: in mobile AR, “it runs on my phone” is not enough. You need to test on the *weakest device you expect users to have*.


## Performance Issues We Observed

### 1) Android performance and stability
On Android, we saw several symptoms that pointed to limited headroom:

- **Lower and less stable FPS** (more visible stutter)
- **Slower iteration after launch** (initial loading felt heavier)
- **Performance getting worse after some time**, which is consistent with phones heating up and throttling CPU/GPU

In practice, AR workloads are already expensive; if the device is mid-range or a few years old, adding heavy 3D content can easily push it over the edge.

### 2) The scanned 3D model was too complex for the goal
Our LiDAR scan was great for capturing the station geometry, but the raw mesh contained a lot of detail we didn’t actually need for navigation.

For TrainFinder, the navigation system mainly needed **walkable surfaces**:

- floors
- stairs/ramps (if used)

Everything else (walls, ceiling, decorative structures, small objects) was visually interesting, but not required for our AR wayfinding line.

Because we imported a more complete mesh than necessary, we paid for it in multiple ways:

- **bigger asset size** (longer import/build times and heavier app)
- **more geometry to render** (higher GPU cost)
- **more complexity when baking/using navigation data**


## What We Would Do Differently (If We Had More Time)

### UX improvements we could not deliver (MVP gap)
Originally, our intended MVP user flow was closer to a real wayfinding tool: the user should be able to **select which platform to navigate to** (and potentially choose it by train number or destination, as described in our project idea write-up).

In the final state of the project, we did not reach that MVP experience.

Because of performance constraints (especially on Android) and the time spent trying to stabilize the AR pipeline, the app ended up in a much simpler state:

- after localization, it only shows a **fixed target** in platform 1
- there is no UI for choosing between **multiple targets/platforms**

With a lighter scene (floor-only model) and better tracking stability, we would have been able to invest time into the actual UX: multiple destinations, platform selection, and a more “real app” navigation flow.

### A Blender pass focused on “navigation-only geometry”
The biggest improvement would have been a stricter asset pipeline in Blender where we exported **only what we needed**.

Concretely, we should have produced a “navigation mesh source model” that contained only:

- simplified floor surfaces
- simplified stairs
- no small clutter
- no ceilings / walls unless they were needed as blockers

That would have helped massively because it reduces both:

- **runtime rendering cost** (fewer triangles)
- **project iteration cost** (smaller files, faster imports/builds)

### Why the floor-only approach fits AR navigation
Our AR experience wasn’t trying to be a photorealistic reconstruction of the station — the main visual element was the path guidance.

So, exporting only the floor geometry would have still satisfied the user experience:

- the navigation line would appear correctly aligned
- the scene would remain lightweight
- we’d avoid wasting budget on invisible/irrelevant surfaces


## Practical Lessons Learned

- **AR tracking has a fixed cost**; you must treat everything else (meshes, textures, effects) as a limited budget.
- **Asset optimization matters more than expected** in mobile AR because you’re rendering *and* tracking continuously.
- **The simplest model that supports the feature wins** — in our case, that was “floors and stairs only”.


## Delivery Notes

For delivery, our main goal was to keep the build reproducible and testable:

- build the same scene consistently
- validate localization + navigation flow on real devices
- ensure performance remained acceptable for the target phone

### Localization reliability
One important point for delivery is that **localization didn’t work reliably** in our final build.

Even when the reference image was detected, the alignment/orientation was not consistent enough for a dependable navigation experience. That made it difficult to validate end-to-end wayfinding, because if the starting pose is wrong, the path can look “correct” in the virtual scene but incorrect in the real station.

The root cause was a core AR constraint we underestimated: once the AR session is tracking, **Unity/AR Foundation effectively “owns” the AR camera pose**. In practice, this meant we could not reliably modify the **rotation** of the camera (and in some cases the origin) while tracking was active, because the tracking system would continuously override it.

We initially tried a more dynamic approach where we computed the user’s offset from the tracked image (using the detected image pose/size) and then placed the user accordingly. However, the computed placement was **not consistent enough** across scans and devices, so the “dynamic” method produced visibly different results each time.

Because of that, our final working approaches were essentially fallbacks:

- **Hardcoded transform:** once the image was recognized, we forced a fixed position/rotation that looked acceptable in our test conditions.
- **Place the XR Origin relative to the image:** we positioned the **XR Origin directly in front of the tracked image** and, upon detection, instantiated the “base” object that contained the navigation mesh/world content.

One practical issue that caused a lot of trouble during real-world use was **traversing doors** while the app was running. When we walked through doorways, tracking and alignment often became noticeably less stable (e.g., brief loss of tracking, jitter, or the content appearing offset), which we captured in our demo video: (insert link).

These approaches let us demonstrate the concept, but they are not robust enough for a real deployment where the user must get consistent localization across different distances, angles, and lighting conditions.

If we were to continue this project, performance work would start earlier, the scan-to-Unity pipeline would enforce a strict “mobile AR budget” from day one, and we would treat localization stability as a first-class requirement before building additional UX features.

## XR discussion: challenges, workflows, immersion, and what comes next

### How is our app performing?

Based on our device testing, performance was highly device-dependent:

- **iPhone 15:** the experience felt smooth and responsive most of the time.
- **Android test device:** lower and less stable FPS, heavier initial loading, and performance degradation over time (likely heat/throttling + limited GPU headroom).

The biggest contributors were the constant cost of AR tracking (camera + pose estimation) plus the cost of rendering a large scanned mesh.

### What challenges do advancements in AR face at the moment?

From our project experience, the biggest “real world” challenges are less about rendering and more about robustness:

- **Reliable localization and drift:** keeping virtual content consistently aligned over time and across conditions.
- **Visual conditions:** glare, reflections, motion blur, low light, and textureless surfaces hurt tracking.
- **Cross-device consistency:** sensor quality and compute budget vary widely (especially on Android), so experiences can feel inconsistent.
- **Authoring + maintenance cost:** indoor AR often needs scanning, alignment, anchors, and ongoing updates when the environment changes.
- **UX constraints in public spaces:** people are moving, distracted, and often in a hurry; AR cues must be clear without demanding too much attention.
- **Transitions like doorways:** moving through doors can temporarily degrade tracking because the camera view changes abruptly (occlusion, lighting changes, fewer stable features), which is especially noticeable in indoor navigation.

### How is the Quest workflow different from creating ordinary desktop apps in Unity?

Even though TrainFinder is a mobile AR project, VR headsets like Quest highlight differences that also apply to XR development more generally:

- **Build target and deployment:** you ship to a headset runtime (Android-based on Quest), not a desktop OS; iteration is often “build + install + test in-headset”.
- **Input model:** instead of mouse/keyboard, you design for head + controller/hand tracking, rays/grabs, and XR interaction patterns.
- **Performance budget:** high, stable frame rate is mandatory for comfort; you optimize aggressively (CPU/GPU, draw calls, fill rate, foveation, fixed refresh rates).
- **Rendering expectations:** stereo rendering, late latching/timewarp, and careful UI depth/scale choices become part of the day-to-day workflow.
- **Comfort constraints:** locomotion, acceleration, camera motion, and latency issues matter far more than in desktop apps.

### What technical improvements will we see to VR rendering and display technologies in the future?

Trends that are likely to matter (especially for standalone headsets) include:

- **Eye tracking + foveated rendering:** render high detail only where the user looks (saves GPU).
- **Better reprojection/spacewarp:** smoother motion with less raw rendering cost.
- **Higher-resolution + higher-efficiency displays:** improved optics and panels with better pixels-per-degree.
- **Lower latency pipelines:** tighter sensor-to-photon timing to reduce discomfort.
- **Wider adoption of passthrough MR:** higher-quality cameras + depth sensing to blend virtual content into the real world more convincingly.

### What are important factors to immersion?

Even when visuals are simple, immersion depends on consistency and comfort:

- **Low latency and stable frame rate** (comfort is foundational).
- **Correct spatial alignment:** content must stay anchored; jitter and drift break the illusion fast.
- **Natural interaction:** believable input mappings, readable UI scale, and clear affordances.
- **Audio cues:** spatial sound can improve presence and reduce the need to look at UI.
- **Coherent world rules:** physics/occlusion/lighting don’t need to be perfect, but they should be consistent.


Author: Hugo
