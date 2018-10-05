using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ASM_FORM_CLIENT.Model
{
    [Serializable]
    class BaseModel
    {
        public string name = "";

        public BaseModel()
        {
            name = "BaseModel";
        }

        public void Print()
        {
            Console.WriteLine(name);
        }

        // Convert an object to a byte array
        public byte[] ObjectToByteArray()
        {

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, this);

            return ms.ToArray();
        }

        // Convert a byte array to an Object
        public static BaseModel ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            BaseModel obj = (BaseModel)binForm.Deserialize(memStream);

            return obj;
        }
    }
}
