# Wave Function Collapse as a Constraint Solver for Game Level Generation

Wave Function Collapse is limited by a lack of global constraints, poor performance and restriction to a finite grid. These issues are rarely addressed directly in implementations. Instead, they are worked around in an ad hoc, game-specific way that fails to exploit constraint programming techniques. Furthermore, presentation of WFC online often fails to acknowledge underlying constraint solving principles used by WFC.

This dissertation seeks to examine how these issues can be addressed and attempts to contextualise WFC within theory of constraint programming. To do this, a simple-tiled implementation of WFC using the Maintaining Arc Consistency 3 algorithm is presented. WFC is extended to generate infinite worlds using Infinite Modifying in Blocks. An interface is provided for the Unity Editor that allows designers to specify their own tile set to use for generation. As example, a game themed after the popular fictional concept of *The Backrooms* is created. Weighted tile selection gives the level designer global control over the percentage of each tile in the output level. However, loading infinite worlds in real-time presents additional performance challenges and further global constraints are needed to improve control further.

Overall, this dissertation brings together WFC and constraint programming concepts with extensive examples to support understanding and further development of WFC.

[See the disseration report here.](190018469-CS4099-Report.pdf)
