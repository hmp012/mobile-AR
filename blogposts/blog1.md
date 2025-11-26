## Introduction to Our Mobile AR Project

For our first project, we explored several different ideas, but the final decision was to develop a **mobile AR application for the Horsens train station**. Since we had no previous experience with Unity or AR development, we did not fully understand the total scope of the project. However, we were enthusiastic and motivated to create something meaningful for the **Horsens community** and for the **international VIA University College students**, who frequently use the train station to travel to campus.

Because the Horsens train station has a medium size and can be confusing for newcomers, we wanted to implement a system we call **"TrainFinder"**.

The purpose of TrainFinder is to help users quickly and easily find their correct platform using augmented reality guidance. Our idea was to let the user **calibrate the environment** by scanning specific pictures located inside the station, and then allow the system to guide them toward their chosen platform.

### How the User Selects the Destination
In our prototype, the user selects the final destination (e.g., **Platform 1, Platform 2, Platform 3**).  
After choosing their platform, the AR navigation system calculates the path and displays the **blue AR line** on the floor, guiding the user step by step.

This milestone represents our first steps: researching Unity, AR Foundation, image tracking, pathfinding, and how to combine them into one functional navigation system.

### 3D Scanning

For the 3D scanning process, we used a borrowed **iPhone 14 Pro** from XRD lab, which includes a built-in LiDAR sensor capable of capturing accurate spatial data. To create our scans, we used the application [3D Scanner App](https://3dscannerapp.com/).

We began by scanning the **main hall** of the Horsens train station, then continued into the **tunnel**, and finally scanned **each platform separately**.  
The scanned models were exported as **.obj** files and later processed in **Blender**.  
For use in Unity, we worked with the cleaned and optimized models in **.fbx** format.

### Setting Physical Size for Tracked Images

In Unity, the physical size for each tracked image is configured inside the **XR Reference Image Library**.  
For each image (e.g., Departures Board, Mailbox, Security Camera), we enabled **Specify Size** and entered the real-world **width in meters**. Unity automatically calculates the height based on the texture ratio.  
This ensures that AR tracking is accurate and correctly scaled in the real environment.

### Testing

Firsly, we built and ran the application on an **Android phone** to test its behavior in a real-world environment. During these tests, we checked whether the 3D model aligned correctly with the physical surroundings and whether the tracking remained stable as we moved around.

One of the first challenges we faced was that we did not initially recognize or understand the **XR Simulation** platform in Unity. Because of this, most of our early testing was done directly on a **real phone**, which made development slower. Later on, we also tested the system in the **real Horsens train station** using printed reference images. We had to choose times when there were fewer people around, which sometimes made the testing schedule complicated.

Another difficulty appeared after we finally discovered the simulation platform:  
our 3D model files were **too large** and caused performance issues on our personal laptops. Because of this, most of the development and testing had to be done in the **XRD Lab**, where we used stronger computers that could handle the heavy 3D environment.

### Version Control

Our project uses Git for version control, with GitHub as the remote repository.  
We also use Google drive to handle large files such as 3D models and textures.

### Resources

- [3D Scanner App](https://3dscannerapp.com/)  
- [Blender](https://www.blender.org/)  
- [AR Tracked Image Manager (Unity Documentation)](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/manual/features/image-tracking.html)



