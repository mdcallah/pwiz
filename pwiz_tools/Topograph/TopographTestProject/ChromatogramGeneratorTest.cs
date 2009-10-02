﻿using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Topograph.MsData;
using pwiz.Topograph.Data;
using pwiz.Topograph.Enrichment;
using pwiz.Topograph.Model;

namespace pwiz.Topograph.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class ChromatogramGeneratorTest : BaseTest
    {
        public ChromatogramGeneratorTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

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

        [TestMethod]
        public void TestChromatogramGenerator()
        {
            //SetupForPwiz();
            String dbPath = Path.Combine(TestContext.TestDir, "test" + Guid.NewGuid() + ".tpg");
            using (var sessionFactory = SessionFactoryFactory.CreateSessionFactory(dbPath, true))
            {
                using (var session = sessionFactory.OpenSession())
               { 
                    DbEnrichment dbEnrichment = EnrichmentDef.GetD3LeuEnrichment();
                    session.Save(dbEnrichment);
                    DbWorkspace dbWorkspace = new DbWorkspace
                    {
                        Enrichment = dbEnrichment
                    };
                    session.Save(dbWorkspace);
                }
            }
            Workspace workspace = new Workspace(dbPath);
            workspace.SetEnrichment(EnrichmentDef.GetN15Enrichment());
            MsDataFile msDataFile;
            using (var session = workspace.OpenWriteSession())
            {
                session.BeginTransaction();
                var dbMsDataFile = new DbMsDataFile()
                {
                    Name = "20090724_HT3_0",
                    Path = Path.Combine(GetDataDirectory(), "20090724_HT3_0.RAW"),
                    Workspace = workspace.LoadDbWorkspace(session),
                };
                session.Save(dbMsDataFile);
                session.Transaction.Commit();

                msDataFile = new MsDataFile(workspace, dbMsDataFile);
            }
            Assert.IsTrue(MsDataFileUtil.InitMsDataFile(workspace, msDataFile));
            DbPeptide dbPeptide;
            using (var session = workspace.OpenWriteSession())
            {
                session.BeginTransaction();
                dbPeptide = new DbPeptide
                {
                    Protein = "TestProtein",
                    MaxTracerCount = 4,
                    Sequence = "YLAAYLLLVQGGNAAPSAADIK",
                    FullSequence = "K.YLAAYLLLVQGGNAAPSAADIK.A",
                    Workspace = workspace.LoadDbWorkspace(session),
                };
                session.Save(dbPeptide);
                var searchResult = new DbPeptideSearchResult
                {
                    Peptide = dbPeptide,
                    MsDataFile = session.Load<DbMsDataFile>(msDataFile.Id),
                    MinCharge = 3,
                    MaxCharge = 3,
                    FirstDetectedScan = 20645,
                    LastDetectedScan = 20645
                };
                session.Save(searchResult);
                session.Transaction.Commit();
            }
            var peptide = new Peptide(workspace, dbPeptide);
            var peptideAnalysis = peptide.EnsurePeptideAnalysis();
            var peptideFileAnalysis = PeptideFileAnalysis.EnsurePeptideFileAnalysis(peptideAnalysis, msDataFile);
            var chromatogramGenerator = new ChromatogramGenerator(workspace);
//            chromatogramGenerator.AddPeptideAnalysis(peptideFileAnalysis);
//            chromatogramGenerator.Start();
//            while (!peptideFileAnalysis.HasChromatograms)
//            {
//                Thread.Sleep(100);
//            }
            var chromatogramDatas = peptideFileAnalysis.GetChromatograms();
            Assert.IsFalse(chromatogramDatas.GetChildCount() == 0);
            chromatogramGenerator.Stop();
        }
    }
}
