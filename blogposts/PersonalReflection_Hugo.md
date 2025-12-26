# Personal Reflection – Hugo

### Main Contributions
*   **AR Project (TrainFinder):** Conducted the feasibility analysis (Swift vs Unity), executed the LiDAR scanning and optimization pipeline, implemented the NavMesh baking and Agent tuning, and developed the programmatic LineRenderer for wayfinding.
*   **VR Project (Tower of Hanoi):** Designed the core Game Architecture (GameManager, SnapZone, Donut inheritance), implemented the Interaction Logic (Snap/Release validation), and built the UI/UX flow (Spatial Panels, Tutorials).

Looking back through this semester... what a journey it has been. Bravely enough, I chose XRD as one of the subjects for my last semester without having any experience with Unity. I wasn't aware at that time that the experience with Unity was expected and not something we were going to learn during the course. This turned out to be the biggest challenge in the whole semester.

On the first lesson, I realized I didn't know that many students, which meant I ended up with Kateryna, which she also didn't have any experience with Unity before.

For the first project, nothing really went well. I investigated whether the project would be viable in Swift, as I had plenty of experience with regular Swift apps. However, we ended up decided to go with Unity, where none of us had any experince. We were both lost and I was really frustrated because I wasn't understanding how the framework worked at all.

**Reflecting on this decision, while Swift (ARKit/RealityKit) would have offered superior performance and native OS integration, Unity provided a higher level of abstraction through the XR Interaction Toolkit. In theory, this abstraction layer allows for rapid prototyping across devices, but the steep learning curve of the component-based architecture (GameObjects, MonoBehaviours) proved to be a significant bottleneck compared to the imperative programming style I was used to.**

We tried following tutorials, reading documentation, using AI...

One of the tutorials ended up with us breaking the XrOrigin without even knowing. Since we were not fully aware at that time what was going wrong, nobody was having similar issues online, and AI models were not helping at all. I felt completely lost during those weeks, specially since I couldn't count on my teammate for help. Far too much time was lost because of this. **The issue stemmed from a fundamental misunderstanding of the XR rig's coordinate space—specifically how the camera offset interacts with the tracked device position. This technical oversight halted our progress because we couldn't visualize the virtual content in the correct physical location.**

The moment we realized the issue with the XrOrigin, it was too late to solve all the other issues we couldn't really take care of because of it.

However, for the second project, things looked a bit more bright. I still couldn't count on my teammate, but thanks to some explanations by Kasper, it allowed me to finally understand basic concepts about Unity, which allowed me to progress much faster. For this project, we decided to scope it down, to have something working that we could at least show.

I enjoyed much more the development of the second project. **I took full ownership of the core game logic, implementing the `GameManager` and the inheritance structure for `SnapZones` and `Donuts`. This architectural decision allowed us to decouple the game state from the interaction logic.** However, I think, focusing on the requirement of "Making something useful", made us de-rail a bit from the VR perspective, as we focused too much on making the rules work, while only playing around with interactables.

**From a theoretical standpoint, we prioritized "pragmatic usability" over "immersive presence". By enforcing strict rules (SnapZones) and using 2D-style UI panels, we reduced the cognitive load for the user but limited the "magical" affordances unique to VR, such as unconstrained physics-based manipulation. We essentially built a 2D game logic inside a 3D space, rather than a fully spatial experience.**

**Technically, the biggest hurdle in the second project was the non-deterministic nature of Unity's physics engine. Issues like the "sinking ring" (due to gravity/collider interpenetration) highlighted the complexity of simulating solid objects in a virtual space. If I were to redo this, I would rely less on Unity's default physics for stacking and instead implement a kinematic approach for the rings to ensure stability, trading physical realism for predictable behavior.**

In the overall perspective, I think the project was a great way to learn about Unity, which, once you are familiar with the framework, has a series of built-in scripts that, combined with OpenXR, make XR development much the development much easier.

It is important to note, Unity's strenght is not performance, but rather developer speed. It's a system which is not super optimized for any OS. However having the possibility to run on so many different platforms and how many pre configured scripts are available for the developer, specially for XR, make the development process much faster.

To sum up, I struggled more with Unity than with XR applications. Unfortunately, I spent far too much time getting to know the framework. This meant that with an extended deadline, we would have been able to devilver a working product, as we show in the blogs a clear path towards solving the current issues.