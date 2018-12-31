using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Visualiser.Containers.Types;
using Visualiser.Containers.Vertices;
using Visualiser.Graphics.Objects;
using Visualiser.Shaders;
using Device = SharpDX.Direct3D11.Device;

namespace Visualiser.Graphics.Components
{
	public class QuadTree
	{
		private readonly int MAX_TRIANGLES = 10000;

		// Properties
		public int TriangleCount { get; set; }
		public int DrawCount { get; set; }
		public PositionTextureNormalVertex[] VertexList { get; set; }
		public QuadTreeNodeType ParentNode { get; set; }

		// Methods
		public bool Initialise(Terrain terrain, Device device)
		{
			// Get the number of vertices in the terrain vertex array.
			int vertexCount = terrain.VertexCount;

			// Store the total triangle count for the vertex list.
			TriangleCount = vertexCount / 3;

			// Create a vertex array to hold all of the terrain vertices.
			VertexList = new PositionTextureNormalVertex[vertexCount];

			// Copy the terrain vertices into the vertex list.
			VertexList = terrain.Vertices;

			// Calculate the center x,z and the width of the mesh.
			float centerX, centerZ, width;
			CalculateMeshDimensions(vertexCount, out centerX, out centerZ, out width);

			// Create the parent node for the quad tree.
			ParentNode = new QuadTreeNodeType();
			QuadTreeNodeType createdNode;

			// Recursively build the quad tree based on the vertex list data and mesh dimensions.
			// ParentNode = CreateTreeNode(device, centerX, centerZ, width);
			CreateTreeNode2(device, ParentNode, centerX, centerZ, width, out createdNode);
			ParentNode = createdNode;

			// Release the vertex list since the quad tree now has the vertices in each node.
			VertexList = null;

			return true;
		}
		private void CreateTreeNode2(SharpDX.Direct3D11.Device device, QuadTreeNodeType node, float positionX, float positionZ, float width, out QuadTreeNodeType createdNode)
		{
			// Store the node position and size.
			node.positionX = positionX;
			node.positionZ = positionZ;
			node.width = width;

			// Initialize the triangle count to zero for the node.
			node.TriangleCount = 0;

			// Initialize the vertex and index buffer to null.
			node.VertexBuffer = null;
			node.IndexBuffer = null;

			// Initialize the Nodes vertex array to null.
			node.vertexArray = null;

			// Count the number of triangles that are inside this node.
			int numTriangles = CountTriangles(positionX, positionZ, width);

			// Case 1: If there are no triangles in this node then return as it is empty and requires no processing.
			if (numTriangles == 0)
			{
				createdNode = node;
				return;
			}

			// Initialize the children nodes of this node to null.
			node.Nodes = new QuadTreeNodeType[4];

			// Case 2: If there are too many triangles in this node then split it into four equal sized smaller tree nodes.
			if (numTriangles > MAX_TRIANGLES)
			{
				for (int i = 0; i < 4; i++)
				{
					// Calculate the position offsets for the new child node.
					float offsetX = (((i % 2) < 1) ? -1.0f : 1.0f) * (width / 4.0f);
					float offsetZ = (((i % 4) < 2) ? -1.0f : 1.0f) * (width / 4.0f);

					// See if there are any triangles in the new node.
					int count = CountTriangles((positionX + offsetX), (positionZ + offsetZ), (width / 2.0));

					if (count > 0)
					{
						// If there are triangles inside where this new node would be then create the child node.
						node.Nodes[i] = new QuadTreeNodeType();
						QuadTreeNodeType aNewNode;

						// Extend the tree starting from this new child node now.
						CreateTreeNode2(device, node.Nodes[i], (positionX + offsetX), (positionZ + offsetZ), (width / 2.0f), out aNewNode);

						node.Nodes[i] = aNewNode;
						createdNode = aNewNode;
					}
				}

				createdNode = node;
				return;
			}

			// Case 3: If this node is not empty and the triangle count for it is less than the max then this node is at the bottom of the tree so create the list of triangles to store in it.
			node.TriangleCount = numTriangles;

			// Calculate the number of vertices.
			int vertexCount = numTriangles * 3;

			// Create the vertex array.
			PositionTextureNormalVertex[] vertices = new PositionTextureNormalVertex[vertexCount];
			// Create the index array
			int[] indices = new int[vertexCount];

			// Create the vertex array.
			node.vertexArray = new XYZType[vertexCount];

			// Initialize the index for this new vertex and index array.
			int index = 0;
			// Go through all the triangles in the vertex list.
			for (int i = 0; i < TriangleCount; i++)
			{
				// If the triangle is inside this node then add it to the vertex array.
				if (IsTriangleContained(i, positionX, positionZ, width))
				{
					// Calculate the index into the terrain vertex list.
					int vertexIndex = i * 3;

					// Get the three vertices of this triangle from the vertex list.
					vertices[index] = VertexList[vertexIndex];
					indices[index] = index;
					// Also store the vertex position information in the node vertex array.
					node.vertexArray[index].x = VertexList[vertexIndex].position.X;
					node.vertexArray[index].y = VertexList[vertexIndex].position.Y;
					node.vertexArray[index].z = VertexList[vertexIndex].position.Z;

					// Increment the indexes.
					index++;
					vertexIndex++;

					vertices[index] = VertexList[vertexIndex];
					indices[index] = index;
					node.vertexArray[index].x = VertexList[vertexIndex].position.X;
					node.vertexArray[index].y = VertexList[vertexIndex].position.Y;
					node.vertexArray[index].z = VertexList[vertexIndex].position.Z;

					// Increment the indexes.
					index++;
					vertexIndex++;
					vertices[index] = VertexList[vertexIndex];
					indices[index] = index;
					node.vertexArray[index].x = VertexList[vertexIndex].position.X;
					node.vertexArray[index].y = VertexList[vertexIndex].position.Y;
					node.vertexArray[index].z = VertexList[vertexIndex].position.Z;

					index++;
				}
			}

			// Set up the description of the vertex buffer. no Need its static
			// Create the vertex buffer.
			node.VertexBuffer = SharpDX.Direct3D11.Buffer.Create(device, BindFlags.VertexBuffer, vertices);

			// Create the index buffer.
			node.IndexBuffer = SharpDX.Direct3D11.Buffer.Create(device, BindFlags.IndexBuffer, indices);

			// Release the vertex and index arrays now that the data is stored in the buffers in the node.
			vertices = null;
			indices = null;

			createdNode = node;

			return;
		}
		public bool GetHeightAtPosition(float positionX, float positionZ, out float height)
		{
			height = 0.0f;

			float meshMinX = ParentNode.positionX - (ParentNode.width / 2.0f);
			float meshMaxX = ParentNode.positionX + (ParentNode.width / 2.0f);
			float meshMinZ = ParentNode.positionZ - (ParentNode.width / 2.0f);
			float meshMaxZ = ParentNode.positionZ + (ParentNode.width / 2.0f);

			// Make sure the coordinates are actually over a polygon.
			if ((positionX < meshMinX) || (positionX > meshMaxX) || (positionZ < meshMinZ) || (positionZ > meshMaxZ))
				return false;

			// Find the node which contains the polygon for this position.
			return FindNode(ParentNode, positionX, positionZ, out height);
		}
		private bool FindNode(QuadTreeNodeType node, float x, float z, out float height)
		{
			height = 0.0f;

			Vector3 vertex1, vertex2, vertex3;

			// Calculate the dimensions of this node.
			float xMin = node.positionX - (node.width / 2.0f);
			float xMax = node.positionX + (node.width / 2.0f);
			float zMin = node.positionZ - (node.width / 2.0f);
			float zMax = node.positionZ + (node.width / 2.0f);

			// See if the x and z coordinate are in this node, if not then stop traversing this part of the tree.
			if ((x < xMin) || (x > xMax) || (z < zMin) || (z > zMax))
				return false;

			// If the coordinates are in this node then check first to see if children nodes exist.
			int count = 0;
			for (int i = 0; i < 4; i++)
				if (node.Nodes[i].width != 0)
				{
					count++;
					if (FindNode(node.Nodes[i], x, z, out height))
						return true;
				}

			// If there were no children then the polygon must be in this node.  Check all the polygons in this node to find 
			// the height of which one the polygon we are looking for.
			int index = 0;
			for (int i = 0; i < node.TriangleCount; i++)
			{
				index = i * 3;
				vertex1.X = node.vertexArray[index].x;
				vertex1.Y = node.vertexArray[index].y;
				vertex1.Z = node.vertexArray[index].z;

				index++;
				vertex2.X = node.vertexArray[index].x;
				vertex2.Y = node.vertexArray[index].y;
				vertex2.Z = node.vertexArray[index].z;

				index++;
				vertex3.X = node.vertexArray[index].x;
				vertex3.Y = node.vertexArray[index].y;
				vertex3.Z = node.vertexArray[index].z;

				// Check to see if this is the polygon we are looking for.
				// If this was the triangle then quit the function and the height will be returned to the calling function.
				if (CheckHeightOfTriangle(x, z, out height, vertex1, vertex2, vertex3))
					return true;
			}

			return false;
		}
		private bool CheckHeightOfTriangle(float x, float z, out float height, Vector3 v0, Vector3 v1, Vector3 v2)
		{
			height = 0.0f;

			// Starting position of the ray that is being cast.
			Vector3 startVector = new Vector3(x, 0.0f, z);

			// The direction the ray is being cast going striahgt down.
			Vector3 directionVector = new Vector3(0.0f, -1.0f, 0.0f);

			// Calculate the two edges from the three points given.
			Vector3 edge1 = new Vector3(v1.X - v0.X, v1.Y - v0.Y, v1.Z - v0.Z);
			Vector3 edge2 = new Vector3(v2.X - v0.X, v2.Y - v0.Y, v2.Z - v0.Z);

			// Calculate the normal of the triangle from the two edges.
			Vector3 normal = new Vector3();
			normal.X = (edge1.Y * edge2.Z) - (edge1.Z * edge2.Y);
			normal.Y = (edge1.Z * edge2.X) - (edge1.X * edge2.Z);
			normal.Z = (edge1.X * edge2.Y) - (edge1.Y * edge2.X);

			float magnitude = (float)Math.Sqrt((normal.X * normal.X) + (normal.Y * normal.Y) + (normal.Z * normal.Z));
			normal.X = normal.X / magnitude;
			normal.Y = normal.Y / magnitude;
			normal.Z = normal.Z / magnitude;

			// Find the distance from the origin to the plane.
			float D = ((-normal.X * v0.X) + (-normal.Y * v0.Y) + (-normal.Z * v0.Z));

			// Get the denominator of the equation.
			float denominator = ((normal.X * directionVector.X) + (normal.Y * directionVector.Y) + (normal.Z * directionVector.Z));

			// Make sure the result doesn't get too close to zero to prevent divide by zero.
			if (Math.Abs(denominator) < 0.0001f)
				return false;

			// Get the numerator of the equation.
			float numerator = -1.0f * (((normal.X * startVector.X) + (normal.Y * startVector.Y) + (normal.Z * startVector.Z)) + D);

			// Calculate where we intersect the triangle.
			float t = numerator / denominator;

			// Find the intersection vector.
			Vector3 Q = new Vector3();
			Q.X = startVector.X + (directionVector.X * t);
			Q.Y = startVector.Y + (directionVector.Y * t);
			Q.Z = startVector.Z + (directionVector.Z * t);

			// Find the three edges of the triangle.
			Vector3 e1 = new Vector3();
			e1.X = v1.X - v0.X;
			e1.Y = v1.Y - v0.Y;
			e1.Z = v1.Z - v0.Z;
			Vector3 e2 = new Vector3();
			e2.X = v2.X - v1.X;
			e2.Y = v2.Y - v1.Y;
			e2.Z = v2.Z - v1.Z;
			Vector3 e3 = new Vector3();
			e3.X = v0.X - v2.X;
			e3.Y = v0.Y - v2.Y;
			e3.Z = v0.Z - v2.Z;

			// Calculate the normal for the first edge.
			Vector3 edgeNormal = new Vector3();
			edgeNormal.X = (e1.Y * normal.Z) - (e1.Z * normal.Y);
			edgeNormal.Y = (e1.Z * normal.X) - (e1.X * normal.Z);
			edgeNormal.Z = (e1.X * normal.Y) - (e1.Y * normal.X);

			// Calculate the determinant to see if it is on the inside, outside, or directly on the edge.
			Vector3 temp = new Vector3();
			temp.X = Q.X - v0.X;
			temp.Y = Q.Y - v0.Y;
			temp.Z = Q.Z - v0.Z;
			float determinant = ((edgeNormal.X * temp.X) + (edgeNormal.Y * temp.Y) + (edgeNormal.Z * temp.Z));

			// Check if it is outside.
			if (determinant > 0.001f)
				return false;

			// Calculate the normal for the second edge.
			edgeNormal.X = (e2.Y * normal.Z) - (e2.Z * normal.Y);
			edgeNormal.Y = (e2.Z * normal.X) - (e2.X * normal.Z);
			edgeNormal.Z = (e2.X * normal.Y) - (e2.Y * normal.X);

			// Calculate the determinant to see if it is on the inside, outside, or directly on the edge.
			temp.X = Q.X - v1.X;
			temp.Y = Q.Y - v1.Y;
			temp.Z = Q.Z - v1.Z;
			determinant = ((edgeNormal.X * temp.X) + (edgeNormal.Y * temp.Y) + (edgeNormal.Z * temp.Z));

			// Check if it is outside.
			if (determinant > 0.001f)
				return false;

			// Calculate the normal for the third edge.
			edgeNormal.X = (e3.Y * normal.Z) - (e3.Z * normal.Y);
			edgeNormal.Y = (e3.Z * normal.X) - (e3.X * normal.Z);
			edgeNormal.Z = (e3.X * normal.Y) - (e3.Y * normal.X);

			// Calculate the determinant to see if it is on the inside, outside, or directly on the edge.
			temp.X = Q.X - v2.X;
			temp.Y = Q.Y - v2.Y;
			temp.Z = Q.Z - v2.Z;
			determinant = ((edgeNormal.X * temp.X) + (edgeNormal.Y * temp.Y) + (edgeNormal.Z * temp.Z));

			// Check if it is outside.
			if (determinant > 0.001f)
				return false;

			// Now we have our height.
			height = Q.Y;

			return true;
		}
		private int CountTriangles(float positionX, float positionZ, double width)
		{
			// Initialize the count to zero.
			int count = 0;

			// Go through all the triangles in the entire mesh and check which ones should be inside this node.
			for (int i = 0; i < TriangleCount; i++)
			{
				// If the triangle is inside the node then increment the count by one.
				if (IsTriangleContained(i, positionX, positionZ, width))
					count++;
			}

			return count;
		}
		private bool IsTriangleContained(int index, float positionX, float positionZ, double width)
		{
			// Calculate the radius of this node
			double radius = width / 2.0f;

			// Get the index into the vertex list.
			int vertexIndex = index * 3;

			// Get the three vertices of this triangle from the vertex list.
			float x1 = VertexList[vertexIndex].position.X;
			float z1 = VertexList[vertexIndex].position.Z;
			vertexIndex++;
			float x2 = VertexList[vertexIndex].position.X;
			float z2 = VertexList[vertexIndex].position.Z;
			vertexIndex++;
			float x3 = VertexList[vertexIndex].position.X;
			float z3 = VertexList[vertexIndex].position.Z;

			// Check to see if the minimum of the x coordinates of the triangle is inside the node.
			float minimumX = Math.Min(x1, Math.Min(x2, x3));
			if (minimumX > (positionX + radius))
				return false;

			// Check to see if the maximum of the x coordinates of the triangle is inside the node.
			float maximumX = Math.Max(x1, Math.Max(x2, x3));
			if (maximumX < (positionX - radius))
				return false;

			// Check to see if the minimum of the z coordinates of the triangle is inside the node.
			float minimumZ = Math.Min(z1, Math.Min(z2, z3));
			if (minimumZ > (positionZ + radius))
				return false;

			// Check to see if the maximum of the z coordinates of the triangle is inside the node.
			float maximumZ = Math.Max(z1, Math.Max(z2, z3));
			if (maximumZ < (positionZ - radius))
				return false;

			return true;
		}
		private void CalculateMeshDimensions(int vertexCount, out float centerX, out float centerZ, out float meshWidth)
		{
			// Initialize the center position of the mesh to zero.
			centerX = 0.0f;
			centerZ = 0.0f;

			// Sum all the vertices in the mesh.
			for (int i = 0; i < vertexCount; i++)
			{
				centerX += VertexList[i].position.X;
				centerZ += VertexList[i].position.Z;
			}

			// And then divide it by the number of vertices to find the mid-point of the mesh.
			centerX = centerX / (float)vertexCount;
			centerZ = centerZ / (float)vertexCount;

			// Initialize the maximum and minimum size of the mesh.
			float maxWidth = 0.0f;
			float maxDepth = 0.0f;

			float minWidth = Math.Abs(VertexList[0].position.X - centerX);
			float minDepth = Math.Abs(VertexList[0].position.Z - centerZ);

			// Go through all the vertices and find the maximum and minimum width and depth of the mesh.
			for (int i = 0; i < vertexCount; i++)
			{
				float width = Math.Abs(VertexList[i].position.X - centerX);
				float depth = Math.Abs(VertexList[i].position.Z - centerZ);

				if (width > maxWidth)
					maxWidth = width;
				if (depth > maxDepth)
					maxDepth = depth;
				if (width < minWidth)
					minWidth = width;
				if (depth < minDepth)
					minDepth = depth;
			}

			// Find the absolute maximum value between the min and max depth and width.
			float maxX = Math.Max(Math.Abs(minWidth), Math.Abs(maxWidth));
			float maxZ = Math.Max(Math.Abs(minDepth), Math.Abs(maxDepth));

			// Calculate the maximum diameter of the mesh.
			meshWidth = Math.Max(maxX, maxZ) * 2.0f;
		}
		public void Shutdown()
		{
			ReleaseNode(ParentNode);
		}
		private void ReleaseNode(QuadTreeNodeType theNode)
		{
			// Watch to see if this actually closes the Node structs.
			if (theNode.VertexBuffer == null)
				return;

			// Release the vertex array for this node.
			theNode.vertexArray = null;

			// Recursively go down the tree and release the bottom nodes first
			for (int i = 0; i < 4; i++)
				if (theNode.Nodes[i].VertexBuffer != null)
					ReleaseNode(theNode.Nodes[i]);

			// Release the vertex buffer for this node
			theNode.VertexBuffer?.Dispose();
			theNode.VertexBuffer = null;
			// Release the index buffer for this node
			theNode.IndexBuffer?.Dispose();
			theNode.IndexBuffer = null;
		}
		public void Render(DeviceContext deviceContext, Frustrum frustrum, TerrainShader terrainShader)
		{
			// Reset the number of the triangles that are drawn for this frame.
			DrawCount = 0;

			// Render each node that is visible at the parent node and moving down the tree.
			RenderNode(deviceContext, ParentNode, frustrum, terrainShader);
		}
		private void RenderNode(DeviceContext deviceContext, QuadTreeNodeType node, Frustrum frustrum, TerrainShader terrainShader)
		{
			// Check to see if the node can be viewed, height doesn't matter in a quad tree.
			// If it can't be seen then none of its children can either, so don't continue down the tree, this is where the speed is gained.
			if (!frustrum.CheckCube(new SharpDX.Vector3(node.positionX, 0.0f, node.positionZ), (node.width / 2.0f)))  //   63.7506409
				return;

			// If it can be seen then check all four child nodes to see if they can also be seen.
			int count = 0;
			for (int i = 0; i < 4; i++)
			{   // parentNode.width > 0.0f && parentNode.Nodes[i].VertexBuffer != null
				if (node.Nodes[i].width > 0.0f) //node.Nodes != null/ parentNode.Nodes[i].width > 0.0f
				{
					count++;
					RenderNode(deviceContext, node.Nodes[i], frustrum, terrainShader);
				}
			}

			// If there were any children nodes then there is no need to continue as parent nodes won't contain any triangles to render.
			if (count != 0)
				return;

			int vertexCount = node.TriangleCount * 3;

			// Otherwise if this node can be seen and has triangles in it then render these triangles.
			// Set vertex buffer stride and offset.
			// Set the vertex buffer to active in the input assembler so it can be rendered.
			deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(node.VertexBuffer, SharpDX.Utilities.SizeOf<PositionTextureNormalVertex>(), 0));

			// Set the index buffer to active in the input assembler so it can be rendered.
			deviceContext.InputAssembler.SetIndexBuffer(node.IndexBuffer, Format.R32_UInt, 0);

			// Set the type of primitive that should be rendered from this vertex buffer, in this case triangles.
			deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

			// Determine the number of indices in this node.
			int indexCount = node.TriangleCount * 3;

			// Call the terrain shader to render the polygons in this node.
			terrainShader.RenderShader(deviceContext, indexCount);

			// Increase the count of the number of polygons that have been rendered during this frame.
			DrawCount += node.TriangleCount;
		}
	}
}