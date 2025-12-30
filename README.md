# 🚆 AR Train Finder Navigator

This project is an Augmented Reality (AR) navigation system designed to help passengers in Horsens easily find the correct **train platform and route to it**.  
Instead of reading confusing signs or maps, the passenger can follow a clear **blue AR navigation line** displayed directly in the real world.

The goal of the system is to make train station navigation **simple, intuitive, and accessible** for all passengers, including tourists or people unfamiliar with the station layout.

---

## 📲 How the Application Works

1. **User opens the app**
   - AR session starts
   - XROrigin initializes

2. **The app detects a reference point security camera image**
   - ARTrackedImageManager
   - AR Anchors

3. **User selects a destination**
   - Example: Platform 1, Platform 2 or Platform 3. 
 

4. **The app calculates the best walking route**
   - NavMesh
   - Pathfinding
   - Waypoints

5. **A blue AR guidance line appears on the floor**
   - Guidance line rendering
   - Direction arrows
   - Occlusion handling

6. **User follows the path**
   - The line updates as the user moves
   - Real-time feedback

7. **User reaches the selected platform**

---

## ✨ Main Features

- AR blue navigation line showing the walking path  
- Real-time pathfinding and route adjustment  
- Image-tracking or anchor-based indoor positioning  
- Realistic occlusion (line goes behind real objects)  
- Clean and simple UI for destination selection  
- Accurate step-by-step station navigation  

---

## 🎯 Project Purpose

Train stations can be confusing and stressful, especially during busy hours.  
This project aims to improve the passenger experience by offering:

- Faster navigation  
- Less confusion  
- Support for people who don’t know the local language  
- A modern, smart-station digital experience  

---

## 📁 Video Demonstration

A video demonstration of the AR navigation system.  
[Watch Demo](blogposts/DemoVideoLink.md)


## 📝 Development Blogs
- [Blog 1 – Project Idea](blogposts/01_Project_Idea.md)
- [Blog 2 – Project Learning & Implementation](blogposts/02_Project_Learning+Impl.md)
- [Blog 3 – Testing, Performance & Delivery](blogposts/03_Testing+Performance+Delivery.md)
- [Blog 4 – VR Introduction](blogposts/04_VR_Intro.md)
- [Blog 5 – VR Implementation](blogposts/05_VR_Implementation.md)
- [Blog 6 – VR UX Improvements](blogposts/06_VR_UX_improvements.md)

## Tools Used
- [Unity](https://unity.com/)
- [3D Scanner App](https://3dscannerapp.com/)
- [Blender](https://www.blender.org/)


## Code References

Here are the resources used to create this project.
- [Unity NavMesh Tutorial](https://learn.unity.com/tutorial/unity-navmesh#5c7f8528edbc2a002053b498)
- [Guidance Line (Unity Asset Store)](https://assetstore.unity.com/packages/tools/game-toolkits/guidance-line-303873)





