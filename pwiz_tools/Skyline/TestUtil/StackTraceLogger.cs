﻿/*
 * Original author: Brendan MacLean <brendanx .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2017 University of Washington - Seattle, WA
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Diagnostics;

namespace pwiz.SkylineTestUtil
{
    public abstract class StackTraceLogger
    {
        private readonly string _filterText;

        protected StackTraceLogger(string filterText)
        {
            _filterText = filterText;
        }

        protected void LogStack(Func<string> logMessage)
        {
            var stackTrace = new StackTrace(1, true).ToString();
            if (!stackTrace.Contains(_filterText))
            {
                Console.WriteLine(logMessage());
                Console.WriteLine(stackTrace);
            }
        }
    }
}
