using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

/// <summary>
/// network tools <see langword="static"/>
/// </summary>
public static class NetworkUtils
{
    /// <summary>
    /// obj -> bytes, if object doesn't mark as [Serializable] then return null
    /// </summary>
    public static byte[] Serialize(object obj)
    {
        // obj can't be null
        if (obj == null || !obj.GetType().IsSerializable) return null;

        BinaryFormatter formatter = new BinaryFormatter();
        using (MemoryStream stream = new MemoryStream())
        {
            formatter.Serialize(stream, obj);
            byte[] data = stream.ToArray();
            return data;
        }
    }

    /// <summary>
    /// bytes -> obj, if object doesn't mark as [Serializable] then return null
    /// </summary>
    public static T Deserialize<T>(byte[] data) where T : class
    {
        // obj can't be null & T must be serializable
        if (data == null || !typeof(T).IsSerializable) return null;

        BinaryFormatter formatter = new BinaryFormatter();
        using (MemoryStream stream = new MemoryStream(data))
        {
            object obj = formatter.Deserialize(stream);
            return obj as T;
        }
    }

    public static string GetLocalIPv4()
    {
        string hostName = Dns.GetHostName(); // get host name

        IPHostEntry iPEntry = Dns.GetHostEntry(hostName);
        
        for (int i = 0; i < iPEntry.AddressList.Length; i++)
        {
            if (iPEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
            {
                return iPEntry.AddressList[i].ToString();
            }
        }
        return null;
    }
}