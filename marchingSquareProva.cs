//this is a simple unity implementation that just create a mesh given an index from the table taken at random;


using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class marchingSquareProva : MonoBehaviour
{
    
    public bool button = false;
    public int levelwidth;
    public int levelheight;
    //public int resolution;
    public float min;
    public float zoom;
    void Start()
    {
        calculateAndGenerate();
    }

    void Update()
    {
        if (button)
        {
            calculateAndGenerate();
            button = false;
        }
        
    }



    void calculateAndGenerate()
    {
        (float[,] perlinPoints, int[,] binaryPoints) = getpoints(levelwidth, levelheight, zoom, min, Random.Range(0,10000));

        List<Vector3> mainVertices = new List<Vector3>();
        List<int> mainTriangles = new List<int>();
        int offset = 0; //serve dentro il loop nestato

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
                
                (Vector3[] tempVertices, int[] tempTriangles) = getVerticesAndTriangles(new Vector3(x, y, 0), idx, offset);
                offset += 1;


                foreach (Vector3 tempV in tempVertices)
                {
                    mainVertices.Add(tempV);
                }
                foreach (int point in tempTriangles)
                {
                    mainTriangles.Add(point);
                }
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



        

        

        mesh.vertices = vertices;
        mesh.triangles = triangles;  

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.mesh = mesh;

        

        
        mr.material = new Material(Shader.Find("Sprites/Default"))
        {
            color = Color.green
        };
    }
    (Vector3[] ver, int[] tri) getVerticesAndTriangles(Vector3 point0, int indice, int idx0)
    {
        int[][] lookUpTable = new int[][]
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

        for(int i = 0; i < lookUpTable.Length; i++)
        {
            for(int j = 0; j < lookUpTable[i].Length; j++)
            {
                lookUpTable[i][j] += idx0*8;
            }
        }

        Vector3[] vertices = new Vector3[] {

            new Vector3(-1, 1, 0) + point0,
            new Vector3(0, 1, 0) + point0,
            new Vector3(1, 1, 0) + point0,
            new Vector3(1, 0, 0) + point0,
            new Vector3(1, -1, 0) + point0,
            new Vector3(0, -1, 0) + point0,
            new Vector3(-1, -1, 0) + point0,
            new Vector3(-1, 0, 0) + point0
        };

        int[] triangles = lookUpTable[indice];

        return (vertices, triangles);



    }

    (float[,], int[,]) getpoints(int x, int y, float k, float razzismo, float seed)
    {
        float[,] perlinRet = new float[x, y];
        int[,] binaryPerlinRet = new int[x, y];
        for(int i = 0; i < x; i++)
        {
            for(int j = 0; j < y; j++)
            {
                perlinRet[i, j] = Mathf.PerlinNoise(i*k + seed, j*k + seed);
                binaryPerlinRet[i, j] = perlinRet[i, j] > razzismo ? 1 : 0;
            }
        }



        return (perlinRet, binaryPerlinRet);
    }

    
    //values è un array composta da 0 e 1 di 4 elementi. la funzione serve a trovare l'indice del mesh giusto dalla lookup table
    /*
     
    0--------1
    |        |
    |        |
    |        |
    3--------2  
     */
    int calculateIndex(int[] values)
    {
        int ret = 0;
        for (int i = 0; i < values.Length; i++) { 

            ret += values[i] * (int)Mathf.Pow(2, i);

        }
        return ret;
    }

}
