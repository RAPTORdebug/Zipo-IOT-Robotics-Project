using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NotificationSystem : MonoBehaviour
{
    public GameObject notificationPanel;
    public Transform canvas;
    
    public void SendNotification(string text)
    {
        GameObject np = Instantiate(notificationPanel, canvas);
        np.transform.GetChild(0).GetComponentInChildren<Text>().text = text;
        StartCoroutine(RemoveSelf(np));
    }

    private IEnumerator RemoveSelf(GameObject go)
    {
        yield return new WaitForSeconds(5f);
        go.GetComponent<Animator>().SetBool("Exit", true);
        yield return new WaitForSeconds(2f);
        Destroy(go);
    }
}
