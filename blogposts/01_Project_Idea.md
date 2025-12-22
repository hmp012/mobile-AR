## Introduction to Our Mobile AR Project

For our mobile AR project, we explored several different ideas, but the final decision was to develop a **wayfinding application for the Horsens train station**.

### Technologies Used

Unlike other teams, developing the app in Swift was considered as an option. This was considered because one of us had not had any experience with Unity before, and Hugo had some experience in Swift from work. Moreover, developing natively improves performance. However, the team decided to go with Unity instead for the following reasons:

- Hardware limitation: Swift apps can only run on iOS devices. None of the team members had an iPhone as a personal device, only Hugo could get one leased from work. However, this lease had some restrictions and the team could not get development stranded due to hardware limitation issues. Moreover, to develop for Swift, a macOS device is required. While Hugo had a mac, this was not the case for Kateryna, so it was't a viable alternative.

- SwiftUI vs UIKit: While Hugo had experience with Swift. He has been developing on SwiftUI, Apple's most recent UI framework. However, ArKit and RealityKit (Apple's APIs for AR development) are both built on top of UIKit, an older framework also developed by Apple. This meant that most of the advantage of having one person with experience in Swift was lost.

- Documentation available: Apple's framework suffers from scarce official documentation and, most of the times outdated unofficial documentation. On the other hand, Unity has amazing official docs, combined with a growing community, which make development a bit easier.

All of these points were considered to be major stoppers, and ended tilting the balance towards **Unity 6.2**.

### The idea: TrainFinder

The Technolgies Used were settled, but we had no previous experience with Unity or AR development. This meant that we did not fully understand the total scope of the project at the begining, which cased major issues during the implementation phase. However, we were enthusiastic and motivated to create something meaningful for the **Horsens community** and for the **international VIA University College students**.

Because the Horsens train station has a medium size, it can be confusing for newcomers, which is why we wanted to implement a system we call **"TrainFinder"**.

The purpose of TrainFinder is to help users quickly and easily find their correct platform using augmented reality guidance. Our idea was to let the user **localize itself** by scanning specific pictures located inside the station, and then show a path in the device, showing the users which steps to take in order to. To do this, the device would use the camera and sensors to track the user's position in real time, updating the path accordingly.

## XR terminology (AR/VR/AV/MR/XR)

During the project we often used “AR” and “XR” interchangeably, but they are not the same:

- **XR (Extended Reality)** is the umbrella term for immersive technologies.
- **AR (Augmented Reality)** overlays virtual content on the real world (our project).
- **VR (Virtual Reality)** replaces the real world with a fully virtual environment.
- **MR (Mixed Reality)** is AR with stronger spatial understanding, so virtual content behaves as if it is part of the physical space (anchoring, occlusion, interaction with surfaces).
- **AV (Augmented Virtuality)** is closer to VR but brings real-world elements into the virtual scene (e.g., passthrough video or tracked real objects inside a virtual environment).

## How TrainFinder adds value to the end user

The main value is practical: **reduce confusion and speed up decisions** in a space that can be stressful and time-critical.

- Less “map reading” and interpretation of signage, especially for newcomers.
- Faster path selection under pressure (e.g., short transfer times).
- Potential accessibility benefits if expanded later (step-free routes, clearer cues, multimodal guidance).

## Why XR helps in this case (what AR adds vs a normal app)

A normal 2D map can tell you what to do, but AR can show it **in the same space where you are acting**.

- **In-context guidance:** the route appears where you walk, not on an abstract diagram.
- **Reduced cognitive load:** less mental work converting “map space” into “real space”.
- **Better orientation for non-locals:** the user can keep attention on the environment while still receiving guidance.

## Unique use cases for this application pattern

Even though TrainFinder is limited to Horsens station for this project, the same approach applies to:

- Indoor navigation in stations, airports, hospitals, campuses, malls.
- Event venues (temporary wayfinding that changes day-to-day).
- “Last-meter” navigation: guiding to a specific entrance, platform section, meeting point, or service desk.

### Going beyond the MVP

A simple project would just show a dropdown for the user to select which platform to head towards. However, we also carried some investigation and found some interesting APIs that could enhance the User Experience. For starters, the platform in which a train would arrive is publicly available in the [Rejseplanen API](https://labs.rejseplanen.dk/hc/en-us), so there are several alternatives for the user to select a platform:
- Input Train number: This will autmatically select a platform and start navigation
- Input destination: A new dropdown will show all trains heading towards the inputted destination

Furthermore, we also found out that the information about where each wagon will be placed in the platform is also available through [Mittog](mittog.dk). Which means that if the user had a selected seat, the app could guide the user up until the specific wagon.

Finally, this project is restricted to Horsens station, but it shouldn't be hard to expand to other stations in Denmark on in the world, as it would only require a 3d map of the surfaces of the station.

### Version Control

Our project uses Git for version control, with GitHub as the remote repository.  
We also use Google drive to handle large files such as 3D models and textures.

### Resources
- [Rejseplanen API](https://labs.rejseplanen.dk/hc/en-us)
- [Mittog](mittog.dk)
- [RealityKit](https://developer.apple.com/augmented-reality/realitykit/)
- [ARKit](https://developer.apple.com/augmented-reality/arkit/)


Authors: Hugo & Kateryna