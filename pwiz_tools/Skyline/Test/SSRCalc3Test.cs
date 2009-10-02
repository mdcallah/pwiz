/*
 * Original author: Brendan MacLean <brendanx .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2009 University of Washington - Seattle, WA
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Util;

namespace pwiz.SkylineTest
{
    /// <summary>
    /// Summary description for SSRCalc3Test
    /// </summary>
    [TestClass]
// ReSharper disable InconsistentNaming
    public class SSRCalc3Test
// ReSharper restore InconsistentNaming
    {
        private readonly object[,] _peptides300A;
        private readonly object[,] _peptides100A;

        public SSRCalc3Test()
        {
            // These peptides were taken from the supporting information for the
            // Krokhin, Anal. Chem., 2006 paper

            // Sequence-Specific Retention Calculator. Algorithm for Peptide
            //    Retention Prediction in Ion-Pair RP-HPLC: Application to
            //    300- and 100-� Pore Size C18 Sorbents

            // http://tinyurl.com/Krokhin2006-AC

            // Supporting information:
            // http://tinyurl.com/Krokhin2006

            // The version currently implemented is SSRCalc v3.0, which
            // does not match v3.3 reported in the supporting information.
            // Reported values are noted in comments, where they differ.

            _peptides300A = new object[,]
            {
                {"LVEYR", 10.69},
                {"EVQPGR", 3.92},
                {"NQYLR", 10.39},
                {"HREER", 1.95},
                {"YQYQR", 5.68},
                {"NNEVQR", 3.77},
                {"NPFLFK", 27.33},
                {"EDPEER", 2.79},
                {"YETIEK", 8.39},
                {"NEQQER", 0.99},
                {"SSESQER", 1.34},
                {"EGEEEER", 2.06},
                {"EQIEELK", 14.34},
                {"NSYNLER", 11.59},
                {"QEDEEEK", 0.85},
                {"RNPFLFK", 28.86},
                {"REDPEER", 3.49},
                {"FEAFDLAK", 29.13},
                {"GNLELLGLK", 32.08},
                {"QEGEKEEK", 0.88},
                {"LFEITPEK", 24.20},
                {"VLLEEQEK", 17.10},
                {"EQIEELKK", 13.61},
                {"EKEDEEEK", 1.20},
                {"SHKPEYSNK", 6.08},
                {"LFEITPEKK", 22.79},
                {"EEDEEEGQR", 1.89},
                {"AIVVLLVNEGK", 32.71},
                {"QEDEEEKQK", 0.66},
                {"NILEASYNTR", 20.09},
                {"AILTVLSPNDR", 29.18},
                {"QQGEETDAIVK", 12.18},
                {"VLLEEQEKDR", 17.24},
                {"HGEWRPSYEK", 16.50},
                {"LVDLVIPVNGPGK", 31.14},
                {"RQQGEETDAIVK", 13.14},
                {"QSHFANAEPEQK", 11.27},
                {"SDLFENLQNYR", 30.44},
                {"SLPSEFEPINLR", 33.12},
                {"RHGEWRPSYEK", 16.40},
                {"ELTFPGSVQEINR", 28.46},
                {"KSLPSEFEPINLR", 32.53},
                {"RSDLFENLQNYR", 29.38},
                {"EEDEEQVDEEWR", 20.02},
                {"WEREEDEEQVDEEWR", 27.02},
                {"NFLSGSDDNVISQIENPVK", 34.63},
                {"LPAGTTSYLVNQDDEEDLR", 31.49},
                {"HGEWRPSYEKQEDEEEK", 17.96},
                {"HGEWRPSYEKEEDEEEGQR", 19.54},
                {"AKPHTIFLPQHIDADLILVVLSGK", 51.49},
                {"LPAGTTSYLVNQDDEEDLRLVDLVIPVNGPGK", 48.93},
                {"LSPGDVVIIPAGHPVAITASSNLNLLGFGINAENNER", 48.29},
                {"FDQR", 4.38},
                {"LLEYK", 14.65},
                {"ILENQK", 7.41},
                {"QVQNYK", 4.12},
                {"NSFNLER", 17.38},
                {"DSFNLER", 17.40},
                {"DDNEELR", 7.78},
                {"GQIEELSK", 14.38},
                {"VLLEEHEK", 16.50},
                {"FFEITPEK", 26.34},
                {"GDFELVGQR", 22.76},
                {"NENQQEQR", 0.39},
                {"GPIYSNEFGK", 21.85},
                {"AIVIVTVNEGK", 25.07},
                {"SDPQNPFIFK", 27.71},
                {"IFENLQNYR", 24.28},
                {"AILTVLKPDDR", 28.26},
                {"LPAGTIAYLVNR", 29.86},
                {"QQSQEENVIVK", 14.40},
                {"SVSSESEPFNLR", 23.84},
                {"SRGPIYSNEFGK", 21.20},
                {"EGSLLLPHYNSR", 26.13},
                {"QSHFADAQPQQR", 11.06},
                {"ELAFPGSAQEVDR", 24.71},
                {"RQQSQEENVIVK", 15.42},
                {"KSVSSESEPFNLR", 23.77},
                {"FQTLFENENGHIR", 28.50},
                {"VLLEEHEKETQHR", 16.28},
                {"NILEASFNTDYEEIEK", 35.62},
                {"KEDDEEEEQGEEEINK", 11.09},
                {"NPQLQDLDIFVNSVEIK", 42.27},
                {"ASSNLDLLGFGINAENNQR", 37.00},
                {"AILTVLKPDDRNSFNLER", 37.94},
                {"NFLAGDEDNVISQVQRPVK", 33.85},
                {"SKPHTIFLPQHTDADYILVVLSGK", 45.74},
                {"FFEITPEKNPQLQDLDIFVNSVEIK", 51.59},
                {"QVQLYR", 12.93},
                {"NPIYSNK", 9.96},
                {"DDNEDLR", 7.55},
                {"EQIEELSK", 14.50},
                {"SRNPIYSNK", 10.29},
                {"AIVIVTVTEGK", 26.18},
                {"SDQENPFIFK", 26.95},
                {"LPAGTIAYLANR", 27.05},
                {"SVSSESGPFNLR", 22.76},
                {"QEINEENVIVK", 21.36},
                {"EGSLLLPNYNSR", 26.40},
                {"QSYFANAQPLQR", 23.73},
                {"ELAFPGSSHEVDR", 22.94},
                {"RQEINEENVIVK", 22.80},
                {"FQTLYENENGHIR", 24.55},
                {"VLLEQQEQEPQHR", 19.09},
                {"NILEAAFNTNYEEIEK", 37.13},
                {"NQQLQDLDIFVNSVDIK", 41.34},
                {"LPAGTIAYLANRDDNEDLR", 33.20},
                {"NFLAGEEDNVISQVERPVK", 34.14},
                {"SKPHTLFLPQYTDADFILVVLSGK", 52.80},
                {"VLDLAIPVNKPGQLQSFLLSGTQNQPSLLSGFSK", 51.34},
                {"LSPGDVFVIPAGHPVAINASSDLNLIGFGINAENNER", 48.61},
                {"SFLPSK", 17.38},
                {"EGLTFR", 17.83},
                {"TILFLK", 30.69},
                {"NLFEGGIK", 24.01},
                {"DKPWWPK", 24.74},
                {"DENFGHLK", 15.61},
                {"FTPPHVIR", 23.05},
                {"DSSSPYGLR", 14.92},
                {"SSDFLAYGIK", 28.65},
                {"NNDPSLYHR", 14.24},
                {"QLSVVHPINK", 21.28},
                {"ENPHWTSDSK", 10.92},
                {"NDSELQHWWK", 27.18},
                {"SYLPSETPSPLVK", 28.38},
                {"EIFRTDGEQVLK", 26.50},
                {"SNLDPAEYGDHTSK", 14.78},
                {"SLTLEDVPNHGTIR", 26.63},
                {"LPLDVISTLSPLPVVK", 44.43},
                {"DPNSEKPATETYVPR", 16.41},
                {"VGPVQLPYTLLHPSSK", 33.89},
                {"FQTLIDLSVIEILSR", 56.36},
                {"YWVFTDQALPNDLIK", 40.64},
                {"KDPNSEKPATETYVPR", 15.78},
                {"LFILDYHDTFIPFLR", 53.07},
                {"VILPADEGVESTIWLLAK", 44.06},
                {"SLSDR", 4.42},
                {"ATLQR", 5.84},
                {"YRDR", 2.75},
                {"HIVDR", 8.12},
                {"FLVPAR", 20.89},
                {"SNNPFK", 9.30},
                {"FSYVAFK", 25.59},
                {"LDALEPDNR", 18.08},
                {"LSAEHGSLHK", 10.95},
                {"GEEEEEDKK", 1.31},
                {"GGLSIISPPEK", 24.34},
                {"QEEDEDEEK", 1.39},
                {"TVTSLDLPVLR", 31.92},
                {"ALTVPQNYAVAAK", 22.30},
                {"QEEEEDEDEER", 4.30},
                {"QEEDEDEEKQPR", 3.67},
                {"EQPQQNECQLER", 10.01},
                {"QEQENEGNNIFSGFK", 24.49},
                {"IESEGGLIETWNPNNK", 30.54},
                {"QEEEEDEDEERQPR", 5.81},
                {"LNIGPSSSPDIYNPEAGR", 26.82},
                {"LAGTSSVINNLPLDVVAATFNLQR", 44.90},
                {"FYLAGNHEQEFLQYQHQQGGK", 32.37},
                {"RFYLAGNHEQEFLQYQHQQGGK", 32.44},
                {"IEKEDVR", 7.69},
                {"VDEVFER", 18.12},
                {"GIIGLVAEDR", 28.64},
                {"QYDEEDKR", 3.82},
                {"EVAFDIAAEK", 27.09},
                {"SLWPFGGPFK", 35.79},
                {"FNLEEGDIMR", 28.00},
                {"GELETVLDEQK", 23.20},
                {"KSLWPFGGPFK", 35.46},
                {"KPESVLNTFSSK", 23.26},
                {"KSSISYHNINAK", 15.73},
                {"FGSLFEVGPSQEK", 29.86},
                {"NIENYGLAVLEIK", 35.30},
                {"EEFFFPYDNEER", 32.62},
                {"SPFNIFSNNPAFSNK", 32.81},
                {"KEEFFFPYDNEER", 32.72},
                {"EVAFDIAAEKVDEVFER", 44.39},
                {"ANAFLSPHHYDSEAILFNIK", 42.20},
                {"LYIAAFHMPPSSGSAPVNLEPFFESAGR", 44.37},
                {"EHEEEEEQEQEEDENPYVFEDNDFETK", 29.16},
                {"HKEHEEEEEQEQEEDENPYVFEDNDFETK", 26.50},
                {"QHEPR", 2.44},
                {"SPQDER", 1.80},
                {"RQQQQR", 1.77},
                {"IVNSEGNK", 5.04},
                {"HSQVAQIK", 10.92},
                {"LRSPQDER", 6.02},
                {"GDLYNSGAGR", 12.19},
                {"LSAEYVLLYR", 32.50},
                {"AAVSHVNQVFR", 23.14},
                {"ATPGEVLANAFGLR", 33.49},
                {"ISTVNSLTLPILR", 37.05},
                {"KEEEEEEQEQR", 4.03},
                {"HSEKEEEDEDEPR", 5.94},
                {"KEDEDEDEEEEEER", 6.39},
                {"GVLGLAVPGCPETYEEPR", 33.41},
                {"VFYLGGNPEIEFPETQQK", 37.06},
                {"VESEAGLTETWNPNHPELK", 31.39},
                {"VEDGLHIISPELQEEEEQSHSQR", 28.77},
                {"TIDPNGLHLPSYSPSPQLIFIIQGK", 45.07},
                {"GGQQQEEESEEQNEGNSVLSGFNVEFLAHSLNTK", 37.57},
                {"RGGQQQEEESEEQNEGNSVLSGFNVEFLAHSLNTK", 36.99},
                {"ALEAFK", 16.38},
                {"TFLWGR", 26.93},
                {"NEPWWPK", 25.98},
                {"LLYPHYR", 22.29},
                {"SDYVYLPR", 25.01},
                {"EEELNNLR", 15.37},
                {"GSAEFEELVK", 26.15},
                {"SSDFLTYGLK", 29.89},
                {"ELVEVGHGDKK", 14.09},
                {"DNPNWTSDKR", 11.67},
                {"HASDELYLGER", 21.11},
                {"LPTNILSQISPLPVLK", 43.30},
                {"NWVFTEQALPADLIK", 40.97},
                {"FQTLIDLSVIEILSR", 56.36},
                {"EHLEPNLEGLTVEEAIQNK", 36.57},
                {"ATFLEGIISSLPTLGAGQSAFK", 52.05},
                {"IFFANQTYLPSETPAPLVHYR", 43.17},
                {"IYDYDVYNDLGNPDSGENHARPVLGGSETYPYPR", 36.67},
                {"SQIVR", 8.97},
                {"VEGGLR", 8.67},
                {"SEFDR", 7.50},
                {"HSYPVGR", 10.87},
                {"EQSHSHSHR", -0.82},
                {"TANSLTLPVLR", 29.66},
                {"AAVSHVQQVLR", 23.22},
                {"ENIADAAGADLYNPR", 27.31},
                {"EEEEEEEEDEEKQR", 5.84},
                {"IRENIADAAGADLYNPR", 28.95},
                {"VESEAGLTETWNPNNPELK", 31.91},
                {"VFYLGGNPETEFPETQEEQQGR", 32.30},
                {"TIDPNGLHLPSFSPSPQLIFIIQGK", 48.01},
                {"GQLVVVPQNFVVAEQAGEEEGLEYVVFK", 48.85},
                {"KGQLVVVPQNFVVAEQAGEEEGLEYVVFK", 47.37},
                {"LLENQK", 8.32},
                {"QIEELSK", 12.03},
                {"NQVQSYK", 6.05},
                {"FFEITPK", 25.11},
                {"NENQQGLR", 6.30},
                {"KQIEELSK", 13.20},
                {"ILLEEHEK", 18.62},
                {"EEDDEEEEQR", 4.04},
                {"DLTFPGSAQEVDR", 24.13},
                {"QSYFANAQPQQR", 15.52},
                {"ILLEEHEKETHHR", 17.28},
                {"NFLAGEEDNVISQIQK", 32.48},
                {"LTPGDVFVIPAGHPVAVR", 37.28},
                {"EEDDEEEEQREEETK", 5.89},
                {"ASSNLNLLGFGINAENNQR", 35.42},
                {"NPQLQDLDIFVNYVEIK", 46.41},
                {"KNPQLQDLDIFVNYVEIK", 45.53},
                {"NENQQGLREEDDEEEEQR", 10.37},
                {"GDQYAR", 3.50},
                {"GDYYAR", 7.60},
                {"EVYLFK", 24.15},
                {"GKEVYLFK", 25.17},
                {"VLYGPTPVR", 23.15},
                {"TGYINAAFR", 23.93},
                {"TNEVYFFK", 28.18},
                {"TLDYWPSLR", 32.85},
                {"KTLDYWPSLR", 32.13},
                {"VLYGPTPVRDGFK", 27.02},
                {"YVLLDYAPGTSNDK", 31.20},
                {"SSQNNEAYLFINDK", 26.36},
                {"NTIFESGTDAAFASHK", 26.97},
            };

            _peptides100A = new object[,]
            {
                {"RQQQQR", -2.55},
                {"HSQVAQIK", 6.35},
                {"GDLYNSGAGR", 12.76},
                {"LSAEYVLLYR", 32.26},
                {"AAVSHVNQVFR", 19.99},
                {"ISTVNSLTLPILR", 33.49},
                {"KEEEEEEQEQR", 2.49},
                {"HSEKEEEDEDEPR", 3.22},
                {"ESHGQGEEEEELEK", 9.59},
                {"KEDEDEDEEEEEER", 5.88},
                {"VFYLGGNPEIEFPETQQK", 33.32},
                {"VESEAGLTETWNPNHPELK", 27.33},
                {"NGIYAPHWNINANSLLYVIR", 43.32},
                {"VEDGLHIISPELQEEEEQSHSQR", 23.43},
                {"TIDPNGLHLPSYSPSPQLIFIIQGK", 39.26},
                {"GGQQQEEESEEQNEGNSVLSGFNVEFLAHSLNTK", 32.81},
                {"SQIVR", 9.5},
                {"VEGGLR", 9.9},
                {"SEFDR", 9.26},
                {"HSYPVGR", 8.6},
                {"LSAEYVR", 15.63},
                {"QQQGDSHQK", -3.67},
                {"EQSHSHSHR", -4.73},
                {"AAVSHVQQVLR", 18.77},
                {"IVNFQGDAVFDNK", 26.55},
                {"EEEEEEEEDEEK", 6.01},
                {"ENIADAAGADLYNPR", 24.93},
                {"IRENIADAAGADLYNPR", 25.84},
                {"LNQCQLDNINALEPDHR", 26.53},
                {"VESEAGLTETWNPNNPELK", 28.61},
                {"VFYLGGNPETEFPETQEEQQGR", 29.57},
                {"TIDPNGLHLPSFSPSPQLIFIIQGK", 41.8},
                {"GQLVVVPQNFVVAEQAGEEEGLEYVVFK", 42.96},
                {"KGQLVVVPQNFVVAEQAGEEEGLEYVVFK", 40.99},
                {"ATLQR", 5.36},
                {"HIVDR", 5.38},
                {"FLVPAR", 22.88},
                {"SNNPFK", 10.18},
                {"FSYVAFK", 25.29},
                {"LDALEPDNR", 15.58},
                {"LSAEHGSLHK", 8.3},
                {"GGLSIISPPEK", 23.33},
                {"TVTSLDLPVLR", 30.49},
                {"ALTVPQNYAVAAK", 20.07},
                {"QEEEEDEDEER", 4.09},
                {"QEEDEDEEKQPR", 1.28},
                {"EQPQQNECQLER", 8.92},
                {"QEQENEGNNIFSGFK", 23.58},
                {"IESEGGLIETWNPNNK", 28.05},
                {"LNIGPSSSPDIYNPEAGR", 25.14},
                {"FYLAGNHEQEFLQYQHQQGGK", 27.44},
                {"RFYLAGNHEQEFLQYQHQQGGK", 27.3},
                {"TFLWGR", 28.96},
                {"DEAFGHLK", 17.37},
                {"NEPWWPK", 28.1},
                {"LLYPHYR", 22.47},
                {"SDYVYLPR", 24.15},
                {"EEELNNLR", 15.97},
                {"DNPNWTSDK", 12.33},
                {"ELVEVGHGDK", 11.58},
                {"KNEPWWPK", 25.76},
                {"GSAEFEELVK", 25.73},
                {"SSDFLTYGLK", 29.75},
                {"DNPNWTSDKR", 12.12},
                {"HASDELYLGER", 20.72},
                {"QDSELQAWWK", 30.36},
                {"LDSQIYGDHTSK", 11.54},
                {"LPTNILSQISPLPVLK", 38.98},
                {"NWVFTEQALPADLIK", 37.91},
                {"TWVQDYVSLYYTSDEK", 36.71},
                {"EHLEPNLEGLTVEEAIQNK", 32.14},
                {"ATFLEGIISSLPTLGAGQSAFK", 46.41},
                {"LVVEDYPYAVDGLEIWAIIK", 51.87},
                {"IFFANQTYLPSETPAPLVHYR", 37.68},
                {"LLEYK", 17.11},
                {"ILENQK", 6.73},
                {"QVQNYK", 2.22},
                {"NSFNLER", 17.88},
                {"DDNEELR", 7.83},
                {"GQIEELSK", 14.17},
                {"VLLEEHEK", 15.13},
                {"FFEITPEK", 26.25},
                {"GDFELVGQR", 22.01},
                {"NENQQEQR", -1.43},
                {"GPIYSNEFGK", 21.89},
                {"AIVIVTVNEGK", 22.71},
                {"SDPQNPFIFK", 27.68},
                {"IFENLQNYR", 24.01},
                {"AILTVLKPDDR", 25.17},
                {"LPAGTIAYLVNR", 29.89},
                {"QQSQEENVIVK", 13.1},
                {"SVSSESEPFNLR", 23.23},
                {"EGSLLLPHYNSR", 23.59},
                {"QSHFADAQPQQR", 8.71},
                {"ELAFPGSAQEVDR", 23.32},
                {"RQQSQEENVIVK", 11.17},
                {"KSVSSESEPFNLR", 21.21},
                {"VLLEEHEKETQHR", 11.74},
                {"EDDEEEEQGEEEINK", 12.07},
                {"NILEASFNTDYEEIEK", 33.78},
                {"KEDDEEEEQGEEEINK", 9.89},
                {"NPQLQDLDIFVNSVEIK", 38.9},
                {"ASSNLDLLGFGINAENNQR", 33.65},
                {"AILTVLKPDDRNSFNLER", 32.2},
                {"NFLAGDEDNVISQVQRPVK", 29.17},
                {"SKPHTIFLPQHTDADYILVVLSGK", 40.68},
                {"FFEITPEKNPQLQDLDIFVNSVEIK", 44.43},
                {"ATLTVLK", 19.9},
                {"QVQLYR", 14.46},
                {"NPIYSNK", 11.49},
                {"DDNEDLR", 7.77},
                {"EQIEELSK", 13.61},
                {"SRNPIYSNK", 9.26},
                {"AIVIVTVTEGK", 23.58},
                {"SDQENPFIFK", 28.08},
                {"LPAGTIAYLANR", 27.35},
                {"SVSSESGPFNLR", 22.56},
                {"QEINEENVIVK", 20.69},
                {"EGSLLLPNYNSR", 25.39},
                {"KSVSSESGPFNLR", 20.5},
                {"QSYFANAQPLQR", 22.78},
                {"ELAFPGSSHEVDR", 21.04},
                {"RQEINEENVIVK", 19.89},
                {"VLLEQQEQEPQHR", 15.87},
                {"NILEAAFNTNYEEIEK", 34.91},
                {"NQQLQDLDIFVNSVDIK", 38.32},
                {"NFLAGEEDNVISQVERPVK", 30.53},
                {"SKPHTLFLPQYTDADFILVVLSGK", 47.46},
                {"VLDLAIPVNKPGQLQSFLLSGTQNQPSLLSGFSK", 43.88},
                {"LSPGDVFVIPAGHPVAINASSDLNLIGFGINAENNER", 42.15},
                {"ETHHR", -4.42},
                {"LLENQK", 8.13},
                {"SEPFNLK", 19.35},
                {"QIEELSK", 13.22},
                {"FFEITPK", 25.23},
                {"NENQQGLR", 5.46},
                {"KQIEELSK", 11.4},
                {"ILLEEHEK", 17.25},
                {"EEDDEEEEQR", 4.41},
                {"DLTFPGSAQEVDR", 23.14},
                {"QSYFANAQPQQR", 14.92},
                {"LTPGDVFVIPAGHPVAVR", 31.71},
                {"EEDDEEEEQREEETK", 4.85},
                {"ASSNLNLLGFGINAENNQR", 31.94},
                {"NPQLQDLDIFVNYVEIK", 42.65},
                {"KNPQLQDLDIFVNYVEIK", 40.57},
                {"NENQQGLREEDDEEEEQR", 10.25},
                {"IILGPK", 18.9},
                {"GDQYAR", -0.19},
                {"GDYYAR", 9.52},
                {"EVYLFK", 25.33},
                {"GKEVYLFK", 24.4},
                {"VLYGPTPVR", 22.52},
                {"TGYINAAFR", 24.12},
                {"TNEVYFFK", 27.86},
                {"TLDYWPSLR", 31.44},
                {"KTLDYWPSLR", 30.38},
                {"VLYGPTPVRDGFK", 23.96},
                {"SSQNNEAYLFINDK", 25.29},
                {"NTIFESGTDAAFASHK", 25.98},
                {"IADMFPFFEGTVFENGIDAAYR", 47.62},
                {"VLILNK", 22.76},
                {"KEEHGR", -3.79},
                {"VDEVFER", 17.81},
                {"GIIGLVAEDR", 27.37},
                {"QYDEEDKR", 3.26},
                {"EVAFDIAAEK", 26.74},
                {"SLWPFGGPFK", 36.06},
                {"SSISYHNINAK", 14.84},
                {"GELETVLDEQK", 22.78},
                {"KSLWPFGGPFK", 34.45},
                {"KPESVLNTFSSK", 21.42},
                {"KSSISYHNINAK", 11.26},
                {"FGSLFEVGPSQEK", 28.16},
                {"NIENYGLAVLEIK", 32.61},
                {"GSMSTIHYNTNANK", 12.23},
                {"SPFNIFSNNPAFSNK", 30.45},
                {"KEEFFFPYDNEER", 29.96},
                {"ANAFLSPHHYDSEAILFNIK", 36.16},
                {"LYIAAFHMPPSSGSAPVNLEPFFESAGR", 39.74},
                {"EHEEEEEQEQEEDENPYVFEDNDFETK", 26.13},
                {"HKEHEEEEEQEQEEDENPYVFEDNDFETK", 22.66},
                {"TILFLK", 30.99},
                {"IFFANK", 22.82},
                {"NLFEGGIK", 24.19},
                {"DKPWWPK", 25.6},
                {"DENFGHLK", 15.59},
                {"FTPPHVIR", 21.57},
                {"SSDFLAYGIK", 28.05},
                {"NNDPSLYHR", 13.01},
                {"QLSVVHPINK", 17.72},
                {"ENPHWTSDSK", 10.11},
                {"HASDEVYLGQR", 15.61},
                {"NYMQVEFFLK", 38.46},
                {"NDSELQHWWK", 26.63},
                {"SYLPSETPSPLVK", 26.57},
                {"SNLDPAEYGDHTSK", 12.94},
                {"SLTLEDVPNHGTIR", 23.22},
                {"LPLDVISTLSPLPVVK", 40.29},
                {"DPNSEKPATETYVPR", 13.18},
                {"VGPVQLPYTLLHPSSK", 29.48},
                {"FQTLIDLSVIEILSR", 51.66},
                {"YWVFTDQALPNDLIK", 37.43},
                {"KDPNSEKPATETYVPR", 12.19},
                {"LFILDYHDTFIPFLR", 48.39},
                {"VILPADEGVESTIWLLAK", 40.32},
                {"TWVQDYVSLYYATDNDIK", 40.18},
                {"LAGTSSVINNLPLDVVAATFNLQR", 40.49},
                {"SSNNQLDQMPR", 13.2},
                {"LLIEDYPYAVDGLEIWTAIK", 54.34},
                {"ATPAEVLANAFGLR", 37.02},
                {"ATPGEVLANAFGLR", 35.41},
                {"LVEYR", 12.69},
                {"NQYLR", 11.9},
                {"HREER", -2.16},
                {"YQYQR", 5.66},
                {"NNEVQR", 1.08},
                {"NPFLFK", 29.36},
                {"YETIEK", 10.32},
                {"NEQQER", -4.36},
                {"SSESQER", -1.98},
                {"EGEEEER", -0.24},
                {"EQIEELK", 14.74},
                {"NSYNLER", 12.72},
                {"QEDEEEK", -2.5},
                {"RNPFLFK", 28.52},
                {"REDPEER", 2.03},
                {"FEAFDLAK", 29.57},
                {"GNLELLGLK", 30.76},
                {"QEGEKEEK", -2.46},
                {"LFEITPEK", 24.09},
                {"VLLEEQEK", 16.15},
                {"EQIEELKK", 11.2},
                {"SHKPEYSNK", 2.76},
                {"AIVVLLVNEGK", 30.03},
                {"NILEASYNTR", 20.86},
                {"AILTVLSPNDR", 26.44},
                {"QQGEETDAIVK", 11.61},
                {"VLLEEQEKDR", 14.7},
                {"HGEWRPSYEK", 14.44},
                {"RQQGEETDAIVK", 9.15},
                {"QSHFANAEPEQK", 9.11},
                {"SDLFENLQNYR", 30.51},
                {"SLPSEFEPINLR", 32.11},
                {"RHGEWRPSYEK", 12.74},
                {"ELTFPGSVQEINR", 26.78},
                {"KSLPSEFEPINLR", 29.68},
                {"RSDLFENLQNYR", 26.91},
                {"EEDEEQVDEEWR", 20.02},
                {"WEREEDEEQVDEEWR", 25.13},
                {"NFLSGSDDNVISQIENPVK", 31.47},
                {"LPAGTTSYLVNQDDEEDLR", 28.66},
                {"HGEWRPSYEKEEDEEEGQR", 16.23},
                {"AKPHTIFLPQHIDADLILVVLSGK", 44.85},
                {"LSPGDVVIIPAGHPVAITASSNLNLLGFGINAENNER", 41.78},
            };
        }

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        /// <summary>
        ///A test for ScoreSequence with 300A column
        ///</summary>
        [TestMethod]
        public void ScoreSequence_300A_Test()
        {
            SSRCalc3 calc = new SSRCalc3(SSRCalc3.Column.A300);

            for (int i = 0; i < _peptides300A.GetLength(0); i++ )
            {
                string peptide = (string) _peptides300A[i, 0];
                double expected = (double) _peptides300A[i, 1];
                double actual = calc.ScoreSequence(peptide);

                // Round the returned value to match the values presented
                // in the supporting information of the SSRCalc 3 publication.
                // First cast to float, since the added precision of the double
                // caused the double representation of 12.805 to round to 12.80
                // instead of 12.81.  When diffed with 12.81 the result was
                // 0.005000000000002558.
                double actualRound = Math.Round((float) actual, 2);

                // Extra conditional added to improve debugging of issues.
                if (Math.Abs(expected - actual) > 0.005)
                    Assert.AreEqual(expected, actualRound, "Peptide {0}", peptide);
            }
        }

        /// <summary>
        ///A test for ScoreSequence with 100A column
        ///</summary>
        // Problems with the results from the article
        // [TestMethod]
        public void ScoreSequence_100A_Test()
        {
            SSRCalc3 calc = new SSRCalc3(SSRCalc3.Column.A100);

            for (int i = 0; i < _peptides100A.GetLength(0); i++)
            {
                string peptide = (string)_peptides100A[i, 0];
                double expected = (double)_peptides100A[i, 1];
                double actual = calc.ScoreSequence(peptide);

                // Round the returned value to match the values presented
                // in the supporting information of the SSRCalc 3 publication.
                // First cast to float, since the added precision of the double
                // caused the double representation of 12.805 to round to 12.80
                // instead of 12.81.  When diffed with 12.81 the result was
                // 0.005000000000002558.
                double actualRound = Math.Round((float)actual, 2);

                // Extra conditional added to improve debugging of issues.
                if (Math.Abs(expected - actual) > 0.005)
                    Assert.AreEqual(expected, actualRound, "Peptide {0}", peptide);
            }
        }
    }
}
