using System.Collections;
using UnityEngine;

public class ReadStream : MonoBehaviour
{
    [SerializeField]
    private string photonUrl;
    WebStreamReader request = null;

    void Start()
    {
        StartCoroutine(WRequest());
    }

    IEnumerator WRequest()
    {
        request = new WebStreamReader();
        request.Start(photonUrl);
        string stream = "";
        while (true)
        {
            string block = request.GetNextBlock();
            if (!string.IsNullOrEmpty(block))
            {
                stream += block;
                string[] data = stream.Split(new string[] { "\n\n" }, System.StringSplitOptions.None);
                stream = data[data.Length - 1];
                for (int i = 0; i < data.Length - 1; i++)
                {
                    if (!string.IsNullOrEmpty(data[i]))
                    {
                        //Parse text here
                    }
                }
            }
            yield return new WaitForSeconds(1);
        }
    }

    void OnApplicationQuit()
    {
        if (request != null)
            request.Dispose();
    }

    void OnDataUpdate(decimal aAmount)
    {
        Debug.Log("Received new amount: " + aAmount);
        // Do whatever you want with the value
    }
}
