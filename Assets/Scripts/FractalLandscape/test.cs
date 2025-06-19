using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        Debug.Log(-10 % 7);
        RandByXorshift randByXorshift = new RandByXorshift();
        randByXorshift.setSeed(1000,1000);
        for (int i = 0; i < 10; i++)
        {
            Debug.Log("seed : 1000,1000    " + randByXorshift.Rand());
        }
        randByXorshift.setSeed(500, 700);
        for (int i = 0; i < 10; i++)
        {
            Debug.Log("seed : 500,700    " + randByXorshift.Rand());
        }
        randByXorshift.setSeed(1000, 1000);
        for (int i = 0; i < 10; i++)
        {
            Debug.Log("seed : 1000,1000    " + randByXorshift.Rand());
        }
        randByXorshift.setSeed(500, 700);
        for (int i = 0; i < 10; i++)
        {
            Debug.Log("seed : 500,700    " + randByXorshift.Rand());
        }
        /*
        Debug.Log(-100);
        //Test();
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        sw.Start();
        for (int n = 0; n < 1000000; n++)
        {
            //TestRandom(n);
        }
        sw.Stop();
        //Debug.Log(sw.ElapsedMilliseconds);
        //TestRandom2(1000);
        */
    }

    public void Test()
    {
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        UTF8Encoding ue = new UTF8Encoding();
        MD5 md5 = new MD5CryptoServiceProvider();
        System.Random rnd = new System.Random();
        int i = 0;
        sw.Start();
        for (i = 0; i < 1000000; i++)
        {
            int x = rnd.Next(1000);
            int z = rnd.Next(1000);
            byte[] hashBytes = md5.ComputeHash(ue.GetBytes(x.ToString() + z.ToString()));
            int seed = 0;
            seed += hashBytes[0];
        }
        sw.Stop();
        Debug.Log(sw.ElapsedMilliseconds);
        int k = 0;
        sw.Restart();
        for (k = 0; k < 1000000; k++)
        {
            int x = rnd.Next(1000);
            int z = rnd.Next(1000);
            byte[] hashBytes = md5.ComputeHash(ue.GetBytes(x.ToString() + z.ToString()));
            int seed = 0;
            for (int j = 0; j < hashBytes.Length; j++)
            {
                seed += hashBytes[j];
            }
        }
        sw.Stop();
        Debug.Log(sw.ElapsedMilliseconds);
        Debug.Log("i = " + i + ", k = " + k);
        return;
    }
    private void TestRandom(int a)
    {
        UnityEngine.Random.InitState(a);
        float alt00 = UnityEngine.Random.Range(0f, 1f);
        UnityEngine.Random.InitState(a + 1);
        float alt01 = UnityEngine.Random.Range(0f, 1f);
        UnityEngine.Random.InitState(a + 2);
        float alt10 = UnityEngine.Random.Range(0f, 1f);
        UnityEngine.Random.InitState(a + 3);
        float alt11 = UnityEngine.Random.Range(0f, 1f);
    }
    private void TestRandom2(int a)
    {
        for(int i = 0; i < a; i += 100)
        {
            UnityEngine.Random.InitState(i);
            float b = UnityEngine.Random.Range(0f, 100f);
            Debug.Log(b);
        }
    }
}
