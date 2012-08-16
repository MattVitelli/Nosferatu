using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gaia.Core
{
    public class CustomList<T>
    {
        int allocatedSize;
        int logSize;

        T[] allocatedData;

        public int Count
        {
            get { return logSize; }
        }

        public CustomList()
        {
            allocatedSize = 4;
            allocatedData = new T[allocatedSize];
            logSize = 0;
        }

        public void Clear()
        {
            logSize = 0;
        }

        public T this[int index]
        {
            get
            {
                return allocatedData[index];
            }
            set
            {
                allocatedData[index] = value;
            }
        }

        public void Add(T element)
        {
            if (logSize >= allocatedSize)
            {
                allocatedSize *= 2;
                T[] tempArray = allocatedData;
                allocatedData = new T[allocatedSize];
                Array.Copy(tempArray, allocatedData, logSize);
                tempArray = null;
            }

            allocatedData[logSize] = element;
            logSize++;
        }
    }
}
