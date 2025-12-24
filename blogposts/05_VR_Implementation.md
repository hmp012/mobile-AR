## Project implementation
Once we got 3 donuts working, we started working on making sure the system follows the rules of the game.

In order to do that, we started by defining the strategy: To make it as dynamic as possible, we would create scripts to define each object's behaviour.
- The SnapZone script was attached to all bases.
- The Donut script, which inherited from SnapZone, was attached to each of the rings.

Thanks to this separation, the GameManager script was able to handle the game.

### Attaching to SnapZone
The idea behind creating the SnapZone script was to handle the user's movements accordingly. In the Tower of Hanoi game, the rings can only be dropped either on top of a larger donut or on top of a base.

Following this rule, we implemented an interactable event for both onSelect and onRelease.

#### Checking for a valid SnapZone
In order to check if the ring was being dropped in a valid SnapZone, we decided to check if the position of the ring itself was close to another SnapZone. Ideally this would be handled by .NET's function Mathf.Approximately which, according to the official documentation: "Compares two floating point values and returns true if they're similar". 
However, in reality, when we were testing, this function was returning false even when the numbers were 0,0000002m apart, which is why we were forced to implement our own NearlyEqual function, which would have 0,4m of threshold.

Now, the system was able to tell if a ring was being dropped in the correct place or not.

### Returning to original position when not dropping into a valid SnapZone
We came out with the idea of storing the original values of the transform and returning them to their original value onRelease if the ring was not close to a SnapZone.

### Checking order of rings
As mentioned above, the onSelect method was storing the original transform values of the rings, to be returned to the original position in case of an invalid onRelease.
However, onSelect was also checking that the selected ring was the one at the top, otherwise, following the same system as with invalid onRelease, it would return the rings to their original spot thanks to storing those values onSelect.

Having more time to think now, we would have created another callback for onHover, checking if the grab would be valid on each ring, so the needed calculations would be done by the time the user selects, so we could prevent it, thus, not having to wait until onRelease is called.

### Issues with residual contacts
Due to Unity's nature, interactable objects can be moved around, and those with a collider will also do it when they collide with another object.

In this case, when the onSelect is valid, the game incorrectly allows collisions with other donuts that could result in donuts being moved out of their SnapZone, this is a known bug that couldn't be fixed due to lack of time.

A possible solution would have been to use the stored original values of the rings onSelect, and make sure all other rings, except the one selected, were returned to their original position.

## Null pointer exceptions
One of the most recurring issues during the development were several null pointer exceptions. These were caused mainly because GameManager works with a fixed list of Donuts/rings but the list could be not updated at the time some functions were running, so objects could be missing or not found where the script was trying to find them.

Ideally, we could use Unity's Update callback. However, we didn't like the option to run the method on every frame, as it can lead to inconsistency in the data and performance issues. Instead, we preferred a more controlled approach, where each of the methods that required it would run a manual refresh method before any other changes were performed.

We acknowledge that this can be the source of future bugs, as it requires a manual implementation and thorough testing, but the performance gains and the data consistency outweighed the cons for such a simple project.

## Conclusion
Despite these challenges, the core mechanics are solid. The separation of concerns between SnapZones, Donuts, and the GameManager allowed us to build a functional Tower of Hanoi implementation that respects the rules of the game.

This solid base allowed us to spend a considerable amount of time on improving the UX.

Author: Hugo
