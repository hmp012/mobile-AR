# 📘 Blog 4 – Introduction to Our VR Application Project

## 🎯 Moving from AR to VR  
After completing our first project — an AR navigation system for Horsens St. — we learned a lot about the development process, project scope, and time management.  

While the AR project was exciting, we also realized that the technical complexity and large workload made it challenging to complete within the time available.

For our next project, we decided to choose an idea that is more realistic to implement while still allowing us to learn important XR skills. This led us to develop a small but meaningful VR application.

---

## 🧠 Idea of the project  
For this project, we chose to implement the classic **Tower of Hanoi** puzzle game in Virtual Reality.

The Tower of Hanoi is a well-known logical puzzle that consists of:
- Three towers
- A set of rings (although we decided to call them donuts)
- The goal is to move all rings from the first tower to third tower following the rule that a larger ring cannot be placed on a smaller one.

The game is perfect for VR because it:
- Uses simple 3D objects  
- Is easy to interact with  
- Helps us practice VR mechanics like **grabbing, throwing, interaction, physics, and hand-tracking**  
- Has clear goals and no need for complex animations or environments


## 🛠️ Unity setup and device connection

Setting up the project required installing:

- Unity XR Interaction Toolkit  
- Meta Quest 2 integration packages  
- OpenXR plugins  
- VR Template dependencies  

It also took time to properly **connect the Meta Quest 2 headset** to Unity using the Meta Quest Link

This setup process was a big part of our early work, but it helped us understand the core structure of VR applications.

---

## 💻 Hardware limitations and XRD lab support

We quickly realized that our personal laptops did **not have enough GPU power** to smoothly run the VR application.
Ideally, we were using Unity's Play mode, but due to the lack of power, it froze often or ran extremely slowly during testing.

Because of this, we were very grateful to have access to the **XRD Lab computers**, which have stronger hardware and allowed us to:

- Run the VR project smoothly
- Test interactions in real time
- Work with heavier models and lighting
- Build and deploy to Meta Quest 2 without crashes

This significantly improved our workflow and made development possible.


## 🎮 Why this was the right choice  
After experiencing the large scope of the AR project, we wanted to ensure that our next application was:

- ✔ Manageable within the time limit
- ✔ Technically achievable
- ✔ Useful for learning Unity VR foundations
- ✔ Focused on interaction, hand controllers, and gameplay logic

The VR Tower of Hanoi puzzle lets us learn essential VR development skills **without overwhelming complexity**, while still creating a fun and interactive experience.

---
## 🎨 Inspiration and project setup

The initial inspiration for this VR project came from Unity’s built-in **VR Template**, which provides a basic environment, interaction system, and XR rig setup. After experimenting with the template, we decided to use it as the starting point for our application.

To create a simple and clear environment for the [**Tower of Hanoi**](https://en.wikipedia.org/wiki/Tower_of_Hanoi) puzzle, we searched for a suitable 3D table model and found one on [Fab](https://www.fab.com/search?q=table):

We imported the table model into Unity and used it as the base for placing the three towers and rings.

For the towers we used a cylinder for the base and another cylinder for the tower itself.

For the rings, we chose to use the Torus shape already available from Unity's VR Template and made it a grabbable object so the user could interact with them and we completely froze the rotation, and restricted the movement to the Y and Z axis, so it couldn't be moved in unwanted directions.


### Colliders
One of the main hiccups at the beginning of the project was setting the colliders for both the bases and the rings.

By default, Unity only offers simple shapes like squares or cylinders for the colliders of an object. Worst case scenario, Unity offers the option to use a Mesh collider, with the small detail that it must be a continuous shape, meaning a ring, with a hole in the middle was not an option.

First we tried to manually create several small cylinder colliders that would emulate the shape of the ring itself. 

This option gave the shape a horizontal collider. However, it wasn't giving any vertical collider, which meant that all rings would stack on top of each other, indistinguishable to the user.

In order to solve this, we tried to replicate the same set of colliders stacked in several different heights. However, this turned out to be a complete disaster.
- First we tried with 200 sets of colliders, which resulted in being so heavy for the app, that the headset couldn't handle it.
- Given the poor result, we decided that we could go with 50, but we faced similar issues: while the app would load into the headset, it would not perform at a decent framerate.
- Finally, we tried with just 3 sets of colliders. Now the app could load with no performance issues. However, the ring colliders were not working as expected, stacking on top of each other, suffering from a similar issue as when we just had 1 collider.

Having tried everything with these colliders, we decided to follow a different alternative: the rings would have a mesh collider, but the towers wouldn't. Using this trick, the user wouldn't notice a difference until they figure they can move the rings around the tower with no limitation. This heavily simplified the setup and translated into a huge performance boost, removing some stress from the CPU.

Now the rings were not stacking on top of each other and they could be grabbed by the user.

---

## 📚 Resources


- [**Tower of Hanoi (Wikipedia)**](https://en.wikipedia.org/wiki/Tower_of_Hanoi)

- [**Unity VR template**](https://unity.com/)
- [**Meta Quest 2 – Unity documentation**](https://developer.oculus.com/documentation/unity/unity-gs-overview/)

- [**3D Table Model (Fab)**](https://www.fab.com/search?q=table)

- [**VR game mechanics tutorial (YouTube)**](https://www.youtube.com/watch?v=xp37Hz1t1Q8)


Authors: Hugo & Kateryna
