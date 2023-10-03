using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

/// <summary>
/// ���繤���� <see langword="static"/>
/// </summary>
public static class NetworkUtils
{
    /// <summary>
    /// obj -> bytes, ���objδ�����Ϊ [Serializable] �򷵻�null
    /// </summary>
    public static byte[] Serialize(object obj)
    {
        //���岻Ϊ���ҿɱ����л�
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
    /// bytes -> obj, ���objδ�����Ϊ [Serializable] �򷵻�null
    /// </summary>
    public static T Deserialize<T>(byte[] data) where T : class
    {
        //���ݲ�Ϊ����T�ǿ����л�������
        if (data == null || !typeof(T).IsSerializable) return null;

        BinaryFormatter formatter = new BinaryFormatter();
        using (MemoryStream stream = new MemoryStream(data))
        {
            object obj = formatter.Deserialize(stream);
            return obj as T;
        }
    }

    /// <summary>
    /// ��ȡ����IPv4,��ȡʧ���򷵻�null
    /// </summary>
    public static string GetLocalIPv4()
    {
        string hostName = Dns.GetHostName(); //�õ�������
        IPHostEntry iPEntry = Dns.GetHostEntry(hostName);
        for (int i = 0; i < iPEntry.AddressList.Length; i++)
        {
            //��IP��ַ�б���ɸѡ��IPv4���͵�IP��ַ
            if (iPEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                return iPEntry.AddressList[i].ToString();
        }
        return null;
    }

    /// <summary>
    /// �������� -> �ַ���
    /// </summary>
    public static string Byte2String(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// �ַ��� -> ��������
    /// </summary>
    public static byte[] String2Byte(string str)
    {
        return Encoding.UTF8.GetBytes(str);
    }
}