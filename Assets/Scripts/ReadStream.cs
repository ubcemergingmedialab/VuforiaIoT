using System.Collections;
using UnityEngine;

public class ReadStream : MonoBehaviour
{
    private Printer printer;
    [SerializeField]
    private string photonUrl;
    WebStreamReader request = null;

    FrontSensorData parseFront = new FrontSensorData();
    LeftSensorData parseLeft = new LeftSensorData();
    DirectionData parseDirection = new DirectionData();

    void Start()
    {
        printer = this.GetComponent<Printer>();
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
                        if (data[i].Contains("Direction"))
                        {
                            Debug.Log(data[i]);
                            string output = data[i].Substring(data[i].IndexOf("{"));
                            Debug.Log(output);
                            parseDirection = JsonUtility.FromJson<DirectionData>(output);
                            Debug.Log(parseDirection.data);
                            printer.dir = parseDirection.data;
                        }
                        if (data[i].Contains("leftDistance"))
                        {
                            string output = data[i].Substring(data[i].IndexOf("{"));
                            parseLeft = JsonUtility.FromJson<LeftSensorData>(output);
                            printer.left = parseLeft.data;
                        }
                        if (data[i].Contains("frontDistance"))
                        {
                            string output = data[i].Substring(data[i].IndexOf("{"));
                            parseFront = JsonUtility.FromJson<FrontSensorData>(output);
                            printer.front = parseFront.data;
                        }

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

    public class FrontSensorData
    {
        public int data;
    }
    public class LeftSensorData
    {
        public int data;
    }
    public class DirectionData
    {
        public string data;
    }
}
