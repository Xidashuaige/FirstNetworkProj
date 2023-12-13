using TMPro;
using UnityEngine;

public class MessageController : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text messageLabel;

    public void SetMessage(string owner, string message)
    {
        nameLabel.text = owner;
        messageLabel.text = message;
    }
}
