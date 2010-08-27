//
// $Id$
//
//
// Original author: Darren Kessner <darren@proteowizard.org>
//
// Copyright 2008 Spielberg Family Center for Applied Proteomics
//   Cedars-Sinai Medical Center, Los Angeles, California  90048
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

#include "FullReaderList.hpp"
#include "pwiz_aux/msrc/data/vendor_readers/Waters/Reader_Waters.hpp"
#include "pwiz_aux/msrc/data/vendor_readers/Bruker/Reader_Bruker.hpp"
#include "pwiz_aux/msrc/data/vendor_readers/ABI/Reader_ABI.hpp"
#include "pwiz_aux/msrc/data/vendor_readers/ABI/T2D/Reader_ABI_T2D.hpp"


namespace pwiz {
namespace msdata {


PWIZ_API_DECL FullReaderList::FullReaderList()
{
    push_back(ReaderPtr(new Reader_Waters)); 
    push_back(ReaderPtr(new Reader_Bruker));
    push_back(ReaderPtr(new Reader_ABI));
    push_back(ReaderPtr(new Reader_ABI_T2D));
}


} // namespace msdata
} // namespace pwiz

