﻿/*
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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using pwiz.Crawdad;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Model.Results
{
    public struct ChromGroupHeaderInfo : IComparable<ChromGroupHeaderInfo>
    {
        public ChromGroupHeaderInfo(float precursor, int fileIndex, int numTransitions, int startTransitionIndex,
                int numPeaks, int startPeakIndex, int maxPeakIndex,
                int numPoints, int compressedSize, long location)
            : this()
        {
            Precursor = precursor;
            FileIndex = fileIndex;
            NumTransitions = numTransitions;
            StartTransitionIndex = startTransitionIndex;
            NumPeaks = numPeaks;
            StartPeakIndex = startPeakIndex;
            MaxPeakIndex = maxPeakIndex;
            NumPoints = numPoints;
            CompressedSize = compressedSize;
            Align = 0;
            LocationPoints = location;
        }

        public float Precursor { get; set; }
        public int FileIndex { get; private set; }
        public int NumTransitions { get; private set; }
        public int StartTransitionIndex { get; private set; }
        public int NumPeaks { get; private set; }
        public int StartPeakIndex { get; private set; }
        public int MaxPeakIndex { get; private set; }
        public int NumPoints { get; private set; }
        public int CompressedSize { get; private set; }
        public int Align { get; private set; }  // Need even number of 4-byte values
        public long LocationPoints { get; private set; }

        public void Offset(int offsetFiles, int offsetTransitions, int offsetPeaks, long offsetPoints)
        {
            FileIndex += offsetFiles;
            StartTransitionIndex += offsetTransitions;
            StartPeakIndex += offsetPeaks;
            LocationPoints += offsetPoints;
        }

        public int CompareTo(ChromGroupHeaderInfo info)
        {
            // Sort by key, and then file index.
            int keyCompare = Precursor.CompareTo(info.Precursor);
            if (keyCompare != 0)
                return keyCompare;
            return FileIndex - info.FileIndex;
        }

        #region Fast file I/O

        /// <summary>
        /// A 2x slower version of ReadArray than <see cref="ReadArray(SafeHandle,int)"/>
        /// that does not require a file handle.  This one is covered in Randy Kern's blog,
        /// but is originally from Eric Gunnerson:
        /// <para>
        /// http://blogs.msdn.com/ericgu/archive/2004/04/13/112297.aspx
        /// </para>
        /// </summary>
        /// <param name="stream">Stream to from which to read the elements</param>
        /// <param name="count">Number of elements to read</param>
        /// <returns>New array of elements</returns>
        public static unsafe ChromGroupHeaderInfo[] ReadArray(Stream stream, int count)
        {
            // Use fast version, if this is a file
            if (stream is FileStream)
                return ReadArray(((FileStream)stream).SafeFileHandle, count);

            ChromGroupHeaderInfo[] results = new ChromGroupHeaderInfo[count];
            int size = sizeof(ChromGroupHeaderInfo);
            byte[] buffer = new byte[size];
            for (int i = 0; i < count; ++i)
            {
                if (stream.Read(buffer, 0, size) != size)
                    throw new InvalidDataException();

                fixed (byte* pBuffer = buffer)
                {
                    results[i] = *(ChromGroupHeaderInfo*)pBuffer;
                }
            }

            return results;
        }

        /// <summary>
        /// Direct read of an entire array throw p-invoke of Win32 WriteFile.  This seems
        /// to coexist with FileStream reading that the write version, but its use case
        /// is tightly limited.
        /// <para>
        /// Contributed by Randy Kern.  See:
        /// http://randy.teamkern.net/2009/02/reading-arrays-from-files-in-c-without-extra-copy.html
        /// </para>
        /// </summary>
        /// <param name="file">File handler returned from <see cref="FileStream.SafeFileHandle"/></param>
        /// <param name="count">Number of elements to read</param>
        /// <returns>New array of elements</returns>
        public static unsafe ChromGroupHeaderInfo[] ReadArray(SafeHandle file, int count)
        {
            ChromGroupHeaderInfo[] results = new ChromGroupHeaderInfo[count];
            fixed (ChromGroupHeaderInfo* p = &results[0])
            {
                byte* p2 = (byte*)p;

                UInt32 cbReadDesired = (UInt32)(sizeof(ChromGroupHeaderInfo) * count);
                UInt32 cbReadActual = 0;
                bool ret = Kernel32.ReadFile(file, p2, cbReadDesired, &cbReadActual, null);
                if (!ret || cbReadActual != cbReadDesired)
                    throw new InvalidDataException();
            }

            return results;
        }

        /// <summary>
        /// Direct write of an entire array throw p-invoke of Win32 WriteFile.  This cannot
        /// be mixed with standard writes to a FileStream, or .NET throws an exception
        /// about the file location not being what it expected.
        /// </summary>
        /// <param name="file">File handler returned from <see cref="FileStream.SafeFileHandle"/></param>
        /// <param name="groupHeaders">The array to write</param>
        public static unsafe void WriteArray(SafeHandle file, ChromGroupHeaderInfo[] groupHeaders)
        {
            fixed (ChromGroupHeaderInfo* p = &groupHeaders[0])
            {
                byte* p2 = (byte*)p;

                UInt32 cbWriteDesired = (UInt32)(sizeof(ChromGroupHeaderInfo) * groupHeaders.Length);
                UInt32 cbWriteActual = 0;
                bool ret = Kernel32.WriteFile(file, p2, cbWriteDesired, &cbWriteActual, null);
                if (!ret || cbWriteActual != cbWriteDesired)
                    throw new IOException();
            }
        }

        #endregion
    }

    public struct ChromTransition
    {
        public ChromTransition(float product) : this()
        {
            Product = product;
        }

        public float Product { get; private set; }

        #region Fast file I/O

        /// <summary>
        /// A 2x slower version of ReadArray than <see cref="ReadArray(SafeHandle,int)"/>
        /// that does not require a file handle.  This one is covered in Randy Kern's blog,
        /// but is originally from Eric Gunnerson:
        /// <para>
        /// http://blogs.msdn.com/ericgu/archive/2004/04/13/112297.aspx
        /// </para>
        /// </summary>
        /// <param name="stream">Stream to from which to read the elements</param>
        /// <param name="count">Number of elements to read</param>
        /// <returns>New array of elements</returns>
        public static unsafe ChromTransition[] ReadArray(Stream stream, int count)
        {
            // Use fast version, if this is a file
            if (stream is FileStream)
                return ReadArray(((FileStream) stream).SafeFileHandle, count);

            // CONSIDER: Probably faster in this case to read the entire block,
            //           and convert from bytes to single float values.
            ChromTransition[] results = new ChromTransition[count];
            int size = sizeof (ChromTransition);
            byte[] buffer = new byte[size];
            for (int i = 0; i < count; ++i)
            {
                if (stream.Read(buffer, 0, size) != size)
                    throw new InvalidDataException();

                fixed (byte* pBuffer = buffer)
                {
                    results[i] = *(ChromTransition*) pBuffer;
                }
            }

            return results;
        }

        /// <summary>
        /// Direct read of an entire array throw p-invoke of Win32 WriteFile.  This seems
        /// to coexist with FileStream reading that the write version, but its use case
        /// is tightly limited.
        /// <para>
        /// Contributed by Randy Kern.  See:
        /// http://randy.teamkern.net/2009/02/reading-arrays-from-files-in-c-without-extra-copy.html
        /// </para>
        /// </summary>
        /// <param name="file">File handler returned from <see cref="FileStream.SafeFileHandle"/></param>
        /// <param name="count">Number of elements to read</param>
        /// <returns>New array of elements</returns>
        public static unsafe ChromTransition[] ReadArray(SafeHandle file, int count)
        {
            ChromTransition[] results = new ChromTransition[count];
            fixed (ChromTransition* p = &results[0])
            {
                byte* p2 = (byte*) p;

                UInt32 cbReadDesired = (UInt32) (sizeof (ChromTransition)*count);
                UInt32 cbReadActual = 0;
                bool ret = Kernel32.ReadFile(file, p2, cbReadDesired, &cbReadActual, null);
                if (!ret || cbReadActual != cbReadDesired)
                    throw new InvalidDataException();
            }

            return results;
        }

        /// <summary>
        /// Direct write of an entire array throw p-invoke of Win32 WriteFile.  This cannot
        /// be mixed with standard writes to a FileStream, or .NET throws an exception
        /// about the file location not being what it expected.
        /// </summary>
        /// <param name="file">File handler returned from <see cref="FileStream.SafeFileHandle"/></param>
        /// <param name="setHeaders">The array to write</param>
        public static unsafe void WriteArray(SafeHandle file, ChromTransition[] setHeaders)
        {
            fixed (ChromTransition* p = &setHeaders[0])
            {
                byte* p2 = (byte*) p;

                UInt32 cbWriteDesired = (UInt32) (sizeof (ChromTransition)*setHeaders.Length);
                UInt32 cbWriteActual = 0;
                bool ret = Kernel32.WriteFile(file, p2, cbWriteDesired, &cbWriteActual, null);
                if (!ret || cbWriteActual != cbWriteDesired)
                    throw new IOException();
            }
        }

        #endregion

        #region object overrides

        public override string ToString()
        {
            return Product.ToString();
        }

        #endregion
    }

    public struct ChromPeak
    {
        [Flags]
        public enum FlagValues
        {
            degenerate_fwhm = 0x1
        }

// ReSharper disable InconsistentNaming
        public static ChromPeak EMPTY;
// ReSharper restore InconsistentNaming

        public ChromPeak(CrawdadPeak peak, float[] times)
            : this(times[peak.TimeIndex],
                    times[peak.StartIndex],
                    times[peak.EndIndex],
                    peak.Area,
                    peak.BackgroundArea,
                    peak.Height,
                    peak.Fwhm * (times[peak.StartIndex + 1] - times[peak.StartIndex]),
                    peak.FwhmDegenerate)
        {
        }

        public ChromPeak(float retentionTime, float startTime, float endTime,
            float area, float backgroundArea, float height, float fwhm, bool fwhmDegenerate)
            : this()
        {
            RetentionTime = retentionTime;
            StartTime = startTime;
            EndTime = endTime;
            Area = area;
            BackgroundArea = backgroundArea;
            Height = height;
            Fwhm = fwhm;
            Flags = 0;
            if (fwhmDegenerate)
                Flags |= FlagValues.degenerate_fwhm;
        }

        public float RetentionTime { get; private set; }
        public float StartTime { get; private set; }
        public float EndTime { get; private set; }
        public float Area { get; private set; }
        public float BackgroundArea { get; private set; }
        public float Height { get; private set; }
        public float Fwhm { get; private set; }
        public FlagValues Flags { get; private set; }

        public bool IsEmpty { get { return EndTime == 0; } }

        public bool ContainsTime(float retentionTime)
        {
            return StartTime <= retentionTime && retentionTime <= EndTime;
        }

        public bool IsFwhmDegenerate
        {
            get { return (Flags & FlagValues.degenerate_fwhm) != 0; }
        }

        public static float Intersect(ChromPeak peak1, ChromPeak peak2)
        {
            return Intersect(peak1.StartTime, peak1.EndTime, peak2.StartTime, peak2.EndTime);
        }

        public static float Intersect(float startTime1, float endTime1, float startTime2, float endTime2)
        {
            return Math.Min(endTime1, endTime2) - Math.Max(startTime1, startTime2);
        }

        #region Fast file I/O

        /// <summary>
        /// A 2x slower version of ReadArray than <see cref="ReadArray(SafeHandle,int)"/>
        /// that does not require a file handle.  This one is covered in Randy Kern's blog,
        /// but is originally from Eric Gunnerson:
        /// <para>
        /// http://blogs.msdn.com/ericgu/archive/2004/04/13/112297.aspx
        /// </para>
        /// </summary>
        /// <param name="stream">Stream to from which to read the elements</param>
        /// <param name="count">Number of elements to read</param>
        /// <returns>New array of elements</returns>
        public static unsafe ChromPeak[] ReadArray(Stream stream, int count)
        {
            // Use fast version, if this is a file
            if (stream is FileStream)
                return ReadArray(((FileStream)stream).SafeFileHandle, count);

            ChromPeak[] results = new ChromPeak[count];
            int size = sizeof(ChromPeak);
            byte[] buffer = new byte[size];
            for (int i = 0; i < count; ++i)
            {
                if (stream.Read(buffer, 0, size) != size)
                    throw new InvalidDataException();

                fixed (byte* pBuffer = buffer)
                {
                    results[i] = *(ChromPeak*)pBuffer;
                }
            }

            return results;
        }

        /// <summary>
        /// Direct read of an entire array throw p-invoke of Win32 WriteFile.  This seems
        /// to coexist with FileStream reading that the write version, but its use case
        /// is tightly limited.
        /// <para>
        /// Contributed by Randy Kern.  See:
        /// http://randy.teamkern.net/2009/02/reading-arrays-from-files-in-c-without-extra-copy.html
        /// </para>
        /// </summary>
        /// <param name="file">File handler returned from <see cref="FileStream.SafeFileHandle"/></param>
        /// <param name="count">Number of elements to read</param>
        /// <returns>New array of elements</returns>
        public static unsafe ChromPeak[] ReadArray(SafeHandle file, int count)
        {
            ChromPeak[] results = new ChromPeak[count];
            if (count > 0)
            {
                fixed (ChromPeak* p = &results[0])
                {
                    byte* p2 = (byte*) p;

                    UInt32 cbReadDesired = (UInt32) (sizeof (ChromPeak)*count);
                    UInt32 cbReadActual = 0;
                    bool ret = Kernel32.ReadFile(file, p2, cbReadDesired, &cbReadActual, null);
                    if (!ret || cbReadActual != cbReadDesired)
                        throw new InvalidDataException();
                }
            }

            return results;
        }

        /// <summary>
        /// Direct write of an entire array throw p-invoke of Win32 WriteFile.  This cannot
        /// be mixed with standard writes to a FileStream, or .NET throws an exception
        /// about the file location not being what it expected.
        /// </summary>
        /// <param name="file">File handler returned from <see cref="FileStream.SafeFileHandle"/></param>
        /// <param name="headers">The array to write</param>
        public static unsafe void WriteArray(SafeHandle file, ChromPeak[] headers)
        {
            fixed (ChromPeak* p = &headers[0])
            {
                byte* p2 = (byte*)p;

                UInt32 cbWriteDesired = (UInt32)(sizeof(ChromPeak) * headers.Length);
                UInt32 cbWriteActual = 0;
                bool ret = Kernel32.WriteFile(file, p2, cbWriteDesired, &cbWriteActual, null);
                if (!ret || cbWriteActual != cbWriteDesired)
                    throw new IOException();
            }
        }

        #endregion
    }

    public struct ChromCachedFile
    {
        private static DateTime GetLastWriteTime(string filePath)
        {
            return File.GetLastWriteTime(SampleHelp.GetPathFilePart(filePath));
        }

        public ChromCachedFile(string filePath)
            : this(filePath, GetLastWriteTime(filePath))
        {
        }

        public ChromCachedFile(string filePath, DateTime fileWriteTime)
            : this()
        {
            FilePath = filePath;
            FileWriteTime = fileWriteTime;
        }

        public string FilePath { get; private set; }
        public DateTime FileWriteTime { get; private set; }

        public bool IsCurrent
        {
            get { return Equals(FileWriteTime, GetLastWriteTime(FilePath)); }
        }
    }

    public struct ChromKey : IComparable<ChromKey>
    {
        public ChromKey(double precursor, double product)
            : this((float)precursor, (float)product)
        {
        }

        public ChromKey(float precursor, float product)
            : this()
        {
            Precursor = precursor;
            Product = product;
        }

        public float Precursor { get; private set; }
        public float Product { get; private set; }

        public override string ToString()
        {
            return string.Format("{0:F04}, {1:F04}", Precursor, Product);
        }

        public int CompareTo(ChromKey key)
        {
            int c = Comparer.Default.Compare(Precursor, key.Precursor);
            if (c != 0)
                return c;
            return Comparer.Default.Compare(Product, key.Product);
        }

        public int CompareTolerant(ChromKey key, float tolerance)
        {
            int c = CompareTolerant(Precursor, key.Precursor, tolerance);
            if (c != 0)
                return c;
            return CompareTolerant(Product, key.Product, tolerance);
        }

        public static int CompareTolerant(float f1, float f2, float tolerance)
        {
            if (Math.Abs(f1 - f2) < tolerance)
                return 0;
            return (f1 > f2 ? 1 : -1);
        }

        private const string PREFIX_TOTAL = "SRM TIC ";
        private const string PREFIX_SINGLE = "SRM SIC ";

        private static readonly Regex REGEX_ABI = new Regex(@"Q1=([^ ]+) Q3=([^ ]+) ");

        public static bool IsKeyId(string id)
        {
            return id.StartsWith(PREFIX_SINGLE); // || id.StartsWith(PREFIX_TOTAL); Skip the TICs, since Skyline calculates these
        }

        public static ChromKey FromId(string id)
        {
            try
            {
                float precursor, product;
                if (id.StartsWith(PREFIX_TOTAL))
                {
                    precursor = (float)double.Parse(id.Substring(PREFIX_TOTAL.Length), CultureInfo.InvariantCulture);
                    product = 0;
                }
                else if (id.StartsWith(PREFIX_SINGLE))
                {
                    // Remove the prefix
                    string mzPart = id.Substring(PREFIX_SINGLE.Length);

                    // Check of ABI id format match
                    string[] mzs;
                    Match match = REGEX_ABI.Match(mzPart);
                    if (match.Success)
                    {
                        mzs = new[] {match.Groups[1].Value, match.Groups[2].Value};
                    }
                    // Try simpler comma separated format (Thermo)
                    else
                    {
                        mzs = mzPart.Split(new[] { ',' });
                        if (mzs.Length != 2)
                            throw new InvalidDataException(
                                string.Format(
                                    "Invalid chromatogram ID {0} found.  The ID must include both precursor and product m/z values.",
                                    id));                        
                    }

                    precursor = (float)double.Parse(mzs[0], CultureInfo.InvariantCulture);
                    product = (float)double.Parse(mzs[1], CultureInfo.InvariantCulture);
                }
                else
                {
                    throw new ArgumentException(string.Format("The value '{0}' is not a valid chromatogram ID.", id));
                }
                return new ChromKey(precursor, product);
            }
            catch (FormatException)
            {
                throw new InvalidDataException(
                    string.Format("Invalid chromatogram ID {0} found.  Failure parsing m/z values.", id));
            }
        }
    }

    public class ChromatogramGroupInfo
    {
        protected readonly ChromGroupHeaderInfo _groupHeaderInfo;
        protected readonly IList<ChromCachedFile> _allFiles;
        protected readonly ChromTransition[] _allTransitions;
        protected readonly ChromPeak[] _allPeaks;

        public ChromatogramGroupInfo(ChromGroupHeaderInfo groupHeaderInfo,
            IList<ChromCachedFile> allFiles, ChromTransition[] allTransitions, ChromPeak[] allPeaks)
        {
            _groupHeaderInfo = groupHeaderInfo;
            _allFiles = allFiles;
            _allTransitions = allTransitions;
            _allPeaks = allPeaks;
        }

        public double PrecursorMz { get { return _groupHeaderInfo.Precursor; } }
        public string FilePath { get { return _allFiles[_groupHeaderInfo.FileIndex].FilePath; } }
        public int NumTransitions { get { return _groupHeaderInfo.NumTransitions; } }
        public int NumPeaks { get { return _groupHeaderInfo.NumPeaks; } }
        public int MaxPeakIndex { get { return _groupHeaderInfo.MaxPeakIndex; } }
        public int BestPeakIndex { get { return MaxPeakIndex; } }

        public float[] Times { get; set; }
        public float[][] IntensityArray { get; set; }

        public IEnumerable<ChromatogramInfo> TransitionPointSets
        {
            get
            {
                for (int i = 0; i < _groupHeaderInfo.NumTransitions; i++)
                {
                    yield return new ChromatogramInfo(_groupHeaderInfo, i,
                        _allFiles, _allTransitions, _allPeaks, Times, IntensityArray);
                }
            }
        }

        public ChromatogramInfo GetTransitionInfo(float productMz, float tolerance)
        {
            int startTran = _groupHeaderInfo.StartTransitionIndex;
            int endTran = startTran + _groupHeaderInfo.NumTransitions;
            for (int i = startTran; i < endTran; i++)
            {
                if (ChromKey.CompareTolerant(productMz, _allTransitions[i].Product, tolerance) == 0)
                {
                    return new ChromatogramInfo(_groupHeaderInfo, i - startTran,
                        _allFiles, _allTransitions, _allPeaks, Times, IntensityArray);
                }
            }
            return null;
        }
        
        public int IndexOfNearestTime(float time)
        {
            int iTime = Array.BinarySearch(Times, time);
            if (iTime < 0)
            {
                // Get index of first time greater than time argument
                iTime = ~iTime;
                // If the value before it was closer, then use that time
                if (iTime == Times.Length || (iTime > 0 && Times[iTime] - time > time - Times[iTime - 1]))
                    iTime--;
            }
            return iTime;
        }

        // ReSharper disable SuggestBaseTypeForParameter
        public int MatchTransitions(TransitionGroupDocNode nodeGroup, float tolerance)
        // ReSharper restore SuggestBaseTypeForParameter
        {
            int match = 0;
            foreach (TransitionDocNode nodeTran in nodeGroup.Children)
            {
                int start = _groupHeaderInfo.StartTransitionIndex;
                int end = start + _groupHeaderInfo.NumTransitions;
                for (int i = start; i < end; i++)
                {
                    if (ChromKey.CompareTolerant((float)nodeTran.Mz, _allTransitions[i].Product, tolerance) == 0)
                        match++;
                }
            }
            return match;
        }

        public void ReadChromatogram(ChromatogramCache cache)
        {
            Stream stream = cache.ReadStream.Stream;

            // Seek to stored location
            stream.Seek(_groupHeaderInfo.LocationPoints, SeekOrigin.Begin);

            // Single read to get all the points
            byte[] pointsCompressed = new byte[_groupHeaderInfo.CompressedSize];
            if (stream.Read(pointsCompressed, 0, pointsCompressed.Length) < pointsCompressed.Length)
                throw new IOException("Failure trying to read points");

            int numPoints = _groupHeaderInfo.NumPoints;
            int numTrans = _groupHeaderInfo.NumTransitions;

            int sizeArray = sizeof(float) * numPoints;
            int size = sizeArray * (numTrans + 1);
            byte[] peaks = pointsCompressed.Uncompress(size);

            float[][] intensities;
            float[] times;

            ChromatogramCache.BytesToTimeIntensities(peaks, numPoints, numTrans,
                out intensities, out times);

            IntensityArray = intensities;
            Times = times;
        }
    }

// ReSharper disable InconsistentNaming
    public enum TransformChrom { none, craw2d, craw1d, savitzky_golay }
// ReSharper restore InconsistentNaming

    public class ChromatogramInfo : ChromatogramGroupInfo
    {
        protected readonly int _transitionIndex;

        public ChromatogramInfo(ChromGroupHeaderInfo groupHeaderInfo, int transitionIndex,
                IList<ChromCachedFile> allFiles, ChromTransition[] allTransitions, ChromPeak[] allPeaks,
                float[] times, float[][] intensities)
            : base(groupHeaderInfo, allFiles, allTransitions, allPeaks)
        {
            if (transitionIndex >= _groupHeaderInfo.NumTransitions)
                throw new IndexOutOfRangeException(string.Format("The index {0} must be between 0 and {1}", transitionIndex, _groupHeaderInfo.NumTransitions));
            _transitionIndex = transitionIndex;

            Times = times;
            IntensityArray = intensities;
            if (intensities != null)
                Intensities = intensities[transitionIndex];
        }

        public double ProductMz
        {
            get
            {
                return _allTransitions[_groupHeaderInfo.StartTransitionIndex + _transitionIndex].Product;
            }
        }

        public float[] Intensities { get; private set; }

        public IEnumerable<ChromPeak> Peaks
        {
            get
            {
                int startPeak = _groupHeaderInfo.StartPeakIndex + (_transitionIndex * _groupHeaderInfo.NumPeaks);
                int endPeak = startPeak + _groupHeaderInfo.NumPeaks;
                for (int i = startPeak; i < endPeak; i++)
                    yield return _allPeaks[i];
            }
        }

        public ChromPeak GetPeak(int peakIndex)
        {
            if (0 > peakIndex || peakIndex > _groupHeaderInfo.NumPeaks)
                throw new IndexOutOfRangeException(string.Format("The index {0} must be between 0 and {1}", peakIndex, _groupHeaderInfo.NumPeaks));
            return _allPeaks[_groupHeaderInfo.StartPeakIndex + peakIndex + (_transitionIndex * _groupHeaderInfo.NumPeaks)];
        }

        public ChromPeak CalcPeak(int startIndex, int endIndex)
        {
            if (startIndex == endIndex)
                return ChromPeak.EMPTY;

            CrawdadPeakFinder finder = new CrawdadPeakFinder();
            finder.SetChromatogram(Times, Intensities);
            var peak = finder.GetPeak(startIndex, endIndex);
            return new ChromPeak(peak, Times);
        }

        public int IndexOfPeak(double retentionTime)
        {
            int i = 0;
            foreach (var peak in Peaks)
            {
                if (peak.ContainsTime((float)retentionTime))
                    return i;
                i++;
            }
            return -1;
        }

        public void AsArrays(out double[] times, out double[] intensities)
        {
            int len = Times.Length;
            times = new double[len];
            intensities = new double[len];
            for (int i = 0; i < len; i++)
            {
                times[i] = Times[i];
                intensities[i] = Intensities[i];
            }
        }

        public double MaxIntensity
        {
            get
            {
                double max = 0;
                foreach (float intensity in Intensities)
                    max = Math.Max(max, intensity);
                return max;
            }
        }

        public void SumIntensities(IList<ChromatogramInfo> listInfo)
        {
            var intensitiesNew = new float[Intensities.Length];
            foreach (var info in listInfo)
            {
                var intensitiesAdd = info.Intensities;
                for (int i = 0; i < intensitiesAdd.Length; i++)
                    intensitiesNew[i] += intensitiesAdd[i];
            }
            Intensities = intensitiesNew;
        }

        public void Transform(TransformChrom transformChrom)
        {
            switch (transformChrom)
            {
                case TransformChrom.craw2d:
                    Crawdad2DTransform();
                    break;
                case TransformChrom.craw1d:
                    Crawdad1DTransform();
                    break;
                case TransformChrom.savitzky_golay:
                    SavitzkyGolaySmooth();
                    break;
            }
        }

        public void Crawdad2DTransform()
        {
            if (Intensities == null)
                return;
            var peakFinder = new CrawdadPeakFinder();
            peakFinder.SetChromatogram(Times, Intensities);
            Intensities = peakFinder.Intensities2d.ToArray();
        }

        public void Crawdad1DTransform()
        {
            if (Intensities == null)
                return;
            var peakFinder = new CrawdadPeakFinder();
            peakFinder.SetChromatogram(Times, Intensities);
            Intensities = peakFinder.Intensities1d.ToArray();
        }

        public void SavitzkyGolaySmooth()
        {
            if (Intensities == null || Intensities.Length < 9)
                return;
            var intRaw = Intensities;
            var intSmooth = new float[intRaw.Length];
            Array.Copy(Intensities, intSmooth, 4);
            for (int i = 4; i < intRaw.Length - 4; i++)
            {
                double sum = 59 * intRaw[i] +
                    54 * (intRaw[i - 1] + intRaw[i + 1]) +
                    39 * (intRaw[i - 2] + intRaw[i + 2]) +
                    14 * (intRaw[i - 3] + intRaw[i + 3]) -
                    21 * (intRaw[i - 4] + intRaw[i + 4]);
                intSmooth[i] = (float)(sum / 231);
            }
            Array.Copy(intRaw, intRaw.Length - 4, intSmooth, intSmooth.Length - 4, 4);
            Intensities = intSmooth;
        }
    }

    internal static class Kernel32
    {
        [DllImport("kernel32", SetLastError = true)]
        internal static extern unsafe bool ReadFile(
            SafeHandle hFile,
            byte* lpBuffer,
            UInt32 numberOfBytesToRead,
            UInt32* lpNumberOfBytesRead,
            NativeOverlapped* lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern unsafe bool WriteFile(
            SafeHandle handle,
            byte* lpBuffer,
            UInt32 numBytesToWrite,
            UInt32* numBytesWritten,
            NativeOverlapped* lpOverlapped);        
    }
}
