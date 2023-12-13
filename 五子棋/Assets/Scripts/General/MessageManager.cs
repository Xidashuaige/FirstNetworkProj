using UnityEngine;

public class MessageManager : MonoBehaviour
{
    [SerializeField] private GameObject _messagePrefab;

    public void AddMessage(string owner, string message)
    {
        var _ = Instantiate(_messagePrefab, transform);

        _.GetComponent<MessageController>().SetMessage(owner, message);
    }
}
