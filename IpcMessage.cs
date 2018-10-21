/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */
 
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPipes
{
    public class IpcMessage
    {
        public string Service { get; set; }
        public string Method { get; set; }
        public object[] Parameters { get; set; }

        public object Return { get; set; }
        public string Error { get; set; }
    }
}
