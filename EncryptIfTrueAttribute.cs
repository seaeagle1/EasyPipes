/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

 using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPipes
{
    /// <summary>
    /// Attribute signals that a tcp-connection should be encrypted after this interface method is called and
    /// return a true value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class EncryptIfTrueAttribute : Attribute
    {
    }
}
