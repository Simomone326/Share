using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Added for easy array manipulation (used in vertex reuse clarity)

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class marchingSquares : MonoBehaviour
{
    // These variables are set by the generator script
    public int levelwidth;
    public int[,] binaryPoints;
    public int levelheight;
    public float min; // Currently unused as binaryPoints is already 0/1

    // Lookup table for marching squares triangulation
    readonly int[][] baseLookUpTable = new int[][]
    {
        new int[] {}, // 0 - empty
        new int[] {6, 5, 7},
        new int[] {4, 3, 5},
        new int[] {6, 4, 7, 7, 4, 3},
        new int[] {2, 1, 3},
        new int[] {2, 1, 3, 1, 7, 3, 3, 7, 5, 5, 7, 6},
        new int[] {2, 1, 4, 4, 1, 5},
        new int[] {6, 1, 7, 1, 6, 2, 2, 6, 4},
        new int[] {0, 7, 1},
        new int[] {0, 6, 1, 1, 6, 5},
        new int[] {0, 7, 1, 1, 7, 5, 5, 3, 1, 5, 4, 3},
        new int[] {0, 6, 4, 4, 3, 0, 0, 3, 1},
        new int[] {0, 7, 3, 3, 2, 0},
        new int[] {0, 6, 2, 2, 6, 5, 5, 3, 2},
        new int[] {2, 0, 4, 4, 0, 5, 5, 0, 7},
        // 15 - full cell: use corner points 0, 2, 4, 6
        new int[] {0, 2, 4, 0, 4, 6}
    };

    public void calculateAndGenerate()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Cache for vertex indices: Key = (cellX, cellY, localVertexIdx 0-7)
        Dictionary<(int, int, int), int> vertexCache = new Dictionary<(int, int, int), int>();

        int maxX = binaryPoints.GetLength(0) - 1;
        int maxY = binaryPoints.GetLength(1) - 1;

        for (int x = 0; x < maxX; x++)
        {
            for (int y = 0; y < maxY; y++)
            {
                // Read corner values: 6 (BL), 4 (BR), 2 (TR), 0 (TL)
                int[] cellValues = new int[4];
                cellValues[0] = binaryPoints[x, y];        // BL (x, y) - Corresponds to corner 6
                cellValues[1] = binaryPoints[x + 1, y];    // BR (x+1, y) - Corresponds to corner 4
                cellValues[2] = binaryPoints[x + 1, y + 1]; // TR (x+1, y+1) - Corresponds to corner 2
                cellValues[3] = binaryPoints[x, y + 1];    // TL (x, y+1) - Corresponds to corner 0

                // Calculate index: BL*1 + BR*2 + TR*4 + TL*8 (This order is standard)
                int configIndex = calculateIndex(cellValues);

                if (configIndex == 0)
                    continue;

                int[] triDef = baseLookUpTable[configIndex];
                if (triDef == null || triDef.Length == 0)
                    continue;

                // Local positions for the 8 possible verts (relative to cell origin (x,y))
                Vector3[] localVerts = new Vector3[]
                {
                    new Vector3(0, 1, 0) + new Vector3(x, y, 0),    // 0 Top-Left (TL)
                    new Vector3(0.5f, 1, 0) + new Vector3(x, y, 0), // 1 Top-Mid (TM)
                    new Vector3(1, 1, 0) + new Vector3(x, y, 0),    // 2 Top-Right (TR)
                    new Vector3(1, 0.5f, 0) + new Vector3(x, y, 0), // 3 Right-Mid (RM)
                    new Vector3(1, 0, 0) + new Vector3(x, y, 0),    // 4 Bottom-Right (BR)
                    new Vector3(0.5f, 0, 0) + new Vector3(x, y, 0), // 5 Bottom-Mid (BM)
                    new Vector3(0, 0, 0) + new Vector3(x, y, 0),    // 6 Bottom-Left (BL)
                    new Vector3(0, 0.5f, 0) + new Vector3(x, y, 0)  // 7 Left-Mid (LM)
                };

                int[] localToGlobal = new int[8];

                // --- START ROBUST VERTEX REUSE LOGIC ---

                for (int i = 0; i < 8; i++)
                {
                    bool mapped = false;
                    int globalIndex = -1;

                    // Reuse checks prioritize already processed (x-1 and y-1) neighbors.

                    // Check for Corner Vertices (0, 2, 4, 6) and Midpoints (1, 3, 5, 7)

                    // Case: Vertex 6 (Bottom-Left: x, y) - Shared by 4 cells
                    if (i == 6)
                    {
                        // Try Below-Left neighbor (x-1, y-1) as its 2 (Top-Right)
                        if (x > 0 && y > 0 && vertexCache.TryGetValue((x - 1, y - 1, 2), out globalIndex)) mapped = true;
                        // Try Below neighbor (x, y-1) as its 0 (Top-Left)
                        else if (y > 0 && vertexCache.TryGetValue((x, y - 1, 0), out globalIndex)) mapped = true;
                        // Try Left neighbor (x-1, y) as its 4 (Bottom-Right)
                        else if (x > 0 && vertexCache.TryGetValue((x - 1, y, 4), out globalIndex)) mapped = true;
                    }
                    // Case: Vertex 4 (Bottom-Right: x+1, y) - Shared by 2 cells (Below)
                    else if (i == 4)
                    {
                        // Try Below neighbor (x, y-1) as its 2 (Top-Right)
                        if (y > 0 && vertexCache.TryGetValue((x, y - 1, 2), out globalIndex)) mapped = true;
                    }
                    // Case: Vertex 0 (Top-Left: x, y+1) - Shared by 2 cells (Left)
                    else if (i == 0)
                    {
                        // Try Left neighbor (x-1, y) as its 2 (Top-Right)
                        if (x > 0 && vertexCache.TryGetValue((x - 1, y, 2), out globalIndex)) mapped = true;
                    }
                    // Case: Vertex 2 (Top-Right: x+1, y+1) - New point in this quadrant
                    else if (i == 2)
                    {
                        // Always new if x and y are less than max. No reuse from x-1 or y-1.
                    }
                    // Case: Midpoint 7 (Left-Mid: x, y+0.5) - Shared by 2 cells (Left)
                    else if (i == 7)
                    {
                        // Try Left neighbor (x-1, y) as its 3 (Right-Mid)
                        if (x > 0 && vertexCache.TryGetValue((x - 1, y, 3), out globalIndex)) mapped = true;
                    }
                    // Case: Midpoint 5 (Bottom-Mid: x+0.5, y) - Shared by 2 cells (Below)
                    else if (i == 5)
                    {
                        // Try Below neighbor (x, y-1) as its 1 (Top-Mid)
                        if (y > 0 && vertexCache.TryGetValue((x, y - 1, 1), out globalIndex)) mapped = true;
                    }
                    // Case: Midpoints 1 and 3 (Top-Mid, Right-Mid) - Always new in this quadrant
                    else if (i == 1 || i == 3)
                    {
                        // Always new. No reuse from x-1 or y-1.
                    }

                    if (!mapped)
                    {
                        // Create a new global vertex
                        globalIndex = vertices.Count;
                        vertices.Add(localVerts[i]);
                    }

                    // Always cache the result for this cell's local index, whether reused or new
                    vertexCache[(x, y, i)] = globalIndex;
                    localToGlobal[i] = globalIndex;
                }

                // --- END ROBUST VERTEX REUSE LOGIC ---

                // Convert triDef (local indices 0..7) to global indices
                for (int t = 0; t < triDef.Length; t += 3)
                {
                    int a = localToGlobal[triDef[t]];
                    int b = localToGlobal[triDef[t + 1]];
                    int c = localToGlobal[triDef[t + 2]];

                    if (a == b || b == c || c == a) continue;

                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);
                }
            } // end for y
        } // end for x

        // Build the mesh
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter mf = GetComponent<MeshFilter>();
        mf.mesh = mesh;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Sprites/Default")) { color = Color.black };

        // 🚀 NEW: Add the Polygon Collider
        AddOptimizedPolygonCollider2D(vertices, triangles);
    }

    int calculateIndex(int[] values)
    {
        int ret = 0;
        // The cell values are read as: BL (0), BR (1), TR (2), TL (3)
        // Bit weights are: BL*1, BR*2, TR*4, TL*8
        for (int i = 0; i < values.Length; i++)
        {
            // Using bit shift for powers of 2 (1 << i)
            ret += values[i] * (1 << i);
        }
        return ret;
    }
    /// <summary>
    /// Adds or updates a PolygonCollider2D component using the generated mesh vertices.
    /// NOTE: For complex shapes or holes, this simple approach might be insufficient.
    /// A proper implementation would require a contour tracing algorithm.
    /// </summary>
    /// <summary>
    /// Implements a simple contour tracing algorithm (a flood-fill style edge follower)
    /// to find the external boundary of the solid mesh, reducing vertex count for the collider.
    /// </summary>
    /// <summary>
    /// Implements a full contour tracing algorithm that handles multiple disconnected shapes 
    /// (islands) by assigning each one a separate path on the PolygonCollider2D.
    /// </summary>
    void AddOptimizedPolygonCollider2D(List<Vector3> vertices, List<int> triangles)
    {
        PolygonCollider2D polyCollider = GetComponent<PolygonCollider2D>();
        if (polyCollider == null)
        {
            polyCollider = gameObject.AddComponent<PolygonCollider2D>();
        }

        // 1. Identify all unique edges and count their uses
        // Key: Tuple (v1, v2) where v1 < v2 (canonical form)
        // Value: Count (1 = exterior edge, 2 = interior edge)
        Dictionary<(int, int), int> edgeCounts = new Dictionary<(int, int), int>();

        for (int i = 0; i < triangles.Count; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            // Helper function logic for canonical storage
            AddOrIncrementEdge(edgeCounts, v1, v2);
            AddOrIncrementEdge(edgeCounts, v2, v3);
            AddOrIncrementEdge(edgeCounts, v3, v1);
        }

        // 2. Extract boundary edges (count == 1)
        List<(int vA, int vB)> boundaryEdges = new List<(int, int)>();
        foreach (var entry in edgeCounts)
        {
            if (entry.Value == 1)
            {
                // Edge used once -> must be an external edge
                // We store the non-canonical pair here to help the tracer find the next vertex easily
                boundaryEdges.Add(entry.Key);
            }
        }

        // 3. Trace ALL contours (paths)
        List<List<Vector2>> allPaths = new List<List<Vector2>>();
        int pathIndex = 0;

        // Keep tracing as long as there are untraced boundary edges left
        while (boundaryEdges.Count > 0)
        {
            List<Vector2> currentPath = new List<Vector2>();

            // Get the first edge of the next shape/island
            (int currentVertexIndex, int nextVertexIndex) = boundaryEdges[0];

            // Set the start and remove the edge from the list
            int startVertexIndex = currentVertexIndex;
            boundaryEdges.RemoveAt(0);

            // Add the starting point to the current path
            currentPath.Add(new Vector2(vertices[startVertexIndex].x, vertices[startVertexIndex].y));
            currentVertexIndex = nextVertexIndex; // Move to the second vertex

            // Loop to follow the path until we return to the start
            while (true)
            {
                // Add the current vertex to the path
                currentPath.Add(new Vector2(vertices[currentVertexIndex].x, vertices[currentVertexIndex].y));

                // Check if the loop is closed
                if (currentVertexIndex == startVertexIndex)
                {
                    // The loop is closed. Remove the redundant last point (which is the start point)
                    currentPath.RemoveAt(currentPath.Count - 1);
                    break;
                }

                // Find the next edge in the path
                bool foundNext = false;
                for (int i = 0; i < boundaryEdges.Count; i++)
                {
                    int vA = boundaryEdges[i].vA;
                    int vB = boundaryEdges[i].vB;
                    int partner = -1;

                    // If vA or vB matches the current vertex, the other is the partner
                    if (vA == currentVertexIndex)
                    {
                        partner = vB;
                    }
                    else if (vB == currentVertexIndex)
                    {
                        partner = vA;
                    }

                    if (partner != -1)
                    {
                        // Found the next edge!
                        currentVertexIndex = partner;
                        boundaryEdges.RemoveAt(i);
                        foundNext = true;
                        break;
                    }
                }

                // Safety break if we hit a dead end (e.g., if geometry is not a clean loop)
                if (!foundNext)
                {
                    Debug.LogWarning("Contour tracing hit a dead end! Path is incomplete.");
                    break;
                }
            }

            // Path tracing for this shape is complete. Save it.
            if (currentPath.Count >= 3) // A path must have at least 3 points to form a polygon
            {
                allPaths.Add(currentPath);
            }
        }

        // 4. Assign ALL paths to the PolygonCollider2D
        polyCollider.pathCount = allPaths.Count;

        for (int i = 0; i < allPaths.Count; i++)
        {
            polyCollider.SetPath(i, allPaths[i].ToArray());
        }
    }

    /// <summary>Helper function to manage canonical edge storage.</summary>
    void AddOrIncrementEdge(Dictionary<(int, int), int> edgeCounts, int vA, int vB)
    {
        // Canonical form: always store the edge with the smaller index first
        (int v1, int v2) edge = (Mathf.Min(vA, vB), Mathf.Max(vA, vB));

        if (edgeCounts.ContainsKey(edge))
        {
            edgeCounts[edge]++;
        }
        else
        {
            edgeCounts.Add(edge, 1);
        }
    }
}