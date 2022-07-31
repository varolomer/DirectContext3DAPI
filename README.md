![alt text](https://github.com/varolomer/DirectContext3DAPI/blob/master/DirectContext3DAPI/Assets/Github/Banner.png)

# Revit External Graphics
![alt text](https://github.com/varolomer/RevitWPF/blob/master/RevitWPF/Assets/Github/BatchWallExportProgress.gif)

This library utilizes Revit's DirectContext3D API to be able to flush Vertex and Index Buffers to Revit, in order to draw external graphics to Revit Canvas. The solution contains Autodesk's RevitSDK sample for DirectContext3D but also contains my basic example as a tutorial code. 

# Vertex and Index Buffers
Understanding how Mesh structure works and how they are representedin Vertex and Index Buffers are the backbone of this API. Even if a user is not familiar with the low-level operations of the buffers, at least a basic understanding of what these buffers mean would help significantly to utilize the API.
![alt text](https://github.com/varolomer/DirectContext3DAPI/blob/master/DirectContext3DAPI/Assets/SS/MeshCube.png)

# Diffuse, Ambient and Specular Colors
The colour maps effects how the graphics are display in the viewport. Especially, in shaded views normal vectors of the vertices are extremely important to properly render the shadings.
![alt text](https://github.com/varolomer/DirectContext3DAPI/blob/master/DirectContext3DAPI/Assets/Github/FaceMaps.gif)
