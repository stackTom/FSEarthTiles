using System;
using System.Collections.Generic;
using System.Text;

namespace FSEarthMasksInternalDLL
{
    public interface FSEarthMasksInternalInterface
    {
        void SetStatus(String iStatus);
        void SetStatusFromFriendThread(String iStatus);
    }
}
