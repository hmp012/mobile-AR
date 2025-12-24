## Focusing on User Experience
Once the core mechanics of the game were solid, we could spend time focusing on improving the UX. In order to accomplish this we decided to:

- Explain the rules of the game before the game started
- Allow the user to set the amount of rings in the game
- Allow the user to restart the game
- Count the steps the user needed to complete the game
- Congratulate the user when the game is finished
- Adding tooltips to show errors

### Explaining the rules of the game before the game started
In order to explain the user the rules, we created a Spatial Panel Manipulator, explaining the rules in 3 simple steps, as shown in the image.

<img src="../images/SpatialPanelManipulator.png" alt="Screenshot from Spatial Panel Manipulator from Unity editor"/>

In here, the user gets welcomed to the game and gets to decide whether to continue with the tutorial or skip directly to the game configuration. To do this, we reused several prefabs from the VR Template tutorial.

Reusing already existing prefabs allowed us to gain some speed, as with our React/Swift backgrounds, this style of UI was certainly different and less intuitive.

If the User decides to follow the tutorial, they get a couple of slides inside of the same Spatial Panel Manipulator where the game is explained in simple terms.

#### Adding depth to the buttons
As a point of comparison, the "Skip Tutorial" button does not have any feeling of depth, unlike the "Continue" button. This was yet another example of our struggles with Unity's UI system, as we just were not expecting to have a completely separate GameObject just to add this depth sensation into the button.

### Setting up the game
Once the tutorial is completed, or if the user clicked "Skip" in the first card of the Spatial Panel Manipulator, they get presented with the last card of the Spatial Panel Manipulator, in there, the users can add or remove rings from the tower.

<img src="../images/GameSetupCard.png" alt="Screenshot from Spatial Panel Manipulator from Unity editor"/>

These buttons, with appropriate color schemes to accommodate the User's behaviour, caused several issues:

Firstly, it took some time to find a way to attach the buttons a callback onClick, as we learned here that Unity callbacks must not return any object. We spent some time trying to figure out why were the functions from GameManager not showing up in the list in the Unity editor.

Secondly, Instantiating new objects that had to be in a specific location wasn't as easy as expected. We went with the approach of each new ring would be 0,1m higher in the y axis. However, despite the colliders being a **Mesh renderer**, when the 4th donut gets added, it doesn't stay at the top of the tower, instead, due to Unity's physics nature, the inertia from the gravity pushing downwards make the 4th ring sink to the middle of the tower.
This issue was something we were really worried, as it was an important bug. However, we realized other popular Unity games were suffering with similar issues, like [Tigerball](https://play.google.com/store/apps/details?id=com.Laxarus.TigerBall&hl=en) for instance, where some of the elements subject to gravity can have different behaviours even when placed at the same spot.

Finally, to make each ring smaller, we decided to multiply the scale times 0,1. This was working perfectly for the first couple of rings. However, once the scale was far too small, Unity was increasing the size. This is a bug we couldn't find a solution for. Although, as a fix, we could have made the Donut prefab have a larger scale from the start, reducing the overall dimensions so this issue wouldn't happen. Moreover, to stop the user from creating several donuts in the wrong order, we could have limited the amount to 3 or 4, to reduce the number of errors.


### Game Start
Ideally, before the game begins, we should have been able to disable all the interactable objects, so the user couldn't grab them without the game started. However, we faced some issues enabling back again when the game started, so in the end, we decided to add yet another check to the onSelect callback, and returning them to their original position once the user tried to drop them.

Upon completion of the game setup, the user would click on "Continue", which would mark the game as started in the GameManager, the Spatial Panel Manipulator would be disabled, and another panel, GameStatusIndicator would appear, showing the user the current status of the game.

<img src="../images/GameStatusIndicator.png" alt="Game Status indicator"/>


### Measuring game movements
Among all features, this was probably the easiest of all. GameManager now handles an int, that counts the number of steps. This number gets updated on the onRelease callback only after checking if it's a valid release.

This value was displayed on the Spatial Manipulator Panel that appears after the game starts.

### Game End
The game concludes when the user successfully stacks all the donuts on the rightmost tower in the correct ascending order of size. Upon detecting this condition, the `GameStatusIndicator` updates to display a congratulatory message to the user. Additionally, a "Restart" button becomes available, allowing the player to reset the experience.

### Restart Button Issues
While the restart functionality is present, it currently suffers from a known bug. The restart button is malfunctioning and requires a specific sequence of interactions to work as intended. Users need to fully restart the game, start the game again and then restart, for the reset to take effect properly. This behavior suggests an issue with how the scene reloading or object state resetting is handled in Unity.

### Tooltips on the rings
To further assist the user and provide immediate feedback when an invalid move is attempted (like placing a larger ring on a smaller one), we decided to attach a tooltip to the donut prefab.

We initially attempted to use the "Lazy Follow" script provided by the Unity VR Template to make the tooltip smoothly follow the user's gaze or the controller. However, we faced significant challenges getting it to behave correctly, as the tooltip would often lag behind or obstruct the view. After some trial and error and tweaking the follow parameters, we managed to make it work effectively, providing a helpful UI element that appears right where the user is looking without being intrusive.


## Project Future
While we are proud of the current state of our Tower of Hanoi VR application, there are several areas we identified for future development to elevate the experience from a prototype to a polished product.

### Enhancing Immersion and Atmosphere
Currently, the game takes place in a relatively sterile environment with a basic table and the game elements. To increase immersion, we would like to:
- **Environment Design**: Create a more engaging setting, such as a cozy study room or a sci-fi lab, rather than a void.
- **Audio Feedback**: Implement sound effects for interactions—grabbing donuts, the "clack" of placing them on a rod, and a celebratory sound when the puzzle is solved.
- **Haptic Feedback**: Utilize the controller's haptics to give physical feedback when the user grabs an object or when a ring snaps into place.

### Addressing Technical Debt and Bugs
Several bugs identified during development remain and would be the priority for the next iteration:
- **Physics Stability**: The issue with the 4th ring sinking due to gravity/inertia needs a robust fix. We could explore switching the donuts to `IsKinematic` once they are successfully snapped to a tower to prevent physics calculations from messing with their position.
- **Restart Reliability**: The restart button's erratic behavior (requiring double clicks or full game restarts) indicates a deeper issue with scene management or variable initialization. A thorough debugging of the `GameManager`'s state machine is needed.
- **Residual Collisions**: The bug where donuts push each other out of snap zones needs to be addressed, possibly by disabling collisions between donuts when they are snapped.

### Code Refactoring
As mentioned in previous posts, our reliance on manual refresh methods to avoid `Update` loops and the resulting Null Pointer Exceptions suggests that our architecture could be improved. Moving towards an event-driven architecture (using C# Events or Unity Actions) would make the communication between `SnapZone`, `Donut`, and `GameManager` more robust and less error-prone.

### Gameplay Expansions
Finally, to make the game more replayable:
- **Dynamic Ring Count**: Fully fix the scaling issues to allow users to select between 3 to 8 rings without physics glitches.
- **Timer and Scoring**: Add a timer to challenge users to solve the puzzle faster, and a local leaderboard to track best times.

Author: Hugo