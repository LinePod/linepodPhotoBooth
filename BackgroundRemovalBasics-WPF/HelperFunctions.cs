using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hpi.Hci.Bachelorproject1617.PhotoBooth
{
    class HelperFunctions
    {

        public static byte[] IntToByteArray(int input)
        {
            byte[] intBytes = BitConverter.GetBytes(input);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(intBytes);
            byte[] result = intBytes;
            return result;
        }
    }
}
