using System;
using System.Collections.Generic;
using System.Text;

namespace FSEarthTilesInternalDLL
{
    public interface FSEarthTilesInternalInterface
    {
        void SetStatus(String iStatus);
        void SetStatusFromFriendThread(String iStatus);
        void SetExitStatusFromFriendThread(String iStatus);
    }
}
