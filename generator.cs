using System.IO.Hashing;
using UnityEngine;
using UnityEngine.Rendering;

public class generator : MonoBehaviour
{
    public bool button = false;
    public int worms = 6;
    int Sx;
    int Sy;
    public GameObject player;
    Vector2 prevpos;
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
    [Range(0.0f, Mathf.PI * 2f)]
    public float minsize = 2;
    public float distToGenChunk = 2;

    // --- NEW: Stored full map and visibility settings ---
    private int[,] fullGeneratedMap;

    [Header("Visibility Settings")]
    public int visibleRadius = 64;
    // --- END NEW ---

    void Start()
    {
        Sx = levelsize / 2;
        Sy = levelsize / 2;
        // Ensure the player starts in the middle of the generated grid
        if (player != null)
        {
            player.transform.position = new Vector3(Sx, Sy, 0);
        }
        prevpos = new Vector2(Sx, Sy);
    }


    void Update()
    {
        Vector2 playerPos = player.transform.position;
        float dPow2 = (playerPos.x - prevpos.x)* (playerPos.x - prevpos.x) + (playerPos.y - prevpos.y)*(playerPos.y - prevpos.y);   

        // Only run if the map has been generated and the player exists
        if (fullGeneratedMap != null && player != null && dPow2 > (visibleRadius/distToGenChunk)* (visibleRadius / distToGenChunk))
        {

            Vector2 currentPos = player.transform.position;
            SendMapToMarchingSquares(currentPos);
            prevpos = currentPos;
        }

        if (button)
        {
            // 1. Generate the full map and store it once.
            fullGeneratedMap = generateMap();

            // 2. Initial mesh generation based on start position.
            if (player != null)
            {
                Vector2 startPos = player.transform.position;
                SendMapToMarchingSquares(startPos);
            }

            seed4Noise = Random.Range(0, 1000000);
            seed4Size = Random.Range(0, 1000000);

            button = false;
        }
    }

    // --- FIX 1: Send the mesh the required world offset ---
    public void SendMapToMarchingSquares(Vector2 playerPos)
    {
        if (fullGeneratedMap == null) return;

        // Get the local map segment and its start coordinates
        (int[,] localMap, int startX, int startY) = getVisibleMap(playerPos);

        marchingSquares march = gameObject.GetComponent<marchingSquares>();

        // IMPORTANT: The dimensions passed to marchingSquares must match the size of localMap.
        march.levelwidth = localMap.GetLength(0);
        march.levelheight = localMap.GetLength(1);
        march.binaryPoints = localMap;
        march.min = min;

        // FIX: Update the MarchingSquares transform position based on the calculated startX/startY
        // This moves the mesh to the correct location in world space.
        march.transform.position = new Vector3(startX, startY, 0f);

        march.calculateAndGenerate();
    }


    // --- FIX 2: Return start coordinates and handle out-of-range boundaries correctly (fill with 0) ---
    /// <summary>
    /// Calculates and returns a subset of the full map centered around the player, 
    /// filling boundary areas outside the full map with 0 (void).
    /// </summary>
    /// <param name="centerPos">The center point for extraction (player's grid position).</param>
    /// <returns>A tuple containing the local map, and its global start X and Y coordinates.</returns>
    (int[,], int, int) getVisibleMap(Vector2 centerPos)
    {
        int mapSize = levelsize;
        int localWidth = visibleRadius;
        int localHeight = visibleRadius;
        int halfRadius = visibleRadius / 2;

        // Calculate the center point on the grid
        int centerX = Mathf.FloorToInt(centerPos.x);
        int centerY = Mathf.FloorToInt(centerPos.y);

        // Calculate the true global starting corner of the local map (may be negative)
        int globalStartX = centerX - halfRadius;
        int globalStartY = centerY - halfRadius;

        int[,] localMap = new int[localWidth, localHeight];

        // Copy the relevant data from the full map to the local map
        for (int x = 0; x < localWidth; x++)
        {
            for (int y = 0; y < localHeight; y++)
            {
                // Calculate the corresponding coordinate in the full map grid
                int globalX = globalStartX + x;
                int globalY = globalStartY + y;

                // Check if the global coordinate is within the boundaries of the full map
                if (globalX >= 0 && globalX < mapSize && globalY >= 0 && globalY < mapSize)
                {
                    // Copy data from the full map
                    localMap[x, y] = fullGeneratedMap[globalX, globalY];
                }
                else
                {
                    // If outside the map range, fill with 0 (Void/Empty) as requested.
                    localMap[x, y] = 0;
                }
            }
        }

        // Return the local map and its calculated global start position
        return (localMap, globalStartX, globalStartY);
    }

    // ... (The rest of the unchanged functions: generateMap, eat, Move, RandomWithSeed) ...
    // ... (The implementation of generateMap, eat, Move, and RandomWithSeed should follow unchanged) ...

    int[,] generateMap()
    {
        // ... (The rest of the unchanged generateMap logic) ...
        int pointsNum = levelsize;
        int[,] fullMap = new int[pointsNum, pointsNum];

        // Initialize map to 1 (SOLID/WALL)
        for (int i = 0; i < fullMap.GetLength(0); i++)
        {
            for (int j = 0; j < fullMap.GetLength(1); j++)
            {
                fullMap[i, j] = 1;
            }
        }

        // Generate worms (eat() sets tunnels to 0)
        for (int wormCounter = 0; wormCounter < worms; wormCounter++)
        {

            float randomValueSize = RandomWithSeed((int)seed4Noise + wormCounter) * 100000;
            float[,] sizePerlin = new float[pointsNum, pointsNum];
            for (int i = 0; i < sizePerlin.GetLength(0); i++)
            {
                for (int j = 0; j < sizePerlin.GetLength(1); j++)
                {
                    sizePerlin[i, j] = Mathf.Clamp(Mathf.PerlinNoise(seed4Size + randomValueSize + i * SizeScale, seed4Size + randomValueSize + j * SizeScale) * SizeScaleRadius, 1, SizeScaleRadius);
                }
            }


            (int x, int y) = (Random.Range(0, levelsize), Random.Range(0, levelsize));
            Vector2 pos = new Vector2();

            if (wormCounter == 0)
            {
                pos = new Vector2(Sx, Sy);
            }
            else
            {
                pos = new Vector2(Random.Range(0, levelsize), Random.Range(0, levelsize));
            }



            float heading = Random.Range(0, 2f * Mathf.PI);


            bool alive = true;
            int sos = 0;
            float randomValue = RandomWithSeed((int)seed4Noise + wormCounter) * 100000;
            while (alive && sos < 100000)
            {
                // Smooth heading
                float nx = Mathf.PerlinNoise((pos.x + randomValue) * NoiseScale + seed4Noise, (pos.y) * NoiseScale + seed4Noise);
                float ny = Mathf.PerlinNoise((pos.x) * NoiseScale + seed4Noise, (pos.y + randomValue) * NoiseScale + seed4Noise);
                nx = (nx - 0.5f) * 2f;
                ny = (ny - 0.5f) * 2f;

                float noiseAngle = Mathf.Atan2(ny, nx);
                heading = Mathf.LerpAngle(heading, noiseAngle, lerpValue);

                int wormX = Mathf.FloorToInt(pos.x);
                int wormY = Mathf.FloorToInt(pos.y);

                int radius = constantSize > 0 ? constantSize : 1;
                if (wormX >= 0 && wormX < levelsize && wormY >= 0 && wormY < levelsize)
                {
                    radius = constantSize > 0 ? constantSize : Mathf.Max(1, (int)sizePerlin[wormX, wormY]);
                }

                eat(fullMap, pos, radius);

                (Vector2 newPos, float newAngle) = Move(pos, heading, 1f, levelsize, levelsize);

                if (newPos.x < 0 || newPos.y < 0)
                    alive = false;
                else
                    pos = newPos;
                sos++;
            }
        }

        return fullMap;
    }

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
                float dx = x - center.x;
                float dy = y - center.y;
                if (dx * dx + dy * dy <= r2)
                {
                    map[x, y] = 0; // Carve out the tunnel (void)
                }
            }
        }
    }


    (Vector2, float) Move(Vector2 currentPos, float angle, float stepSize = 1f, int mapWidth = 0, int mapHeight = 0)
    {
        Vector2 newPos = currentPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * stepSize;

        if (mapWidth > 0 && mapHeight > 0)
        {
            if (newPos.x < 0 || newPos.x >= mapWidth || newPos.y < 0 || newPos.y >= mapHeight)
            {
                return (new Vector2(-1f, -1f), angle);
            }
        }

        return (newPos, angle);
    }

    float RandomWithSeed(int seed)
    {
        System.Random rand = new System.Random(seed);
        return (float)rand.NextDouble();
    }
}