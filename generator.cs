using System.IO.Hashing;
using UnityEngine;

public class generator : MonoBehaviour
{

    public bool button = false;
    public int worms = 6;
    int Sx;
    int Sy;
    public int levelsize = 1000;
    public float min = 0.5f;
    public float seed4Noise = 0;
    public float NoiseScale = 0.02f;
    public float seed4Size = 0;
    public float SizeScale = 0.002f;
    public float SizeScaleRadius = 20;
    public int constantSize = -1;
    [Range(0.0f, 1.0f)]
    public float lerpValue = 0.47f;
    [Range(0.0f, Mathf.PI*2f)]
    public float minsize = 2;
    
    void Start()
    {
        Sx = levelsize / 2;
        Sy = levelsize / 2;
    }

    // Update is called once per frame
    void Update()
    {
        
        

        if (button)
        {
            int[,] map = generateMap();
            
            marchingSquares march = gameObject.GetComponent<marchingSquares>();
            march.levelwidth = levelsize;
            march.levelheight = levelsize;
            march.binaryPoints = map;
            march.min = min;
            
            march.calculateAndGenerate();



            seed4Noise = Random.Range(0, 1000000);
            seed4Size = Random.Range(0, 1000000);

            button = false;
        }
    }

    int[,] generateMap()
    {
        int pointsNum = levelsize;
        int[,] fullMap = new int[pointsNum, pointsNum];
        for (int i = 0; i < fullMap.GetLength(0); i++)
        {
            for (int j = 0; j < fullMap.GetLength(1); j++)
            {
                fullMap[i, j] = 1;
            }
        }






        for (int wormCounter = 0; wormCounter < worms; wormCounter++)
        {
            

            int[,] binaryRet = new int[pointsNum, pointsNum];

            float[,] wormPerlin = new float[pointsNum, pointsNum];
            float[,] sizePerlin = new float[pointsNum, pointsNum];
            

            for (int i = 0; i < wormPerlin.GetLength(0); i++)
            {
                for (int j = 0; j < wormPerlin.GetLength(1); j++)
                {
                    wormPerlin[i, j] = Mathf.PerlinNoise(seed4Noise + i * NoiseScale, seed4Noise + j * NoiseScale);
                }
            }
            float randomValueSize = RandomWithSeed((int)seed4Noise + wormCounter) * 100000;
            for (int i = 0; i < sizePerlin.GetLength(0); i++)
            {
                for (int j = 0; j < sizePerlin.GetLength(1); j++)
                {
                    sizePerlin[i, j] = Mathf.Clamp( Mathf.PerlinNoise(seed4Size + randomValueSize + i * SizeScale, seed4Size + randomValueSize + j * SizeScale) * SizeScaleRadius, 1, SizeScaleRadius);
                }
            }

            


            (int x, int y) = (Random.Range(0, levelsize), Random.Range(0, levelsize));

            Vector2 pos = new Vector2();

            if(wormCounter == 0)
            {
                pos = new Vector2(Sx, Sy);
            }
            else
            {
                pos = new Vector2(Random.Range(0, Sx), Random.Range(0, 2 * Sy));
            }



                float heading = Random.Range(0, 2f * Mathf.PI);
            

            bool alive = true;
            int sos = 0;
            float randomValue = RandomWithSeed((int)seed4Noise + wormCounter) * 100000;
            while (alive && sos < 100000)
            {
                // Smooth heading
                // Create a directional vector from two separate noise fields
                float nx = Mathf.PerlinNoise((pos.x + randomValue) * NoiseScale + seed4Noise, (pos.y) * NoiseScale + seed4Noise);
                float ny = Mathf.PerlinNoise((pos.x) * NoiseScale + seed4Noise, (pos.y + randomValue) * NoiseScale + seed4Noise);
                nx = (nx - 0.5f) * 2f;
                ny = (ny - 0.5f) * 2f;

                // Convert that vector into an angle
                float noiseAngle = Mathf.Atan2(ny, nx);

                // Smoothly blend the heading
                heading = Mathf.LerpAngle(heading, noiseAngle, lerpValue);

                int radius = constantSize > 0 ? constantSize : Mathf.Max(1, (int)sizePerlin[(int)pos.x, (int)pos.y]);
                eat(fullMap, pos, radius);

                (Vector2 newPos, float newAngle) = Move(pos, heading, 1f, levelsize, levelsize);

                if (newPos.x < 0 || newPos.y < 0)  // out of bounds flag
                    alive = false;
                else
                    pos = newPos;
                sos++;
            }
            if (sos == 100000)
            {
                print("phew");
            }



        }
        


        return fullMap;
        
    }

    //non serve un return perchè map è un pointer alla stessa regione di memoria quindi modificando map modifico anche il parametro che ci ho inserito (fullMap)
    void eat(int[,] map, Vector2 center, float radius)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
        int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + radius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
        int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(center.y + radius));

        float r2 = radius * radius;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                // Distance from the floating center
                float dx = x - center.x;
                float dy = y - center.y;
                if (dx * dx + dy * dy <= r2)
                {
                    map[x, y] = 0;
                }
            }
        }
    }


    (Vector2, float) Move(Vector2 currentPos, float angle, float stepSize = 1f, int mapWidth = 0, int mapHeight = 0)
    {
        // Compute new position based on heading and step size
        Vector2 newPos = currentPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * stepSize;

        // Check map bounds (if provided)
        if (mapWidth > 0 && mapHeight > 0)
        {
            if (newPos.x < 0 || newPos.x >= mapWidth || newPos.y < 0 || newPos.y >= mapHeight)
            {
                // Worm has gone out of bounds — return flag value, but still keep angle valid
                return (new Vector2(-1f, -1f), angle);
            }
        }

        // Normal case: still inside map
        return (newPos, angle);
    }

    float RandomWithSeed(int seed)
    {
        System.Random rand = new System.Random(seed);
        return (float)rand.NextDouble(); // Returns a value between 0 and 1
    }


}
