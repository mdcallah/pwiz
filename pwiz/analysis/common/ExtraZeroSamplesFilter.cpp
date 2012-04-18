//
// $Id: ZeroSamplesFilter.cpp  $
//
//
//
// Original author: Brian Pratt <brian.pratt <a.t> insilicos.com>
//
// Copyright 2012  Spielberg Family Center for Applied Proteomics
//   University of Southern California, Los Angeles, California  90033
//
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
//

#define PWIZ_SOURCE


#include "ExtraZeroSamplesFilter.hpp"
#include "pwiz/utility/misc/Container.hpp"
#include <cmath>


namespace pwiz {
namespace analysis {

/// removes zero samples in signal profiles, except those flanking nonzero samples
/// simply looks for runs of 0 values, removes all but start and end of run


void ExtraZeroSamplesFilter::remove_zeros(const vector<double>& x, const vector<double>& y,
                            vector<double>& xProcessed, vector<double>& yProcessed, bool bPreserveFlamkingZeros)
{
    xProcessed.resize(0);
    yProcessed.resize(0);
    if (x.size() > 3) {
        xProcessed.reserve(x.size());
        yProcessed.reserve(y.size());
        // leave flanking zeros around non-zero data points
        int i;
        for (i=0; i < (int)y.size()-1;i++)
        {
            if (y[i] || y[i+1] || (i && y[i-1])) {
                xProcessed.push_back(x[i]);
                yProcessed.push_back(y[i]);
            }
        }
        if (y[i] || y[i-1]) {
            xProcessed.push_back(x[i]);
            yProcessed.push_back(y[i]);
        }
        xProcessed.resize(xProcessed.size()); // offer to trim excess capacity
        yProcessed.resize(yProcessed.size()); // offer to trim excess capacity
    }
    else
    {
        xProcessed = x;
        yProcessed = y;
    }
}


} // namespace analysis
} // namespace pwiz
