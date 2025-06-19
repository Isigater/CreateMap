using System;
using System.Threading.Tasks;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;

public class CreateLandscape : MonoBehaviour
{
    [SerializeField] MeshFilter meshFilter;
    [SerializeField] MeshRenderer meshRenderer;
    private int size = 1500;
    int done = 0;
    bool isDone = false;

    private async void Start()
    {
        await StartMesh();
    }
    private void Update()
    {
        if (isDone) return;
        Debug.Log(done + " / " + size * size);
    }
    private async Task StartMesh()
    {
        // 格子点を頂点としてメッシュを生成する
        // 頂点の高さを AltitudeCalcuLator で計算してランダムにする
        AltitudeCalculatorByBrock altitudeCalculatorByBrock = new AltitudeCalculatorByBrock();
        AltitudeCalculatorByBrock2 altitudeCalculatorByBrock2 = new AltitudeCalculatorByBrock2();
        AltitudeCalculator altitudeCalculator = new AltitudeCalculator();
        Vector3[] vertices = new Vector3[size * size];
        Color[] colors = new Color[size * size];
                
        int oct = 3;
        float colMax = (float)(50 * 2 * (1-Math.Pow(1/2, oct)));//((50 + 50 / (int)Math.Pow(2, oct - 1)) * oct / 2);

        Func<int> asyncJob = () =>
        {
            // 頂点座標の計算
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    float alt = altitudeCalculatorByBrock2.GetAltitude(x, z, oct);
                    float col = 1 - alt / colMax;
                    col -= 0.15f;
                    Vector3 v = new Vector3(x, alt, z);
                    vertices[x * size + z] = v;
                    colors[x * size + z] = new Color(col, col, col);
                    done++;
                }
                //Debug.Log("x = " + x + "/" + size);
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
        isDone = true;
    }
}


public class AltitudeCalculator
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
        float freq = 1;
        const int FREQ_RATIO = 2;
        float amp = 1;
        const float AMP_RATIO = 0.5f;
        float altitude = 0f;
        for(int i = 0; i < oct; i++)
        {
            float noise = Mathf.PerlinNoise(x * freq/1000, z * freq/1000);
            Debug.Log(noise);
            altitude += Mathf.PerlinNoise(x * freq, z * freq) * ALTITUDE_LIMIT * amp;
            freq *= FREQ_RATIO;
            amp *= AMP_RATIO;
        }
        Debug.Log(altitude);
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

        //Mathf.PerlinNoise(x, y)

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

public class RandByXorshift
{
    private Int64 _x;
    private Int64 _y = 362436069;
    private Int64 _z = 521288629;
    private Int64 _w;

    public void setSeed(int x,int w)
    {
        int n = w % 3 + x % 7 + 9;
        _x = x + x << w;
        _w = w + w << x;
        _y = 362436069;
        _z = 521288629;
        for (int i = 0; i < n; i++) this.Rand();
    }
    public float Rand()
    {
        Int64 _t = _x ^ (_x << 11);
        _t ^= _t >> 8;
        _x = _y; _y = _z; _z = _w;
        _w = (_w ^ (_w >> 19)) ^ _t;
        //return _w;
        return (float)((double)_w /(double)0x7FFFFFFFFFFFFFFF);
    }
}

public class AltitudeCalculatorByBrock2
{
    // 100m四方を一つのブロックとして考える
    private const int BROCK_SIZE = 100;

    //高さ制限
    public const int ALTITUDE_LIMIT = 50;

    public double h = 0;
    RandByXorshift rand = new RandByXorshift();

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

    private float GetPerlinNoize(int x, int z)
    {
        // 格子点の整数座標
        int x0 = (x / BROCK_SIZE);
        int z0 = (z / BROCK_SIZE);
        int x1 = x0 + 1;
        int z1 = z0 + 1;

        // 小数部分（0～1）
        float xf = (float)(x % BROCK_SIZE) / BROCK_SIZE;
        float zf = (float)(z % BROCK_SIZE) / BROCK_SIZE;

        // 各格子点の勾配ベクトルを乱数で決定
        rand.setSeed(x0, z0);
        float angle00 = rand.Rand() * 2 * Mathf.PI;
        Vector2 grad00 = new Vector2(Mathf.Cos(angle00), Mathf.Sin(angle00));

        rand.setSeed(x1, z0);
        float angle10 = rand.Rand() * 2 * Mathf.PI;
        Vector2 grad10 = new Vector2(Mathf.Cos(angle10), Mathf.Sin(angle10));

        rand.setSeed(x0, z1);
        float angle01 = rand.Rand() * 2 * Mathf.PI;
        Vector2 grad01 = new Vector2(Mathf.Cos(angle01), Mathf.Sin(angle01));

        rand.setSeed(x1, z1);
        float angle11 = rand.Rand() * 2 * Mathf.PI;
        Vector2 grad11 = new Vector2(Mathf.Cos(angle11), Mathf.Sin(angle11));

        // 各格子点からの相対ベクトル
        Vector2 d00 = new Vector2(xf, zf);
        Vector2 d10 = new Vector2(xf - 1, zf);
        Vector2 d01 = new Vector2(xf, zf - 1);
        Vector2 d11 = new Vector2(xf - 1, zf - 1);

        // ドット積
        float dot00 = Vector2.Dot(grad00, d00);
        float dot10 = Vector2.Dot(grad10, d10);
        float dot01 = Vector2.Dot(grad01, d01);
        float dot11 = Vector2.Dot(grad11, d11);

        // Fade関数で補間
        float u = (float)Fade(xf);
        float v = (float)Fade(zf);

        // 線形補間
        float nx0 = Mathf.Lerp(dot00, dot10, u);
        float nx1 = Mathf.Lerp(dot01, dot11, u);
        float nxy = Mathf.Lerp(nx0, nx1, v);

        // 標準化してALTITUDE_LIMITを掛ける
        return nxy * ALTITUDE_LIMIT;
    }

    // 座標から高さを出す
    private float GetPerlinNoize2(int x, int z)
    {
        // 100m四方の格子点、その端数
        float xfloat = Math.Abs(x % BROCK_SIZE);
        float zfloat = Math.Abs(z % BROCK_SIZE);
        if (x < 0) xfloat = 100 - xfloat;
        if (z < 0) zfloat = 100 - zfloat;
        int xint = x - (int)xfloat;
        int zint = z - (int)zfloat;

        rand.setSeed(xint, zint);
        float alt00 = rand.Rand();
        rand.setSeed(xint + BROCK_SIZE, zint);
        float alt10 = rand.Rand();
        rand.setSeed(xint, zint + BROCK_SIZE);
        float alt01 = rand.Rand();
        rand.setSeed(xint + BROCK_SIZE, zint + BROCK_SIZE);
        float alt11 = rand.Rand();

        if(h < alt00) h = alt00;

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
            //altitude += GetPerlinNoize(x * freq, z * freq) * amp;
            altitude += (Mathf.PerlinNoise(x * freq, z * freq) - 0.5f) * amp * ALTITUDE_LIMIT;
            freq *= FREQ_RATIO;
            amp *= AMP_RATIO;
        }
        return altitude;
    }

}
