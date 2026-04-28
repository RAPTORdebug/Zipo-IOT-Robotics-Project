using UnityEngine;
using UnityEngine.UI;

// Changes TicksPerCell, TargetAngleZ and CellDistance in Firebase using UI
public class CalibaratingScript : MonoBehaviour
{
    public MainControlScript mcs;
    public FirebaseManager fm;
    public GameObject menu;
    
    public Button[] buttons;
    
    private int startAction;
    private bool actionFinished;

    public InputField tazField, tpcField, cdField;
    
    private int targetAngleZ;
    private int ticksPerCell;
    private int cellDistance;
    
    private NotificationSystem notificationSystem;
    
    void Start()
    {
        notificationSystem = GetComponent<NotificationSystem>();
        
        notificationSystem.SendNotification("Calibrate the movement of the robot");
        
        Debug.Log("Calibration Started");
        foreach (Button b in buttons)
        {
            b.interactable = false;
        }

        fm.ReadInt("car/ticksPerCell", (value) =>
        {
            ticksPerCell = value;
            tpcField.text = value.ToString();
            fm.ReadInt("car/targetAngleZ", (val2) =>
            {
                targetAngleZ = val2;
                tazField.text = val2.ToString();
                fm.ReadInt("car/cellDistance", (val3) =>
                {
                    cellDistance = val3;
                    cdField.text = val3.ToString();
                    SetButtonState(true);
                    menu.SetActive(true);
                });
            });
        });
    }

    private void Update()
    {
        if (startAction != 0)
        {
            SetButtonState(false);

            fm.SetPathAndDrive(new int[] { startAction }, () => { actionFinished = true; });

            startAction = 0;
        }

        if (actionFinished)
        {
            startAction = 0;
            fm.ReadBool("car/drive", (val) =>
            {
                if (!val)
                {
                    SetButtonState(true);
                    actionFinished = false;
                }
            });
        }
    }

    public void SetAction(int action)
    {
        actionFinished = false;
        startAction = action;
    }

    public void SubmitTaz()
    {
        SetButtonState(false);
        fm.SetInt("car/targetAngleZ", int.Parse(tazField.text), () =>
        {
            SetButtonState(true);
        });
        fm.SetBool("car/update", true);
    }
    
    public void SubmitTpc()
    {
        SetButtonState(false);
        fm.SetInt("car/ticksPerCell", int.Parse(tpcField.text), () =>
        {
            SetButtonState(true);
        });
        fm.SetBool("car/update", true);
    }

    public void SubmitCD()
    {
        SetButtonState(false);
        fm.SetInt("car/cellDistance", int.Parse(cdField.text), () =>
        {
            SetButtonState(true);
        });
        fm.SetBool("car/update", true);
    }

    private void SetButtonState(bool state)
    {
        foreach (Button b in buttons)
        {
            b.interactable = state;
        }

        tazField.interactable = state;
        tpcField.interactable = state;
    }

    public void FinishCalibration()
    {
        menu.SetActive(false);
        mcs.enabled = true;
        this.enabled = false;
    }
}
