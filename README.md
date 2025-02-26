# Grass Gpu Instancing
 Instancing grass blades (3d models) using GPU

Most people are familiar with CPU, the main building block of the computer, it's so smart that it can handle millions and even billions of operations per second.
However, the computers have another beast inside, the GPU or Graphic processing unit. Most gamers are familiar with GPUs and it's probably what they always check first whenever they buy a new PC. GPUs are not as smart as CPUs, but they are so unimaginably fast that they can handle half a trillion operation per second, that's why GPUs are fit for rendering, a process that is known to needs heavy concurrent computations to create a simple frame.
In this project I explored using GPUs to instance millions of grass blades (3d models), each consists of 8 vertices. Each blade is getting its own shading that is matching with the terrain corresponding terrain color giving it a beautiful and artistic cozy look!

To give the grass a natural wind sway animation, I resorted to my favorite trigonometric function, Sin(x), I used sin function to generate a distorted wind texture.
All that's left is to pass the world UVs of the terrain to each instance of the grass and use that UV to sample the wind texture from earlier and output all of this to the grass vertices position in the vertex shader.
