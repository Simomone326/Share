//this is a simple unity implementation that just create a mesh given an index from the table taken at random;


using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class marchingSquares : MonoBehaviour
{


    public int levelwidth;
    public int[,] binaryPoints;
    public int levelheight;
    public float min;
    
    



    public void calculateAndGenerate()
    {

        
        List<Vector3> mainVertices = new List<Vector3>();
        List<int> mainTriangles = new List<int>();
        //int offset = 0; //serve dentro il loop nestato

        for (int x = 0; x < binaryPoints.GetLength(0) - 1; x++)
        {
            for (int y = 0; y < binaryPoints.GetLength(1) - 1; y++)
            {
                int[] mainValues = new int[4];
                mainValues[0] = binaryPoints[x, y];
                mainValues[1] = binaryPoints[x + 1, y];
                mainValues[2] = binaryPoints[x + 1, y + 1];
                mainValues[3] = binaryPoints[x, y + 1];

                int idx = calculateIndex(mainValues);

                // 🧠 Use consistent per-cell vertex offset
                int idx0 = y + x * (levelheight - 1);

                // 🧱 Generate local vertices + triangles
                (Vector3[] tempVertices, int[] tempTriangles) = getVerticesAndTriangles(
                    new Vector3(x, y, 0),
                    idx,
                    idx0
                );

                // Append to the master lists
                mainVertices.AddRange(tempVertices);
                mainTriangles.AddRange(tempTriangles);
            }
        }



        int[] mainTrianglesArray = mainTriangles.ToArray();
        Vector3[] mainVerticesArray = mainVertices.ToArray();

        genMesh(mainVerticesArray, mainTrianglesArray);
    }
    void genMesh(Vector3[] vertices, int[] triangles)
    {
        


        

        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;






        mesh.vertices = vertices;
        mesh.triangles = triangles;  

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.mesh = mesh;




        mr.material = new Material(Shader.Find("Sprites/Default"))
        {
            color = Color.black
        };
    }
    (Vector3[] ver, int[] tri) getVerticesAndTriangles(Vector3 point0, int indice, int idx0)
    {
        int[][] baseLookUpTable = new int[][]
        {
        new int[] {},
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
        new int[] {4, 2, 0, 0, 6, 4}
        };

        // Create a local copy of the triangle list for this cell
        int[] triangles = (int[])baseLookUpTable[indice].Clone();

        // Apply per-cell vertex offset
        for (int j = 0; j < triangles.Length; j++)
        {
            triangles[j] += idx0 * 8;
        }

        Vector3[] vertices = new Vector3[]
        {
        new Vector3(0, 1, 0) + point0,    // 0
        new Vector3(0.5f, 1, 0) + point0, // 1
        new Vector3(1, 1, 0) + point0,    // 2
        new Vector3(1, 0.5f, 0) + point0, // 3
        new Vector3(1, 0, 0) + point0,    // 4
        new Vector3(0.5f, 0, 0) + point0, // 5
        new Vector3(0, 0, 0) + point0,    // 6
        new Vector3(0, 0.5f, 0) + point0  // 7
        };

        return (vertices, triangles);
    }

    int calculateIndex(int[] values)
    //values è un array composta da 0 e 1 di 4 elementi. la funzione serve a trovare l'indice del mesh giusto dalla lookup table
    /*
     
    0--------1
    |        |
    |        |
    |        |
    3--------2  
     */
    {
        int ret = 0;
        for (int i = 0; i < values.Length; i++) { 

            ret += values[i] * (int)Mathf.Pow(2, i);

        }
        return ret;
    }



}
