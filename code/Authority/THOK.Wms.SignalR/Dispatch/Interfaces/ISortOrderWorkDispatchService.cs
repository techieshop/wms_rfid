﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace THOK.Wms.SignalR.Dispatch.Interfaces
{
    public interface ISortOrderWorkDispatchService
    {
        void Dispatch(string workDispatchId);
    }
}