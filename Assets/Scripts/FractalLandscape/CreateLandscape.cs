using System;
using System.Threading.Tasks;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;

public class CreateLandscape : MonoBehaviour
{
    [SerializeField] MeshFilter meshFilter;
    [SerializeField] MeshRenderer meshRenderer;
    private int size = 5000;

    private async void Start()
    {
        await StartMesh();
    }
    private async Task StartMesh()
    {
        // 格子点を頂点としてメッシュを生成する
        // 頂点の高さを AltitudeCalcuLator で計算してランダムにする
        AltitudeCalcuLator altitudeCalcuLator = new AltitudeCalcuLator();
        Vector3[] vertices = new Vector3[size * size];
        Color[] colors = new Color[size * size];
                
        int oct = 1;
        float colMax = ((50 + 50 / (int)Math.Pow(2, oct - 1)) * oct / 2);

        Func<int> asyncJob = () =>
        {
            // 頂点座標の計算
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    float alt = altitudeCalcuLator.GetAltitude(x, z, oct);
                    float col = 1 - alt / colMax;
                    Vector3 v = new Vector3(x, alt, z);
                    vertices[x * size + z] = v;
                    colors[x * size + z] = new Color(col, col, col);
                }
                Debug.Log("x = " + x + "/" + size);
            }
            return 0;
        };
        await Task.Run(asyncJob);

        // 以下メッシュ関連
        int triangleIndex = 0;
        int[] triangles = new int[(size - 1) * (size - 1) * 6];

        for (int x = 0; x < size - 1; x++)
        {
            for (int z = 0; z < size - 1; z++)
            {
                int a = x * size + z;
                int b = a + 1;
                int c = a + size;
                int d = c + 1;

                triangles[triangleIndex] = a;
                triangles[triangleIndex + 1] = b;
                triangles[triangleIndex + 2] = c;

                triangles[triangleIndex + 3] = c;
                triangles[triangleIndex + 4] = b;
                triangles[triangleIndex + 5] = d;

                triangleIndex += 6;
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;

        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }
}


public class AltitudeCalcuLator
{
    // 100m四方を一つのブロックとして考える
    private const int BROCK_SIZE = 64;

    //高さ制限
    public const int ALTITUDE_LIMIT = 50;

    // 五次補完関数 6t^5 - 15t^4 + 10t^3
    private double Fade(double t)
    {
        double num = (t * t * t * (6 * t * t - 15 * t + 10));
        return num;
    }

    // 線形補完
    private float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    // ランダム関数のシード値生成
    private int GetSeed(int x, int z)
    {
        // ↓↓↓ 一つ目のやり方
        //return int.Parse(x.ToString() + z.ToString());
        // 一つ目ここまで


        // ↓↓↓ 二つ目のやり方
        /*UTF8Encoding ue = new UTF8Encoding();
        MD5 md5 = new MD5CryptoServiceProvider();
        byte[] hashBytes = md5.ComputeHash(ue.GetBytes(x.ToString() + z.ToString()));　// これが重い
        int seed = 0;
        for (int i = 0; i < hashBytes.Length; i++)
        {
            seed += hashBytes[i];
        }
        return seed;
        */
        // 二つ目ここまで
        return x * 5000 + z;
    }

    private float GetRandom(int x, int z)
    {
        Vector2 vec = new Vector2(x / 16, z / 16);
        return ((Mathf.Sin(Vector2.Dot(vec, new Vector2(12.9898f, 78.233f))) * 43758.5453f) % 1f) / 2f + 0.5f;
    }

    // 座標から高さを出す
    private float GetPerlinNoize(int x, int z) 
    {
        // 100m四方の格子点、その端数
        float xfloat = Math.Abs(x % BROCK_SIZE);
        float zfloat = Math.Abs(z % BROCK_SIZE);
        if (x < 0) xfloat = 100 - xfloat;
        if (z < 0) zfloat = 100 - zfloat;
        int xint = x - (int)xfloat;
        int zint = z - (int)zfloat;
        System.Random random = new System.Random();

        /* 格子点の値生成
         *   01  11
         *   00  10
         */

        float alt00 = GetRandom(xint, zint);
        float alt01 = GetRandom(xint, zint + BROCK_SIZE);
        float alt10 = GetRandom(xint + BROCK_SIZE, zint);
        float alt11 = GetRandom(xint + BROCK_SIZE, zint + BROCK_SIZE);

        float xRatio = xfloat / BROCK_SIZE;// (float)Fade(xfloat / BROCK_SIZE);
        float zRatio = zfloat / BROCK_SIZE;// (float)Fade(zfloat / BROCK_SIZE);
        return Lerp(Lerp(alt00, alt10, xRatio), Lerp(alt01, alt11, xRatio), zRatio) * ALTITUDE_LIMIT;
        //return GetRandom(x, z) * ALTITUDE_LIMIT;
    }

    // ノイズを幅と高さを変えて重ねてより細かく複雑にする
    // octはオクターブの数
    public float GetAltitude(int x, int z, int oct) 
    {
        int freq = 1;
        const int FREQ_RATIO = 2;
        float amp = 1;
        const float AMP_RATIO = 0.5f;
        float altitude = 0f;
        for(int i = 0; i < oct; i++)
        {
            altitude += Mathf.PerlinNoise(x * freq, z * freq) * ALTITUDE_LIMIT * amp;
            freq *= FREQ_RATIO;
            amp *= AMP_RATIO;
        }
        return altitude;
    }
}

public class AltitudeCalculatorByBrock
{
    // 100m四方を一つのブロックとして考える
    private const int BROCK_SIZE = 100;

    //高さ制限
    public const int ALTITUDE_LIMIT = 50;

    // 五次補完関数 6t^5 - 15t^4 + 10t^3
    private double Fade(double t)
    {
        double num = (t * t * t * (6 * t * t - 15 * t + 10));
        return num;
    }

    // 線形補完
    private float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    // ランダム関数のシード値生成
    private int GetSeed(int x, int z)
    {
        // ↓↓↓ 一つ目のやり方
        //return int.Parse(x.ToString() + z.ToString());
        // 一つ目ここまで


        // ↓↓↓ 二つ目のやり方
        /*UTF8Encoding ue = new UTF8Encoding();
        MD5 md5 = new MD5CryptoServiceProvider();
        byte[] hashBytes = md5.ComputeHash(ue.GetBytes(x.ToString() + z.ToString()));　// これが重い
        int seed = 0;
        for (int i = 0; i < hashBytes.Length; i++)
        {
            seed += hashBytes[i];
        }
        return seed;
        */
        // 二つ目ここまで

        return x * 5000 + z;
    }

    private float GetRandom(int x, int z)
    {
        Vector2 vec = new Vector2(x, z);
        return Mathf.Sin(Vector2.Dot(vec, new Vector2(12.9898f, 78.233f))) * 43758.5453f % 1f;
    }

    // 座標から高さを出す
    private float GetPerlinNoize(int x, int z)
    {
        // 100m四方の格子点、その端数
        float xfloat = Math.Abs(x % BROCK_SIZE);
        float zfloat = Math.Abs(z % BROCK_SIZE);
        if (x < 0) xfloat = 100 - xfloat;
        if (z < 0) zfloat = 100 - zfloat;
        int xint = x - (int)xfloat;
        int zint = z - (int)zfloat;
        System.Random random = new System.Random();

        /* 格子点の値生成
         *   01  11
         *   00  10
         */
        /*
        UnityEngine.Random.InitState(GetSeed(xint, zint));
        float alt00 = UnityEngine.Random.Range(0f, 1f);
        UnityEngine.Random.InitState(GetSeed(xint, zint + BROCK_SIZE));
        float alt01 = UnityEngine.Random.Range(0f, 1f);
        UnityEngine.Random.InitState(GetSeed(xint + BROCK_SIZE, zint));
        float alt10 = UnityEngine.Random.Range(0f, 1f);
        UnityEngine.Random.InitState(GetSeed(xint + BROCK_SIZE, zint + BROCK_SIZE));
        float alt11 = UnityEngine.Random.Range(0f, 1f);
        */

        float alt00 = GetRandom(xint,              zint             );
        float alt01 = GetRandom(xint,              zint + BROCK_SIZE);
        float alt10 = GetRandom(xint + BROCK_SIZE, zint             );
        float alt11 = GetRandom(xint + BROCK_SIZE, zint + BROCK_SIZE);


        float xRatio = (float)Fade(xfloat / BROCK_SIZE);
        float zRatio = (float)Fade(zfloat / BROCK_SIZE);
        return Lerp(Lerp(alt00, alt10, xRatio), Lerp(alt01, alt11, xRatio), zRatio) * ALTITUDE_LIMIT;
    }

    // ノイズを幅と高さを変えて重ねてより細かく複雑にする
    // octはオクターブの数
    public float GetAltitude(int x, int z, int oct)
    {
        int freq = 1;
        const int FREQ_RATIO = 2;
        float amp = 1;
        const float AMP_RATIO = 0.5f;
        float altitude = 0f;
        for (int i = 0; i < oct; i++)
        {
            altitude += GetPerlinNoize(x * freq, z * freq) * amp;
            freq *= FREQ_RATIO;
            amp *= AMP_RATIO;
        }
        return altitude;
    }

}