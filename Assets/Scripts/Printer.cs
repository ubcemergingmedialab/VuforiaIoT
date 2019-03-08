using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;
using Image = UnityEngine.UI.Image;

public class Printer : MonoBehaviour, ITrackableEventHandler
{

    [SerializeField]
    private GameObject imageTarget;
    [SerializeField]
    private GameObject canvas;

    [SerializeField]
    private Image lSensor;
    [SerializeField]
    private Image fSensor;
    [SerializeField]
    private Image direction;

    [SerializeField]
    private Text lText;
    [SerializeField]
    private Text fText;
    [SerializeField]
    private Text dText;

    public int front;
    public int left;
    public string dir;

    [SerializeField]
    private int threshold;

    private Color initColor;

    private TrackableBehaviour mTrackableBehaviour;
    private bool targetFound;

    public void OnTrackableStateChanged(TrackableBehaviour.Status previousStatus, TrackableBehaviour.Status newStatus)
    {
        if (newStatus == TrackableBehaviour.Status.DETECTED ||
            newStatus == TrackableBehaviour.Status.TRACKED ||
            newStatus == TrackableBehaviour.Status.EXTENDED_TRACKED)
        {
            targetFound = true; //when target is found
        }
        else
        {
            targetFound = false; //when target is lost
        }
    }



    // Use this for initialization
    void Start()
    {
        initColor = lSensor.color;
        mTrackableBehaviour = imageTarget.GetComponent<TrackableBehaviour>();
        if (mTrackableBehaviour)
        {
            mTrackableBehaviour.RegisterTrackableEventHandler(this);
        }
    }

    void Update()
    {
        if (targetFound)
        {
            canvas.SetActive(true);
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit(); // exit app if back/esc button is pressed
            }


            SetLeft();
            SetFront();
            SetDirection();


        }

        if (!targetFound)
        {
            canvas.SetActive(false);
        }
    }


    private void SetLeft()
    {
        if(left > 0)
        {
            if (left < threshold)
            {
                lSensor.color = Color.red;

            }
            else
            {
                lSensor.color = initColor;
            }
            lText.text = left.ToString();
        }
        
    }
    private void SetFront()
    {
        if(front > 0)
        {
            if (front < threshold)
            {
                fSensor.color = Color.red;

            }
            else
            {
                fSensor.color = initColor;
            }
            fText.text = front.ToString();
        }
        
    }

    private void SetDirection()
    {
        if (!string.IsNullOrEmpty(dir))
        {
            dText.text = dir;
        }
    }
}
